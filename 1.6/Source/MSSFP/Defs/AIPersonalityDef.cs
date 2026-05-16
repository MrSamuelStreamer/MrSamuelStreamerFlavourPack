using System;
using System.Collections.Generic;
using MSSFP.AICore;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Defs;

/// <summary>
/// XML-defined AI personality. Drop a new file under Defs/AICore/Personalities/ to add one.
/// Six shipped: Grep.ai, ChudGPT, Clive, Geminon, Copylot, Deepsink.
/// </summary>
public class AIPersonalityDef : Def
{
    /// <summary>Short label shown in gizmos / messages (e.g. "Grep.ai"). Falls back to <see cref="Def.label"/>.</summary>
    public string labelShort;

    /// <summary>Tint applied to chat messages and letters for this personality.</summary>
    public Color textColor = Color.white;

    /// <summary>
    /// Tint applied to the holo-projection body when this persona is active. Defaults to
    /// <see cref="textColor"/> when unset so each persona keeps its visual identity without
    /// requiring two parallel color definitions. Alpha channel reserved for future
    /// translucency work (currently ignored by the Cutout pawn shader).
    /// </summary>
    public Color? holoTint;

    /// <summary>
    /// Resolved holo tint — <see cref="holoTint"/> if set, otherwise <see cref="textColor"/>.
    /// Normalises 0-255 channel inputs (parser yields >1.0 channels when XML uses ints) to 0-1
    /// space, mirroring <see cref="MSSFP.AICore.AICoreSpeech"/> normalisation. Alpha forced to 1.0.
    /// </summary>
    public Color HoloTintOrTextColor
    {
        get
        {
            Color c = holoTint ?? textColor;
            if (c.r > 1f || c.g > 1f || c.b > 1f)
                return new Color(c.r / 255f, c.g / 255f, c.b / 255f, 1f);
            return c;
        }
    }

    /// <summary>Optional gizmo icon path under a Textures folder.</summary>
    public string iconPath;

    /// <summary>
    /// Optional xenotype forced on the holo pawn at generation time. Applied to the
    /// <see cref="Verse.PawnGenerationRequest.ForcedXenotype"/> in
    /// <see cref="MSSFP.Holo.CompHoloProjector.EnsureHoloPawn"/>.
    ///
    /// CAVEAT: only takes effect on the first generation of the holo pawn for a given
    /// projector. Once <c>stored</c> holds a pawn, swapping persona at runtime will NOT
    /// regenerate its body — the pawn keeps its existing xenotype. To re-roll, the
    /// projector must be destroyed or the stored pawn cleared. This is intentional:
    /// regenerating a colonist's body on persona swap would wipe their pawn-state
    /// (mood, skills, hediffs) silently.
    /// </summary>
    public XenotypeDef forcedXenotype;

    /// <summary>
    /// Optional fixed gender for the holo pawn at generation time. Applied to the
    /// <see cref="Verse.PawnGenerationRequest.FixedGender"/>. Same first-generation-only
    /// caveat as <see cref="forcedXenotype"/>. Leave unset to let the PawnKindDef roll
    /// gender as normal.
    /// </summary>
    public Gender? fixedGender;

    /// <summary>Lines used for ambient unsolicited chatter (no target pawn).</summary>
    public RulePackDef ambientChatter;

    /// <summary>Lines used when addressing a specific colonist by name.</summary>
    public RulePackDef pawnAddress;

    /// <summary>Rule pack producing sculpture titles.</summary>
    public RulePackDef artTitles;

    /// <summary>Rule pack producing sculpture descriptions.</summary>
    public RulePackDef artDescriptions;

    /// <summary>
    /// Lines used when this persona initiates a vanilla social interaction
    /// (Chitchat, DeepTalk, Slight, Insult, KindWords). The patch
    /// <c>PersonaSocialSwap_Patch</c> replaces the vanilla
    /// <c>logRulesInitiator</c> resolution with these rules at a fixed
    /// per-entry probability. Leave null to keep vanilla chitchat for this
    /// persona. Rules can reference [INITIATOR_*] and [RECIPIENT_*] pawn
    /// fields; the patch supplies both via <see cref="GrammarUtility.RulesForPawn"/>.
    /// </summary>
    public RulePackDef socialInitiator;

    /// <summary>Relative weight when a core rolls a personality (random pick mode).</summary>
    public float weight = 1f;

    /// <summary>
    /// Hediffs that MAY land on a holo pawn driven by this personality. Vanilla hediff-givers
    /// (aging, disease, body modification) are otherwise filtered out by
    /// <see cref="MSSFP.Holo.HoloHediffPolicy"/>. Chemical hediffs and the hologram marker are
    /// always allowed regardless of this list.
    /// </summary>
    public List<HediffDef> allowedHediffs = new();

    /// <summary>
    /// Hediffs auto-applied every time the projection spawns. Idempotent: re-applying does
    /// not stack severity, already-present hediffs are skipped. Use for personality flavour
    /// (e.g. Hollee → Dementia).
    /// </summary>
    public List<HediffDef> fixedHediffs = new();

    /// <summary>Concrete worker class that drives this personality. Must derive from <see cref="AIPersonalityWorker"/>.</summary>
    public Type workerClass = typeof(AIPersonalityWorker);

    [Unsaved(false)]
    private AIPersonalityWorker workerInt;

    /// <summary>Lazily-instantiated worker. Stateless — never store per-core state here.</summary>
    public AIPersonalityWorker Worker
    {
        get
        {
            if (workerInt != null)
                return workerInt;
            workerInt = (AIPersonalityWorker)Activator.CreateInstance(workerClass);
            workerInt.def = this;
            return workerInt;
        }
    }

    public string LabelShortOrLabel => string.IsNullOrEmpty(labelShort) ? label : labelShort;

    /// <summary>
    /// Validate referenced rule packs are non-empty. Empty packs would resolve
    /// to fallback strings at runtime and surface as "no rules for keyword"
    /// noise in the player log — better to flag at def-load time.
    /// </summary>
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string err in base.ConfigErrors())
            yield return err;

        if (socialInitiator != null && socialInitiator.RulesPlusIncludes.NullOrEmpty())
            yield return $"AIPersonalityDef {defName}: socialInitiator pack '{socialInitiator.defName}' has no rules (after includes resolved).";
    }
}
