using RimWorld;
using Verse;

namespace MSSFP.Compatibility.BigAndSmall;

/// <summary>
/// Repair half-populated humanlike trackers on MSSFP sapient ravens produced by
/// <c>BigAndSmall.RaceMorpher.SwapAnimalToSapientVersion</c>.
///
/// Root cause: B&amp;S generates the <c>HL_MSSFP_Raven</c> ThingDef at runtime with
/// <c>race.intelligence = Humanlike</c> (copied from the Human ThingDef) — RimWorld then
/// treats the pawn as humanlike for all downstream purposes (social thoughts, conversations,
/// JobGivers, etc.). However, the swap pipeline
/// (<see cref="!:BigAndSmall.RaceMorpher.SwapAnimalToSapientVersion"/> +
/// <c>SwapThingDef</c>) leaves a subset of humanlike trackers null on the resulting pawn.
/// Confirmed by inspecting a saved Raven 1 pawn block:
///
///   present: story, needs, jobs, drafter, ageTracker, health, interactions, stances,
///            carryTracker, psychicEntropy, royalty, apparel, equipment, inventory, ownership
///   null:    relations, skills, workSettings, playerSettings, ideo, timetable, drugs,
///            outfits, foodRestriction, records
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

        // story is present after B&S swap, but story.traits may be null if Pawn_StoryTracker
        // was created outside the normal constructor path that initialises new TraitSet(pawn).
        // SkillRecord.Interval() accesses pawn.story.traits without a null guard → NPE every tick.
        if (pawn.story != null && pawn.story.traits == null)
            pawn.story.traits = new TraitSet(pawn);

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
