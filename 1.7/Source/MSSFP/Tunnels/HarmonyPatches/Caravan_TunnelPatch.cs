using System.Collections.Generic;
using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.Tunnels.HarmonyPatches;

/// <summary>
/// Reroutes core <see cref="Caravan"/> behaviour for <see cref="TunnelCaravan"/> instances:
/// no downed/mental-break/night-resting/mass-immobilization logic (they are minecart
/// cargo, not marching pawns), custom pathing through the tunnel graph, and a custom
/// arrival-time estimate and inspect string based on <see cref="TunnelCaravan.Progress"/>.
/// </summary>
[HarmonyPatch(typeof(Caravan))]
public static class Caravan_TunnelPatch
{
    [HarmonyPatch(nameof(Caravan.AllOwnersDowned), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool AllOwnersDownedPrefix(Caravan __instance, ref bool __result)
    {
        if (!TunnelUtilities.IsEnabled()) return true;

        if (__instance is TunnelCaravan)
        {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyPatch(nameof(Caravan.AllOwnersHaveMentalBreak), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool AllOwnersHaveMentalBreakPrefix(Caravan __instance, ref bool __result)
    {
        if (!TunnelUtilities.IsEnabled()) return true;

        if (__instance is TunnelCaravan)
        {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyPatch(nameof(Caravan.NightResting), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool NightRestingPrefix(Caravan __instance, ref bool __result)
    {
        if (!TunnelUtilities.IsEnabled()) return true;

        if (__instance is TunnelCaravan)
        {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyPatch(nameof(Caravan.ImmobilizedByMass), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool ImmobilizedByMassPrefix(Caravan __instance, ref bool __result)
    {
        if (!TunnelUtilities.IsEnabled()) return true;

        if (__instance is TunnelCaravan)
        {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyPatch(nameof(Caravan.CantMove), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool CantMovePrefix(Caravan __instance, ref bool __result)
    {
        if (!TunnelUtilities.IsEnabled()) return true;

        if (__instance is TunnelCaravan)
        {
            __result = __instance.pather.Paused;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(Caravan_PathFollower), "GenerateNewPath")]
    public static class Caravan_PathFollower_GenerateNewPath_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(Caravan_PathFollower __instance, ref WorldPath __result, Caravan ___caravan)
        {
            // Cached once at method entry (Cluster A perf note P2) — this runs on the
            // hot per-tick pathing path, so avoid repeated cache lookups below.
            bool tunnelsEnabled = TunnelUtilities.IsEnabled();
            if (!tunnelsEnabled) return true;

            if (___caravan is TunnelCaravan tunnelCaravan)
            {
                TunnelGenData tunnelGenData = TunnelGenData.Instance;
                if (tunnelGenData != null)
                {
                    List<int> nodes = tunnelGenData.FindTunnelPath(tunnelCaravan.origin, tunnelCaravan.destination);
                    if (nodes != null && nodes.Count > 0)
                    {
                        // Current progress (0 to 1)
                        float progress = Mathf.Clamp01((float)(Find.TickManager.TicksGame - tunnelCaravan.travelStartsAtTick) / (tunnelCaravan.travelEndsAtTick - tunnelCaravan.travelStartsAtTick));

                        // How many nodes to skip
                        int nodesToSkip = Mathf.FloorToInt(progress * (nodes.Count - 1));

                        WorldPath path = Find.WorldPathPool.GetEmptyWorldPath();

                        // Add nodes from Destination back to the point where we are.
                        // Final path in WorldPath should be [CurrentNode, ..., Destination]
                        for (int i = nodes.Count - 1; i >= nodesToSkip; i--)
                        {
                            path.AddNodeAtStart(nodes[i]);
                        }

                        path.SetupFound(100f, tunnelCaravan.Tile.Layer);

                        // RimWorld's GenerateNewPath expects that if we're moving,
                        // the path starts at the current tile or the next tile.
                        PlanetTile startTile = !__instance.Moving || !__instance.nextTile.Valid || !__instance.IsNextTilePassable() ? ___caravan.Tile : __instance.nextTile;

                        if (path.NodesLeftCount > 0)
                        {
                            if (path.Peek(0) != startTile)
                            {
                                // If the first node isn't our startTile, we might need to adjust.
                                // If we're already at startTile (which is nodes[nodesToSkip]), it's fine.
                                // But if we've moved a bit and startTile is something else, we add it.
                                if (path.NodesLeftCount >= 2 && path.Peek(1) == startTile)
                                {
                                    path.ConsumeNextNode();
                                }
                                else
                                {
                                    path.AddNodeAtStart(startTile);
                                }
                            }
                        }
                        else
                        {
                            path.AddNodeAtStart(startTile);
                        }

                        __result = path;
                        return false;
                    }
                }
            }
            return true;
        }
    }

    [HarmonyPatch("ShouldAutoCapture")]
    [HarmonyPrefix]
    public static bool ShouldAutoCapturePrefix(Caravan __instance, ref bool __result)
    {
        if (!TunnelUtilities.IsEnabled()) return true;

        if (__instance is TunnelCaravan)
        {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyPatch(typeof(CaravanArrivalTimeEstimator), "EstimatedTicksToArrive", new[] { typeof(Caravan), typeof(bool) })]
    [HarmonyPrefix]
    public static bool EstimatedTicksToArrivePrefix(Caravan caravan, ref int __result)
    {
        if (!TunnelUtilities.IsEnabled()) return true;

        if (caravan is TunnelCaravan tunnelCaravan)
        {
            __result = Mathf.Max(0, tunnelCaravan.travelEndsAtTick - Find.TickManager.TicksGame);
            return false;
        }
        return true;
    }

    [HarmonyPatch(nameof(Caravan.GetInspectString))]
    [HarmonyPrefix]
    public static bool GetInspectStringPrefix(Caravan __instance, ref string __result)
    {
        if (!TunnelUtilities.IsEnabled()) return true;

        if (__instance is TunnelCaravan tunnelCaravan)
        {
            __result = tunnelCaravan.Progress;
            return false;
        }
        return true;
    }
}
