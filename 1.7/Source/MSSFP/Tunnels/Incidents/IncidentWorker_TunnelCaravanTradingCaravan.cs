using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace MSSFP.Tunnels.Incidents;

/// <summary>
/// Incident: a friendly trading caravan is encountered inside the tunnel.
/// The player's caravan can trade at leisure, then reform normally at any time.
///
/// No combat, no reform blocking. Uses vanilla LordJob_TradeWithColony directly.
/// NPC exit via LordToil_ExitMap does NOT call ExitMapAndCreateCaravan, so the
/// (commit 4) tunnel-encounter reform patch is never triggered by the trader's departure.
/// </summary>
public class IncidentWorker_TunnelCaravanTradingCaravan : IncidentWorker_TunnelCaravanSomethingHappened
{
    // Stored between PostProcessGeneratedPawnsAfterSpawning and CreateLordJob.
    private IntVec3 _chillSpot;

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (!base.CanFireNowSub(parms)) return false;
        // Fail fast if no eligible faction exists (e.g. early game, all factions hostile).
        return Find.FactionManager.AllFactionsListForReading.Any(x =>
            !x.IsPlayer
            && !x.HostileTo(Faction.OfPlayer)
            && !x.Hidden
            && x.def.humanlikeFaction
            && !x.temporary
            && x.def.caravanTraderKinds.Any()
            && !x.def.pawnGroupMakers.NullOrEmpty());
    }

    protected override List<Pawn> GeneratePawns(IncidentParms parms)
    {
        if (!TryFindTraderFaction(out Faction faction))
            return new List<Pawn>(); // causes base TryExecuteWorker to return false

        parms.faction = faction; // used by LordMaker.MakeNewLord and CreateLordJob

        return PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
        {
            groupKind = PawnGroupKindDefOf.Trader,
            faction = faction,
            points = TraderCaravanUtility.GenerateGuardPoints(),
            dontUseSingleUseRocketLaunchers = true,
        }, warnOnZeroResults: true).ToList();
    }

    protected override void PostProcessGeneratedPawnsAfterSpawning(List<Pawn> generatedPawns)
    {
        Map map = generatedPawns.FirstOrDefault()?.MapHeld;
        if (map == null) return;

        // Pick a standable cell near the map centre as the trading camp spot.
        _chillSpot = map.Center;
        if (!_chillSpot.Standable(map))
            CellFinder.TryFindRandomCellNear(map.Center, map, 15,
                c => c.Standable(map) && !c.Fogged(map), out _chillSpot);
    }

    protected override LordJob CreateLordJob(List<Pawn> generatedPawns, IncidentParms parms)
    {
        // LordJob_TradeWithColony: vanilla state machine for map-based trader visits.
        // Handles travel, wait (27k–45k ticks), gift on departure, and self-exit.
        return new LordJob_TradeWithColony(parms.faction, _chillSpot);
    }

    private static bool TryFindTraderFaction(out Faction faction)
    {
        return Find.FactionManager.AllFactionsListForReading
            .Where(x =>
                !x.IsPlayer
                && !x.HostileTo(Faction.OfPlayer)
                && !x.Hidden
                && x.def.humanlikeFaction
                && !x.temporary
                && x.def.caravanTraderKinds.Any()
                && !x.def.pawnGroupMakers.NullOrEmpty())
            .TryRandomElement(out faction);
    }
}
