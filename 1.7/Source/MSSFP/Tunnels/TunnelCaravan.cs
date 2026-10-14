using System.Collections.Generic;
using System.Linq;
using MSSFP.Tunnels.Incidents;
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

    // Cached tunnel path (origin → destination) plus the TunnelGenData.graphVersion it was
    // computed against. FindTunnelPath is a full BFS over the tunnel graph, so recomputing
    // it every tick/render-frame per in-transit caravan is wasteful — the path only ever
    // changes when the underlying graph mutates (tunnel overlay/clear), not tick-to-tick.
    private List<int> _cachedPath;
    private int _cachedPathGraphVersion = -1;

    private List<int> GetCachedPath(TunnelGenData tunnelGenData)
    {
        if (_cachedPath == null || _cachedPathGraphVersion != tunnelGenData.graphVersion)
        {
            _cachedPath = tunnelGenData.FindTunnelPath(origin, destination);
            _cachedPathGraphVersion = tunnelGenData.graphVersion;
        }

        return _cachedPath;
    }

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
                List<int> nodes = GetCachedPath(tunnelGenData);
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
            List<int> nodes = GetCachedPath(tunnelGenData);
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
        if (!IncidentWorker_TunnelCaravanSomethingHappened.MeetsCaravanGuard(this, out var reason))
        {
            ModLog.Debug($"[TunnelCaravan] TryFireTunnelIncidents: guard failed (done={done}, spawned={Spawned}, pawns={PawnsListForReading.Count}, reason={reason})");
            return;
        }

        ModLog.Debug($"[TunnelCaravan] TryFireTunnelIncidents: guard passed, checking categories");
        // MTB values are in tiles (once per tile check), not days.
        // At 130 tiles (cross-globe): ThreatBig ~48%, ThreatSmall ~74%, Misc ~96% chance of ≥1 fire.
        TryFireCategory(IncidentCategoryDefOf.ThreatBig, 200f);
        TryFireCategory(IncidentCategoryDefOf.ThreatSmall, 80f);
        TryFireCategory(IncidentCategoryDefOf.Misc, 40f);
    }

    private void TryFireCategory(IncidentCategoryDef category, float mtbTiles)
    {
        // Higher weight multiplier → shorter mean-time-between → more frequent incidents.
        // Floor matches the settings slider's minimum, avoiding a divide-by-zero blowout.
        float weightMultiplier = Mathf.Max(MSSFPMod.settings.TunnelIncidentWeightMultiplier, 0.05f);
        float adjustedMtbTiles = mtbTiles / weightMultiplier;

        if (!Rand.MTBEventOccurs(adjustedMtbTiles, 1f, 1f))
        {
            ModLog.Debug($"[TunnelCaravan] TryFireCategory {category.defName}: MTB check failed (mtbTiles={adjustedMtbTiles})");
            return;
        }

        IncidentParms parms = StorytellerUtility.DefaultParmsNow(category, this);

        List<IncidentDef> candidates = DefDatabase<IncidentDef>.AllDefsListForReading
            .Where(d => d.category == category && d.TargetAllowed(this) && d.Worker.CanFireNow(parms))
            .ToList();

        ModLog.Debug($"[TunnelCaravan] TryFireCategory {category.defName}: MTB passed, {candidates.Count} candidate(s): [{string.Join(", ", candidates.Select(d => d.defName))}]");

        IncidentDef chosen = candidates.RandomElementByWeightWithFallback(d => d.baseChance);

        ModLog.Debug($"[TunnelCaravan] TryFireCategory {category.defName}: chose {chosen?.defName ?? "none"}");
        chosen?.Worker.TryExecute(parms);
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
