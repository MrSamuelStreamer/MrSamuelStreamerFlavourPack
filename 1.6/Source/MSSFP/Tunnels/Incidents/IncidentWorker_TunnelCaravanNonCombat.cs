using System.Collections.Generic;
using MSSFP.Tunnels.MapComponents;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace MSSFP.Tunnels.Incidents;

/// <summary>
/// Abstract base for non-combat "Something Happened" tunnel incidents.
///
/// Bypasses IncidentWorker_Ambush.TryExecuteWorker entirely — no ghost pawn.
/// The approach:
///   1. CaptureEncounterSetup runs normally.
///   2. SetupCaravanAttackMap is called with an empty enemy list. This is safe:
///      CalculateIncidentMapSize handles an empty list, the enemy-spawn loop
///      is skipped, and CaravanEnterMapUtility.Enter places player pawns normally.
///   3. PostSetupEncounterMap(map) gives subclasses a clean entry point.
///   4. Letter is sent with map.Parent (a WorldObject) as look target — the
///      "Jump to location" button works without any pawn on the map.
///   5. Notify_GeneratedPotentiallyHostileMap is NOT called — no hostiles means
///      no game pause.
///
/// Subclasses override PostSetupEncounterMap(Map) to place content on the map.
/// Subclasses should NOT override GeneratePawns, CreateLordJob, or
/// PostProcessGeneratedPawnsAfterSpawning — all three are sealed here.
/// </summary>
public abstract class IncidentWorker_TunnelCaravanNonCombat : IncidentWorker_TunnelCaravanSomethingHappened
{
    // Sealed — non-combat incidents never need enemy pawns.
    // IncidentWorker_Ambush.TryExecuteWorker bails on an empty list,
    // but we never call that path.
    protected sealed override List<Pawn> GeneratePawns(IncidentParms parms)
        => new List<Pawn>();

    // Sealed — no lord needed for non-combat encounters.
    protected sealed override LordJob CreateLordJob(List<Pawn> generatedPawns, IncidentParms parms)
        => null;

    // Sealed — use PostSetupEncounterMap(Map) instead.
    protected sealed override void PostProcessGeneratedPawnsAfterSpawning(List<Pawn> generatedPawns)
    {
    }

    /// <summary>
    /// Called after the encounter map is generated and player pawns have entered.
    /// Place items, add MapComponents, etc. here instead of in
    /// PostProcessGeneratedPawnsAfterSpawning.
    /// </summary>
    protected virtual void PostSetupEncounterMap(Map map)
    {
    }

    /// <summary>
    /// Overrides IncidentWorker_TunnelCaravanSomethingHappened.TryExecuteWorker without
    /// calling base — base calls IncidentWorker_Ambush.TryExecuteWorker, which would bail
    /// because GeneratePawns returns empty. We replicate the relevant setup here.
    /// </summary>
    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        CaptureEncounterSetup(parms);

        LongEventHandler.QueueLongEvent(delegate
        {
            DoNonCombatExecute(parms);
        }, "GeneratingMapForNewEncounter", doAsynchronously: false, null);

        return true;
    }

    /// <summary>
    /// Performs the actual encounter-map setup. Promoted to protected virtual so
    /// subclasses (e.g. the cave-in) can wrap it — e.g. to set a routing flag
    /// before SetupCaravanAttackMap is called.
    /// </summary>
    protected virtual void DoNonCombatExecute(IncidentParms parms)
    {
        Map map = CaravanIncidentUtility.SetupCaravanAttackMap(
            (Caravan)parms.target,
            new List<Pawn>(),
            sendLetterIfRelatedPawns: false);

        // No hostiles — CheckWonBattle fires immediately without this guard.
        map.components.Add(new MapComponent_SuppressBattleWon(map));

        TunnelUtilities.TrySpawnRubyVeins(map);
        PostSetupEncounterMap(map);

        // map.Parent is a WorldObject; LookTargets has an implicit operator for it.
        // The "Jump to location" button in the letter works correctly.
        // Notify_GeneratedPotentiallyHostileMap is intentionally omitted — no game pause.
        SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, map.Parent);
    }
}
