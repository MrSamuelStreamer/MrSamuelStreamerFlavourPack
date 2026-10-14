using System.Collections.Generic;
using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Gravship lift takes the projector with it but holos are spawned pawns on the map. A holo
/// whose projector is not on the new map is orphaned — the projection must collapse.
/// Postfix on <see cref="GravshipPlacementUtility.PlaceGravshipInMap"/> sweeps the new map's
/// spawned pawns and collapses any holo whose projector parent is missing or off-map.
/// </summary>
[HarmonyPatch(typeof(GravshipPlacementUtility), nameof(GravshipPlacementUtility.PlaceGravshipInMap))]
public static class GravshipPlacementUtility_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Map map)
    {
        if (map?.mapPawns == null)
            return;

        List<Pawn> orphans = new List<Pawn>();
        foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
        {
            CompHoloProjected comp = p?.TryGetComp<CompHoloProjected>();
            if (comp?.sourceProjector == null)
                continue;
            Thing proj = comp.sourceProjector;
            if (proj.Map != map || !proj.Spawned)
                orphans.Add(p);
        }

        foreach (Pawn orphan in orphans)
        {
            Log.Message($"[MSSFP] Collapsing orphan holo {orphan.LabelShortCap} after gravship placement (projector off-map).");
            if (orphan.Spawned)
                orphan.DeSpawn();
            if (!orphan.Destroyed)
                orphan.Destroy();
        }
    }
}
