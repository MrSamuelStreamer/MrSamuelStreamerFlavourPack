using MSSFP.Comps;
using RimWorld;
using Verse;

namespace MSSFP.AICore;

/// <summary>
/// Worker for the Mr Beans coffee-machine persona. Stateless singleton — see
/// <see cref="AIPersonalityWorker"/> invariant. Per-core state lives in
/// <see cref="CompTrueAICore.personalityScratch"/> under the keys below.
///
/// Personality hook: emits a red-letter ThreatBig urgent bulletin during the local-time
/// "night shift" window [01:00, 05:00) when the colony is in a peaceful state. Lines are
/// pulled from <c>AIPersonalityDef.scheduledChatter</c> (the <c>MSSFP_RP_AICore_MrBeans_Urgent</c>
/// pack), distinct from the MTB-driven ambient chatter pack so the urgent voice never bleeds
/// into ordinary daytime quips.
///
/// Throttle bookkeeping:
///   <c>mrbeans_lastAlert</c> — last TicksGame an urgent fired. ≥ <see cref="GenDate.TicksPerDay"/>
///   gap enforced so even a deeply-insomniac colony sees at most one bulletin per game day.
///
/// Gating (in evaluation order — cheapest first):
///   1. <c>core.parent.Map</c> non-null (skip pocket maps / caravan-only state)
///   2. Local hour in [1, 5)
///   3. <c>StoryDanger.None</c> (never preempt raid/threat letters with flavour)
///   4. Per-core daily throttle from scratch
///   5. MTB roll — ~one fire every ~5 in-game days within the active window
///   6. Non-empty resolved line from <see cref="RollScheduled"/>
///
/// Cap policy: this hook DELIBERATELY bypasses <c>CompTrueAICore.lettersPerDay</c>.
/// The ambient chatter path's letter cap is for stochastic flavour letters and would
/// silently swallow scheduled bulletins on a noisy day. Daily throttle above plus the
/// MTB roll already self-rate-limit the bulletin to "rare 3am event" cadence.
/// </summary>
public class MrBeansWorker : AIPersonalityWorker
{
    private const string ScratchKey_LastAlert = "mrbeans_lastAlert";

    /// <summary>
    /// MTB hours for the urgent roll WITHIN the [1,5) active window. At 250-tick rare-tick
    /// cadence and a 4-hour window per game day, ~120h yields roughly one bulletin every
    /// 5 in-game days on average — sparse enough that the player still finds it funny.
    /// </summary>
    private const float UrgentMtbHours = 120f;

    private const int WindowStartHourInclusive = 1;
    private const int WindowEndHourExclusive = 5;

    public override bool TickScheduled(CompTrueAICore core)
    {
        if (core?.parent == null) return false;

        // Map null-guard: pocket maps and caravan-only state can leave parent.Map null even
        // when Spawned is true on edge transitions. GenLocalDate.HourOfDay(Map) NREs on null.
        Map map = core.parent.Map;
        if (map == null) return false;

        int hour = GenLocalDate.HourOfDay(map);
        if (hour < WindowStartHourInclusive || hour >= WindowEndHourExclusive) return false;

        // Never preempt threat letters with flavour. Vanilla DangerRating folds raids,
        // mech clusters, etc. into a single high-level signal — the right gate for "is the
        // player currently being attacked-at".
        if (map.dangerWatcher != null && map.dangerWatcher.DangerRating != StoryDanger.None)
            return false;

        int now = Find.TickManager.TicksGame;

        // Scratch throttle. Stored as string per personalityScratch contract.
        if (core.personalityScratch != null
            && core.personalityScratch.TryGetValue(ScratchKey_LastAlert, out string lastStr)
            && int.TryParse(lastStr, out int last)
            && now - last < GenDate.TicksPerDay)
        {
            return false;
        }

        // MTB roll against 250-tick rare-tick delta. Matches the cadence the host comp ticks at.
        if (!Rand.MTBEventOccurs(UrgentMtbHours, GenDate.TicksPerHour, 250f)) return false;

        string line = RollScheduled(core);
        if (string.IsNullOrEmpty(line)) return false;

        string label = def?.LabelShortOrLabel ?? "Mr Beans";
        bool emitted = AICoreSpeech.EmitLetter(core, label, line, LetterDefOf.ThreatBig);
        if (!emitted) return false;

        // Only commit the throttle on a successful emit. A silent-suppress (no anchor /
        // unpowered race) must leave the window open so the next eligible tick can retry.
        core.personalityScratch ??= new System.Collections.Generic.Dictionary<string, string>();
        core.personalityScratch[ScratchKey_LastAlert] = now.ToString();
        return true;
    }
}
