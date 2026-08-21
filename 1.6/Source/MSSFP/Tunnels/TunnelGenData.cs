using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.Tunnels;

/// <summary>
/// Sticky per-save snapshot of whether the tunnel system is enabled for this world,
/// and (once enabled) the live tunnel graph: candidate node tiles, drawn links between
/// them, and the pathing helper used to resolve travel routes and reachability.
/// The enabled flag is set once during world-gen (commit 2) and never re-derived from
/// live settings, so flipping the mod settings mid-game never retroactively
/// enables/disables an existing save. The graph itself is rebuilt each session (see
/// <see cref="WorldComponentTick"/>) rather than persisted, mirroring the DFP source.
/// </summary>
public class TunnelGenData(World world) : WorldComponent(world)
{
    public bool tunnelsEnabledForSave;
    public bool wasSet;
    public bool ossuaryFired;
    public bool brendaFired;
    public bool shrineFired;

    public WorldPathing Pather
    {
        get
        {
            field ??= new WorldPathing(Find.WorldGrid.Surface);
            return field;
        }
    }

    public static IEnumerable<WorldObject> WorldObjectsWithTunnelEntrances()
    {
        foreach (TunnelEntrance tunnelEntrance in Find.WorldObjects.AllWorldObjects.OfType<TunnelEntrance>())
        {
            yield return tunnelEntrance;
        }

        foreach (Map map in Find.Maps.Where(m => m.IsPlayerHome))
        {
            yield return Find.WorldObjects.WorldObjectAt<WorldObject>(map.Tile);
        }
    }

    public struct TunnelLink
    {
        public PlanetTile neighbor;
        public TunnelDef tunnel;
    }

    public static TunnelGenData Instance => Find.World.GetComponent<TunnelGenData>();
    public Dictionary<PlanetLayer, List<PlanetTile>> tunnelNodes = new();
    public Dictionary<SurfaceTile, List<TunnelLink>> potentialTunnels = new();
    private Dictionary<int, HashSet<int>> reachableCache = new();
    private bool tunnelNetworkReady;

    public HashSet<int> GetReachableTilesFrom(int startTile)
    {
        if (reachableCache.TryGetValue(startTile, out HashSet<int> reachable))
            return reachable;

        HashSet<int> visited = new HashSet<int> { startTile };
        Queue<int> queue = new Queue<int>();
        queue.Enqueue(startTile);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            if (Find.WorldGrid[current] is { } surfaceTile && potentialTunnels.TryGetValue(surfaceTile, out List<TunnelLink> links))
            {
                foreach (TunnelLink link in links)
                {
                    if (visited.Add(link.neighbor.tileId))
                    {
                        queue.Enqueue(link.neighbor.tileId);
                    }
                }
            }
        }

        reachableCache[startTile] = visited;
        return visited;
    }

    public void SendCaravan(Building_Tunnel tunnel)
    {
        if (!tunnel.innerContainer.OfType<Pawn>().Any())
        {
            Log.Error("Attempted to send tunnel caravan without any pawns. Cancelling.");
            tunnel.CancelLoad();
            return;
        }

        float distance = Find.WorldGrid.ApproxDistanceInTiles(tunnel.origin, tunnel.destination);

        float tilesPerHour = Mathf.Max(MSSFPMod.settings.TunnelDefaultTilesPerHour, 0.0001f);
        if (MSSFPDefOf.MSSFP_Minecarts.IsFinished)
        {
            tilesPerHour = Mathf.Max(MSSFPMod.settings.TunnelResearchedTilesPerHour, 0.0001f);
        }

        int ticksToTravel = Mathf.FloorToInt((distance / tilesPerHour) * GenDate.TicksPerHour);

        // Create actual Caravan
        List<Pawn> pawns = tunnel.innerContainer.OfType<Pawn>().ToList();
        TunnelCaravan newCaravan = (TunnelCaravan)WorldObjectMaker.MakeWorldObject(MSSFPDefOf.MSSFP_TunnelCaravanWorldObject);
        newCaravan.Tile = tunnel.Tile;
        newCaravan.SetFaction(Faction.OfPlayer);
        Find.WorldObjects.Add(newCaravan);

        foreach (Pawn pawn in pawns)
        {
            tunnel.innerContainer.Remove(pawn);
            newCaravan.AddPawn(pawn, true);
            if (!pawn.IsWorldPawn())
            {
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
            }
        }

        // Transfer items
        List<Thing> items = tunnel.innerContainer.Where(t => !(t is Pawn)).ToList();
        foreach (Thing item in items)
        {
            tunnel.innerContainer.Remove(item);
            newCaravan.AddPawnOrItem(item, true);
        }

        newCaravan.origin = tunnel.origin;
        newCaravan.destination = tunnel.destination;
        newCaravan.travelEndsAtTick = Find.TickManager.TicksGame + ticksToTravel;
        newCaravan.travelStartsAtTick = Find.TickManager.TicksGame;
        newCaravan.tunnel = tunnel;

        // Start movement via pather.
        // We set repathImmediately to true so it triggers our GenerateNewPath Harmony patch.
        newCaravan.pather.StartPath(newCaravan.destination.tileId, null, true);

        tunnel.ClearCaravan();
    }

    public List<int> FindTunnelPath(PlanetTile start, PlanetTile end)
    {
        Queue<PlanetTile> queue = new Queue<PlanetTile>();
        Dictionary<PlanetTile, PlanetTile> cameFrom = new Dictionary<PlanetTile, PlanetTile>();

        queue.Enqueue(start);
        cameFrom[start] = PlanetTile.Invalid;

        while (queue.Count > 0)
        {
            PlanetTile current = queue.Dequeue();

            if (current == end)
            {
                List<int> path = new List<int>();
                PlanetTile step = current;
                while (step != PlanetTile.Invalid)
                {
                    path.Add(step.tileId);
                    step = cameFrom[step];
                }
                path.Reverse();
                return path;
            }

            if (current.Tile is SurfaceTile currentSurfaceTile && potentialTunnels.TryGetValue(currentSurfaceTile, out List<TunnelLink> links))
            {
                foreach (TunnelLink link in links)
                {
                    if (!cameFrom.ContainsKey(link.neighbor))
                    {
                        cameFrom[link.neighbor] = current;
                        queue.Enqueue(link.neighbor);
                    }
                }
            }
        }

        Log.Error("Could not find tunnel path from " + start + " to " + end + ". No fallback available.");
        return new List<int> { start.tileId };
    }

    public void Clear()
    {
        tunnelNodes.Clear();
        potentialTunnels.Clear();
        reachableCache.Clear();
    }

    public TunnelDef GetTunnelDef(PlanetTile fromTile, PlanetTile toTile)
    {
        return MSSFPDefOf.MSSFP_Tunnel;
    }

    public void OverlayTunnel(PlanetTile fromPlanetTile, PlanetTile toPlanetTile, TunnelDef tunnelDef)
    {
        PlanetLayer layer = fromPlanetTile.Layer;

        if (tunnelDef == null)
        {
            Log.ErrorOnce("Attempted to remove tunnel with overlayTunnel; not supported", 90292249);
        }
        else
        {
            tunnelDef = GetTunnelDef(fromPlanetTile, toPlanetTile);

            Tile fromTile = layer.Tiles[fromPlanetTile];
            Tile toTole = layer.Tiles[toPlanetTile];

            if (fromTile.Isnt(out SurfaceTile fromSurfaceTile) || toTole.Isnt(out SurfaceTile toSurfaceTile))
                return;

            if (!potentialTunnels.ContainsKey(fromSurfaceTile))
                potentialTunnels.Add(fromSurfaceTile, []);
            if (!potentialTunnels.ContainsKey(toSurfaceTile))
                potentialTunnels.Add(toSurfaceTile, []);

            potentialTunnels[fromSurfaceTile].RemoveAll(tile => tile.neighbor == toPlanetTile);
            potentialTunnels[toSurfaceTile].RemoveAll(tile => tile.neighbor == fromPlanetTile);

            TunnelLink toTunnelLink = new TunnelLink { neighbor = toPlanetTile, tunnel = tunnelDef };
            potentialTunnels[fromSurfaceTile].Add(toTunnelLink);

            TunnelLink fromTunnelLink = new TunnelLink { neighbor = fromPlanetTile, tunnel = tunnelDef };
            potentialTunnels[toSurfaceTile].Add(fromTunnelLink);
            reachableCache.Clear();
            tunnelNetworkReady = true;
        }
    }

    public void GetChildHolders(List<IThingHolder> outChildren) { }

    public ThingOwner GetDirectlyHeldThings() => null;

    public IThingHolder ParentHolder => null;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref tunnelsEnabledForSave, "tunnelsEnabledForSave");
        Scribe_Values.Look(ref wasSet, "wasSet");
        Scribe_Values.Look(ref ossuaryFired, "ossuaryFired");
        Scribe_Values.Look(ref brendaFired, "brendaFired");
        Scribe_Values.Look(ref shrineFired, "shrineFired");
    }

    public override void WorldComponentTick()
    {
        base.WorldComponentTick();

        if (!tunnelsEnabledForSave)
            return;

        if (!tunnelNetworkReady)
        {
            tunnelNetworkReady = true;
            if (potentialTunnels.Count == 0)
                new WorldGenStep_Tunnels().Regenerate(Find.WorldGrid.Surface);
        }

        foreach (TunnelCaravan caravan in Find.WorldObjects.AllWorldObjects.OfType<TunnelCaravan>().Where(c => !c.done && !c.mapGenerating))
        {
            if (Find.TickManager.TicksGame < caravan.travelEndsAtTick) continue;

            WorldObject wo = Find.WorldObjects.WorldObjectAt<WorldObject>(caravan.destination);
            LongEventHandler.QueueLongEvent(() =>
            {
                Map map = Current.Game.FindMap(wo.Tile);
                if (map == null)
                {
                    map = GetOrGenerateMapUtility.GetOrGenerateMap(wo.Tile, wo.def, null);
                }

                ModLog.Debug("Generated map for caravan");
                caravan.SpawnToMap(map);
                ModLog.Debug("Spawned to map");
                Current.Game?.CurrentMap = map;
                caravan.done = true;
            }, "MSSFP_TunnelMap", false, exception =>
            {
                ModLog.Error(exception.ToString());
                caravan.mapGenerating = false;
            });
        }

        // Remove finished caravans.
        foreach (var done in Find.WorldObjects.AllWorldObjects.OfType<TunnelCaravan>().Where(c => c.done).ToList())
            done.Destroy();
    }

    [DebugAction("Spawning", "Spawn Tunnel Entrance", actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.PlayingOnWorld)]
    private static void SpawnTunnelEntrance()
    {
        if (!TunnelUtilities.IsEnabled()) return;

        PlanetTile tile = GenWorld.MouseTile();
        if (!tile.Valid || Find.World.Impassable(tile))
        {
            Messages.Message("Impassable", MessageTypeDefOf.RejectInput, false);
        }
        else
        {
            WorldGenStep_TunnelEntrances.SpawnTunnelEntrance(tile, tile.Layer, Faction.OfAncients);
        }
    }

    [DebugAction("Spawning", "Regenerate Tunnels", allowedGameStates = AllowedGameStates.PlayingOnWorld)]
    public static void RegenerateTunnels()
    {
        if (!TunnelUtilities.IsEnabled()) return;

        LongEventHandler.QueueLongEvent(() =>
        {
            new WorldGenStep_Tunnels().Regenerate(Find.WorldGrid.Surface);
            Find.World.renderer.AllDrawLayers.First(layer => layer is WorldDrawLayer_Tunnels).SetDirty();
        }, "Regenerating Tunnel Network", false, exception =>
        {
            ModLog.Error(exception.ToString());
        });
    }
}
