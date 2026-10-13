using System.Collections.Generic;
using MSSFP.Tunnels.MapComponents;
using RimWorld;
using Verse;

namespace MSSFP.Tunnels.Incidents;

/// <summary>
/// Abstract base for all "Something Happened" tunnel-caravan incidents.
/// Captures TunnelCaravan travel state into TunnelEncounterSetup statics before
/// base.TryExecuteWorker destroys the caravan object, and enforces the standard
/// TunnelCaravan-only CanFireNowSub guard plus the tunnel-system enable/disable gate.
///
/// Fire-once-per-game persistence (Brenda, Forgotten Ossuary) is NOT handled here —
/// each such subclass reads/writes its own dedicated flag on
/// <see cref="TunnelGenData"/> (e.g. brendaFired, ossuaryFired) directly, since that
/// state must survive independently of the incident def's own lifecycle.
///
/// Also provides two shared helpers used by non-SomethingHappened tunnel incidents
/// (InsectAttack, FactionAmbush) that cannot inherit from this class:
///   CaptureEncounterSetup(parms)  — the TunnelEncounterSetup capture block
///   MeetsCaravanGuard(caravan)    — the TunnelCaravan CanFireNowSub checks
/// </summary>
public abstract class IncidentWorker_TunnelCaravanSomethingHappened : IncidentWorker_Ambush
{
    // ── Shared helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Captures the TunnelCaravan's travel state into TunnelEncounterSetup statics.
    /// Called by TryExecuteWorker here and also exposed for workers that extend a
    /// different base class (e.g. IncidentWorker_TunnelCaravanFactionAmbush).
    /// </summary>
    internal static void CaptureEncounterSetup(IncidentParms parms)
    {
        if (parms.target is not TunnelCaravan tunnelCaravan)
            return;

        TunnelEncounterSetup.PendingCaravan = tunnelCaravan;
        TunnelEncounterSetup.HasActiveEncounter = true;
        TunnelEncounterSetup.ActiveEncounterTile = tunnelCaravan.Tile;
        TunnelEncounterSetup.ActiveOrigin = tunnelCaravan.origin;
        TunnelEncounterSetup.ActiveDestination = tunnelCaravan.destination;
        TunnelEncounterSetup.ActiveTunnel = tunnelCaravan.tunnel;
        TunnelEncounterSetup.ActiveTravelStartsAtTick = tunnelCaravan.travelStartsAtTick;
        TunnelEncounterSetup.ActiveTravelEndsAtTick = tunnelCaravan.travelEndsAtTick;
    }

    internal static bool MeetsCaravanGuard(TunnelCaravan tunnelCaravan)
    {
        return MeetsCaravanGuard(tunnelCaravan, out _);
    }

    /// <summary>
    /// Shared CanFireNowSub guard for all tunnel-caravan incidents.
    /// Returns false if the target is not an in-transit, non-spawned TunnelCaravan.
    /// Does NOT call base.CanFireNowSub — callers are responsible for that.
    /// </summary>
    internal static bool MeetsCaravanGuard(TunnelCaravan tunnelCaravan, out string reason)
    {
        if (tunnelCaravan.PawnsListForReading.Count < 1)
        {
            reason = "Caravan has no pawns";
            return false;
        }

        int t = Find.TickManager.TicksGame;
        if (t <= tunnelCaravan.travelStartsAtTick || t >= tunnelCaravan.travelEndsAtTick)
        {
            reason = "Caravan is not in transit";
            return false;
        }

        if (tunnelCaravan.done)
        {
            reason = "Caravan has already arrived";
            return false;
        }
        reason = "";
        return true;
    }

    /// <summary>
    /// Returns a standable, unfogged cell near the map centre.
    /// Falls back to a random nearby cell if map.Center itself is blocked.
    /// </summary>
    protected static IntVec3 FindCellNearCenter(Map map, int radius = 15)
    {
        IntVec3 cell = map.Center;
        if (!cell.Standable(map))
            CellFinder.TryFindRandomCellNear(map.Center, map, radius,
                c => c.Standable(map) && !c.Fogged(map), out cell);
        return cell;
    }

    // Called after combat pawns are spawned on the encounter map.
    // NonCombat incidents seal this method, so this only fires for combat
    // variants (InsectAttack, AncientDefences) that go through base.TryExecuteWorker.
    protected override void PostProcessGeneratedPawnsAfterSpawning(List<Pawn> generatedPawns)
    {
        base.PostProcessGeneratedPawnsAfterSpawning(generatedPawns);
        Map map = generatedPawns.Count > 0 ? generatedPawns[0].Map : null;
        if (map != null)
            TunnelUtilities.TrySpawnRubyVeins(map);
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        CaptureEncounterSetup(parms);
        return base.TryExecuteWorker(parms);
    }

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (!TunnelUtilities.IsEnabled()) return false;
        if (MSSFPMod.settings.DisableTunnelIncidents) return false;

        if (parms.target is not TunnelCaravan tunnelCaravan)
            return false;

        if (!MeetsCaravanGuard(tunnelCaravan))
            return false;

        return base.CanFireNowSub(parms);
    }
}
