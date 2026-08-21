using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.Tunnels;

public class TunnelCaravan : Caravan
{
    public PlanetTile destination = PlanetTile.Invalid;
    public PlanetTile origin = PlanetTile.Invalid;

    public Building_Tunnel tunnel;

    public int travelStartsAtTick = -1;
    public int travelEndsAtTick = -1;

    public bool mapGenerating;
    public bool done;

    private int _lastIncidentTileIndex = -1;

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_References.Look(ref tunnel, "tunnel");
        Scribe_Values.Look(ref travelStartsAtTick, "travelStartsAtTick", -1);
        Scribe_Values.Look(ref travelEndsAtTick, "travelEndsAtTick", -1);
        Scribe_Values.Look(ref origin, "origin");
        Scribe_Values.Look(ref destination, "destination");
        Scribe_Values.Look(ref mapGenerating, "MapGenerating");
        Scribe_Values.Look(ref done, "Done");
    }

    public override Vector3 DrawPos
    {
        get
        {
            if (pather.curPath == null || pather.curPath.NodesLeftCount < 2)
                return base.DrawPos;

            if (!pather.MovingNow)
                return base.DrawPos;

            // When we are using the vanilla-managed pather, the caravan is always moving from Tile to pather.nextTile.
            Vector3 startPos = Find.WorldGrid.GetTileCenter(Tile);
            Vector3 endPos = Find.WorldGrid.GetTileCenter(pather.nextTile);

            // pather.nextTileCostLeft decreases from pather.nextTileCostTotal down to 0 as we move between tiles.
            // But wait, TunnelCaravan doesn't use the vanilla movement cost logic for speed.
            // We force it through ticksToTravel.

            // Let's recalculate progress between the CURRENT tile and the NEXT tile based on time.
            float totalProgress = Mathf.Clamp01((float)(Find.TickManager.TicksGame - travelStartsAtTick) / (travelEndsAtTick - travelStartsAtTick));

            TunnelGenData tunnelGenData = TunnelGenData.Instance;
            if (tunnelGenData != null)
            {
                List<int> nodes = tunnelGenData.FindTunnelPath(origin, destination);
                if (nodes != null && nodes.Count > 1)
                {
                    float floatIndex = totalProgress * (nodes.Count - 1);
                    int index = Mathf.FloorToInt(floatIndex);
                    float t = floatIndex - index;

                    if (index < nodes.Count - 1)
                    {
                        Vector3 s = Find.WorldGrid.GetTileCenter(nodes[index]);
                        Vector3 e = Find.WorldGrid.GetTileCenter(nodes[index + 1]);
                        return FinalizePoint(Vector3.Slerp(s, e, t));
                    }
                }
            }

            return base.DrawPos;
        }
    }

    private Vector3 FinalizePoint(Vector3 inp)
    {
        return inp + inp.normalized * 0.015f;
    }

    protected override void Tick()
    {
        if (done)
        {
            Destroy();
            return;
        }

        // Update Tile based on progress.
        // This will cause the pather to consume nodes if the current Tile changes.
        float progress = Mathf.Clamp01((float)(Find.TickManager.TicksGame - travelStartsAtTick) / (travelEndsAtTick - travelStartsAtTick));

        // We want to know which tile we are "on" according to our progress.
        TunnelGenData tunnelGenData = TunnelGenData.Instance;
        if (tunnelGenData != null)
        {
            List<int> nodes = tunnelGenData.FindTunnelPath(origin, destination);
            if (nodes != null && nodes.Count > 0)
            {
                int index = Mathf.FloorToInt(progress * (nodes.Count - 1));
                int currentTileId = nodes[Mathf.Clamp(index, 0, nodes.Count - 1)];

                if (Tile != currentTileId)
                {
                    ModLog.Debug($"[TunnelCaravan] Tile advance: {Tile} → {currentTileId} (index={index}, progress={progress:F3})");
                    Tile = currentTileId;
                    // If we changed tiles, we might need to refresh the path to ensure it starts at the new tile
                    // and correctly shows progress on the map.
                    pather.Notify_Teleported_Int(); // Clears current path state safely
                    pather.StartPath(destination.tileId, null, true);
                }

                if (index != _lastIncidentTileIndex)
                {
                    _lastIncidentTileIndex = index;
                    TryFireTunnelIncidents();
                }
            }
        }

        base.Tick();
    }

    private void TryFireTunnelIncidents()
    {
        // Deferred to commit 3: DFP gates this on
        // IncidentWorker_TunnelCaravanSomethingHappened.MeetsCaravanGuard, which ships
        // with the tunnel incident workers. No-op until then — no tunnel IncidentDefs
        // exist yet in commit 2, so there would be nothing to fire regardless.
    }

    public string TimeToGo => (travelEndsAtTick - Find.TickManager.TicksGame).ToStringTicksToPeriod();

    public string Progress => PawnsListForReading.Count > 0 ? "MSSFP_Tunnels_CaravanProgress".Translate(TimeToGo, PawnsListForReading[0].Named("PAWN")) : "MSSFP_Tunnels_CaravanProgressItemsOnly".Translate(TimeToGo);

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
        {
            if (gizmo is Command command)
            {
                if (command.defaultLabel == "CommandSettle".Translate() || command.defaultLabel == "CommandSetupCamp".Translate() || command.defaultLabel == "CommandPauseCaravan".Translate())
                {
                    continue;
                }
            }
            yield return gizmo;
        }
    }

    public void SpawnToMap(Map map)
    {
        Building_Tunnel bld = map.listerThings.AllThings.OfType<Building_Tunnel>().FirstOrDefault(t => t.Tile == map.Tile);
        IntVec3 loc = map.Center;
        if (bld != null)
        {
            loc = bld.Position;
        }
        else
        {
            if (RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith(x => x.Standable(map) && !x.Fogged(map), map, out IntVec3 result))
            {
                loc = result;
            }
        }

        List<Pawn> pawns = PawnsListForReading.ToList();
        foreach (Pawn pawn in pawns)
        {
            GenSpawn.Spawn(pawn, loc, map);
        }

        List<Thing> items = Goods.ToList();
        foreach (Thing item in items)
        {
            GenPlace.TryPlaceThing(item, loc, map, ThingPlaceMode.Near);
        }
    }
}
