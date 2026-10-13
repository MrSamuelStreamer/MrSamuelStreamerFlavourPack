using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Tunnels.Incidents;

/// <summary>
/// Incident: an abandoned supply cache is discovered in the tunnel.
/// No combat — loot is spawned near the map centre. No ghost pawn required;
/// IncidentWorker_TunnelCaravanNonCombat handles map setup and letter sending.
/// </summary>
public class IncidentWorker_TunnelCaravanAbandonedCache : IncidentWorker_TunnelCaravanNonCombat
{
    protected override void PostSetupEncounterMap(Map map)
    {
        SpawnCacheItems(map);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SpawnCacheItems(Map map)
    {
        TrySpawnStack(ThingDefOf.Silver, Rand.Range(50, 200), map);
        TrySpawnStack(ThingDefOf.Steel, Rand.Range(30, 100), map);
        TrySpawnStack(ThingDefOf.MealSurvivalPack, Rand.Range(5, 15), map);
        TrySpawnStack(ThingDefOf.MedicineHerbal, Rand.Range(3, 10), map);
        if (Rand.Chance(0.5f))
            TrySpawnStack(ThingDefOf.ComponentIndustrial, Rand.Range(2, 7), map);
        if (Rand.Chance(0.25f))
            TrySpawnStack(ThingDefOf.Gold, Rand.Range(10, 40), map);
        if (Rand.Chance(0.15f))
            TrySpawnStack(ThingDef.Named("MedicineIndustrial"), Rand.Range(2, 6), map);
    }

    private static void TrySpawnStack(ThingDef def, int count, Map map)
    {
        if (def == null) return;
        if (!CellFinder.TryFindRandomCellNear(map.Center, map, 10,
                c => c.Standable(map) && !c.Fogged(map), out IntVec3 cell))
            return;

        Thing thing = ThingMaker.MakeThing(def);
        thing.stackCount = Mathf.Min(count, def.stackLimit);
        GenSpawn.Spawn(thing, cell, map);
    }
}
