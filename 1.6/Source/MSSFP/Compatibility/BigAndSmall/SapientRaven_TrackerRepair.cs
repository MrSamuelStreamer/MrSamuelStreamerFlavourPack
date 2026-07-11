using RimWorld;
using Verse;

namespace MSSFP.Compatibility.BigAndSmall;

/// <summary>
/// Repair half-populated humanlike trackers on MSSFP sapient ravens produced by
/// <c>BigAndSmall.RaceMorpher.SwapAnimalToSapientVersion</c>.
///
/// Root cause: B&amp;S applies the <c>HL_MSSFP_Raven_RaceHediff</c> (aptitudes + forced traits)
/// to make the pawn sapient, but does NOT repoint <c>pawn.def</c> — the pawn keeps the animal
/// <c>MSSFP_Raven</c> ThingDef (confirmed by the tick-error label <c>MSSFP_Raven…</c>, which is
/// <c>def.defName + thingID</c>). The morph attaches some humanlike trackers (notably a
/// <c>Pawn_SkillTracker</c> with leveled records from the aptitudes) while leaving others null
/// on the resulting pawn — observed null in the wild: relations, workSettings, playerSettings,
/// ideo, timetable, drugs, outfits, foodRestriction, records, and — critically —
/// <c>story</c> itself (so <c>story.traits</c> is unreachable, not merely null).
///
/// Third-party mods accessing the null fields then throw <see cref="System.NullReferenceException"/>
/// — observed in the wild from:
///   * <c>Xenomorphtype.ThoughtWorker_Obsession.CurrentSocialStateInternal</c> (relations)
///   * <c>Xenomorphtype.ThoughtWorker_TraumatizedBy.CurrentSocialStateInternal</c> (relations)
///   * <c>RecreationalSexWithEuterpe.JobGiver_GetFood_GetPriority_Patch.Postfix</c>
///   * <c>SpeakUp</c> grammar resolver (story/skills)
///   * vanilla <c>Pawn.GetDisabledWorkTypes</c> via various mods (skills/workSettings)
///
/// Each NRE is caught and written to the log with full stack trace. On a busy colony this
/// produces thousands of writes per minute and craters TPS.
///
/// Fix: idempotently construct the missing trackers. Each tracker's vanilla constructor
/// allocates empty internal collections only — no game-state side effects beyond bringing
/// the pawn into the shape RimWorld and downstream mods expect. <see cref="Pawn_WorkSettings.EnableAndInitialize"/>
/// is called only when the pawn is player-faction (mirrors what
/// <see cref="!:PawnGenerator.GeneratePawn"/> does for a Colonist kind).
///
/// Called from two sites:
/// 1. <see cref="!:MSSFP.Incidents.IncidentWorker_RavenCreepJoinerJoin.GeneratePawn"/> —
///    immediately after the swap, before adding the aura hediff. Fixes future spawns.
/// 2. <see cref="!:MSSFP.HarmonyPatches.Pawn_SpawnSetup_SapientRavenRepair"/> postfix on
///    <c>Pawn.SpawnSetup(map, respawningAfterLoad: true)</c>. Fixes existing sapient
///    ravens in saves authored before this patch shipped.
///
/// All assignments are guarded by null-checks — safe to call multiple times, safe to call
/// on a pawn that has already been repaired.
/// </summary>
public static class SapientRaven_TrackerRepair
{
    /// <summary>
    /// Idempotently populate the humanlike trackers that B&amp;S's sapient-animal swap leaves
    /// null on the resulting pawn. No-op for trackers that already exist.
    /// </summary>
    public static void EnsureHumanlikeTrackers(Pawn pawn)
    {
        if (pawn == null) return;

        if (pawn.relations == null)
            pawn.relations = new Pawn_RelationsTracker(pawn);

        // When B&S RaceMorpher.SwapAnimalToSapientVersion can't produce the HL_MSSFP_Raven
        // humanlike def it returns null (it never hands back a half-built pawn), and the
        // incident keeps the raw animal "MSSFP_Raven" pawn. That pawn has no story tracker
        // (animals never do). The skills block below then allocates a Pawn_SkillTracker whose
        // constructor creates a level-0 SkillRecord for every skill — and SkillRecord.Interval()
        // dereferences pawn.story.traits every tick (unconditionally when Anomaly is active),
        // so "skills present + story null" NREs every tick. Ensure the story tracker, its trait
        // set, and default backstories all exist before we (re)build the skills tracker.
        // Pawn_StoryTracker's constructor allocates a fresh TraitSet; the backstory defs mirror
        // what a successful B&S swap assigns (RaceMorpher.cs), and GetNamedSilentFail keeps a
        // missing def from reintroducing a crash.
        if (pawn.story == null)
            pawn.story = new Pawn_StoryTracker(pawn);
        else if (pawn.story.traits == null)
            pawn.story.traits = new TraitSet(pawn);

        if (pawn.story.Adulthood == null)
            pawn.story.Adulthood = DefDatabase<BackstoryDef>.GetNamedSilentFail("Colonist97");
        if (pawn.story.Childhood == null)
            pawn.story.Childhood = DefDatabase<BackstoryDef>.GetNamedSilentFail("TribeChild19");

        if (pawn.skills == null)
            pawn.skills = new Pawn_SkillTracker(pawn);

        if (pawn.workSettings == null)
        {
            pawn.workSettings = new Pawn_WorkSettings(pawn);
            // EnableAndInitialize sets default work priorities for a player-faction colonist.
            // Calling it on a non-player pawn would assign priorities the AI shouldn't have,
            // so we gate on faction. Mirrors PawnGenerator's behaviour for the Colonist kind.
            if (pawn.Faction != null && pawn.Faction.IsPlayer)
                pawn.workSettings.EnableAndInitialize();
        }

        if (pawn.playerSettings == null)
            pawn.playerSettings = new Pawn_PlayerSettings(pawn);

        if (pawn.timetable == null)
            pawn.timetable = new Pawn_TimetableTracker(pawn);

        if (pawn.drugs == null)
            pawn.drugs = new Pawn_DrugPolicyTracker(pawn);

        if (pawn.outfits == null)
            pawn.outfits = new Pawn_OutfitTracker(pawn);

        if (pawn.foodRestriction == null)
            pawn.foodRestriction = new Pawn_FoodRestrictionTracker(pawn);

        if (pawn.records == null)
            pawn.records = new Pawn_RecordsTracker(pawn);

        // DLC-gated trackers. Constructing these when the corresponding DLC is inactive
        // would create a tracker referencing types from an unloaded assembly.
        if (ModsConfig.IdeologyActive && pawn.ideo == null)
            pawn.ideo = new Pawn_IdeoTracker(pawn);

        if (ModsConfig.RoyaltyActive && pawn.royalty == null)
            pawn.royalty = new Pawn_RoyaltyTracker(pawn);
    }
}
