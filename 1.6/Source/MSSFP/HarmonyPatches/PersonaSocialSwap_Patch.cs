using System.Collections.Generic;
using HarmonyLib;
using MSSFP.AICore;
using MSSFP.Comps;
using MSSFP.Defs;
using MSSFP.Holo;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Postfix on <see cref="Verse.PlayLogEntry_Interaction.ToGameStringFromPOV_Worker"/>.
///
/// When the initiator of a whitelisted social interaction (Chitchat, DeepTalk,
/// Slight, Insult, KindWords) is a holo-projected persona, swaps the rendered
/// line for one drawn from the persona's <c>socialInitiator</c> rule pack at a
/// fixed per-entry probability. Same log entry always resolves the same way
/// (seed derived from <c>LogEntry.logID</c>), so re-rendering on POV change
/// or save/load is stable.
///
/// Priority.Last so other mods' postfixes (typo cleanup, color injection) run
/// before our swap reads the result. We replace the string outright when the
/// roll hits — downstream postfixes that *modify* the vanilla string will not
/// see our text, which is intentional: persona text is authored, not vanilla-
/// shaped.
///
/// Bubbles: Jaxe.Bubbles' <c>Verse_PlayLog_Add</c> postfix reads via
/// <c>entry.ToGameStringFromPOV(initiator)</c> after our swap runs, so persona
/// text flows into chat bubbles automatically. No separate render path.
///
/// extraSentencePacks: rare on the whitelisted defs (0/17 in smoke save).
/// v1 drops them when persona swap fires; revisit if reports surface.
/// </summary>
[HarmonyPatch(typeof(PlayLogEntry_Interaction), "ToGameStringFromPOV_Worker")]
[HarmonyPriority(Priority.Last)]
public static class PersonaSocialSwap_Patch
{
    /// <summary>Probability a holo-initiated whitelisted interaction renders persona text.</summary>
    private const double SwapChance = 0.75;

    /// <summary>
    /// XOR mask for the resolver seed. Mirrors vanilla's logID-seeded resolve
    /// contract while keeping our resolver's random choices distinct from any
    /// vanilla code that may also re-seed off logID.
    /// </summary>
    private const int SeedXor = 0x504552;

    /// <summary>
    /// XOR mask for the swap-decision roll. Deliberately distinct from
    /// <see cref="SeedXor"/> so the 75% gate and the line-pick draw from
    /// independent streams.
    /// </summary>
    private const int RollXor = 0x5045527E;

    /// <summary>
    /// Whitelist of interaction defNames eligible for persona-flavoured output.
    /// Excludes recruitment/romance/anomaly/training paths where vanilla logic
    /// is load-bearing for game mechanics (severity calc, success/failure
    /// arcs).
    /// </summary>
    private static readonly HashSet<string> WhitelistedInteractions = new()
    {
        "Chitchat",
        "DeepTalk",
        "Slight",
        "Insult",
        "KindWords",
    };

    [HarmonyPostfix]
    public static void Postfix(
        ref string __result,
        Pawn ___initiator,
        Pawn ___recipient,
        InteractionDef ___intDef,
        int ___logID)
    {
        // Cheap rejects first — most log entries are not from holos.
        if (___initiator == null || ___intDef == null)
            return;
        if (!WhitelistedInteractions.Contains(___intDef.defName))
            return;

        CompHoloProjected holo = ___initiator.TryGetComp<CompHoloProjected>();
        AIPersonalityDef persona = holo?.Persona;
        if (persona?.socialInitiator == null)
            return;

        // Deterministic gate keyed on logID. Same entry → same outcome
        // across renders, POV changes, and save/load cycles.
        double roll = new System.Random(___logID ^ RollXor).NextDouble();
        if (roll >= SwapChance)
            return;

        CompTrueAICore core = holo.ProjectorComp?.parent?.TryGetComp<CompTrueAICore>();
        if (core == null)
            return;

        AIPersonalityWorker worker = persona.Worker;
        if (worker == null)
            return;

        // Re-seed grammar RNG so each entry resolves to the same persona line
        // every time (vanilla mirrors this exact pattern in its own worker).
        Rand.PushState();
        try
        {
            Rand.Seed = ___logID ^ SeedXor;
            string line = worker.RollSocialLine(core, ___initiator, ___recipient);
            if (!string.IsNullOrEmpty(line))
                __result = line;
        }
        finally
        {
            Rand.PopState();
        }
    }
}
