using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Tunnels;

/// <summary>
/// GenStep for the Cave-In encounter map. Runs at order 240 (after MSSFP_TunnelCaveSetup
/// at 230, before FindPlayerStartSpot at 850).
///
/// Builds a deterministic rock layout:
///   · Every non-tunnel cell                               → biome-matched natural rock wall
///   · Two 5-deep barrier bands in the tunnel (~20 from Z-centre) → CollapsedRocks (mineable)
///   · Enclosed centre section + open approach sections   → clear
///
/// Also scatters miscellaneous loot in the open (non-barrier) tunnel cells.
///
/// Pawn placement is intentionally NOT handled here.
/// CaravanEnterMapUtility.Enter places pawns at the tunnel edge (approach section)
/// after all GenSteps complete. IncidentWorker_TunnelCaravanCaveIn.PostSetupEncounterMap
/// (commit 3) then teleports them into the enclosed section.
///
/// Layout constants are internal so IncidentWorker_TunnelCaravanCaveIn can reference
/// them for pawn placement without duplicating the geometry.
/// </summary>
public class GenStep_TunnelCaveInLayout : GenStep
{
    public override int SeedPart => 847293015;

    // Tunnel is 5 cells wide: centreX − HalfWidth … centreX + HalfWidth.
    internal const int TunnelHalfWidth = 2;

    // Barriers begin this many cells from the map centre along Z.
    internal const int BarrierOffset = 20;

    // Each barrier is this many cells deep.
    internal const int BarrierDepth = 5;

    public override void Generate(Map map, GenStepParams parms)
    {
        ThingDef rockDef = GetRockWallDef(map);
        int centreX = map.Size.x / 2;
        int centreZ = map.Size.z / 2;

        SpawnRocks(map, rockDef, centreX, centreZ);
        SpawnLoot(map, centreX, centreZ);
    }

    // ── Layout helpers ─────────────────────────────────────────────────────────
    // internal so IncidentWorker_TunnelCaravanCaveIn can reuse the same geometry.

    internal static bool IsInTunnel(int x, int centreX)
        => x >= centreX - TunnelHalfWidth && x <= centreX + TunnelHalfWidth;

    internal static bool IsInBarrier(int z, int centreZ)
    {
        // Lower barrier: [centreZ − BarrierOffset − BarrierDepth + 1, centreZ − BarrierOffset]
        // Upper barrier: [centreZ + BarrierOffset,                     centreZ + BarrierOffset + BarrierDepth − 1]
        int lowerFar = centreZ - BarrierOffset - BarrierDepth + 1;
        int lowerNear = centreZ - BarrierOffset;
        int upperNear = centreZ + BarrierOffset;
        int upperFar = centreZ + BarrierOffset + BarrierDepth - 1;
        return (z >= lowerFar && z <= lowerNear) || (z >= upperNear && z <= upperFar);
    }

    internal static bool ShouldBeRock(int x, int z, int centreX, int centreZ)
        => !IsInTunnel(x, centreX) || IsInBarrier(z, centreZ);

    // ── Map manipulation ───────────────────────────────────────────────────────

    private static void SpawnRocks(Map map, ThingDef wallDef, int centreX, int centreZ)
    {
        // Barrier cells use CollapsedRocks — the vanilla "cave-in debris" building that
        // pawns can mine through normally. Solid walls outside the tunnel use the
        // biome-matched natural rock so they blend with the surrounding mountain.
        ThingDef barrierDef = ThingDef.Named("CollapsedRocks");

        foreach (IntVec3 cell in map.AllCells)
        {
            if (!ShouldBeRock(cell.x, cell.z, centreX, centreZ)) continue;

            ThingDef def = IsInBarrier(cell.z, centreZ) ? barrierDef : wallDef;
            GenSpawn.Spawn(
                ThingMaker.MakeThing(def),
                cell, map,
                WipeMode.VanishOrMoveAside);
        }
    }

    // ── Loot spawning ──────────────────────────────────────────────────────────

    private static void SpawnLoot(Map map, int centreX, int centreZ)
    {
        // All in-bounds, non-barrier tunnel cells — both approach sections and the
        // enclosed centre. Loot rewards mining and provides flavour throughout.
        List<IntVec3> openCells = new List<IntVec3>();
        for (int x = centreX - TunnelHalfWidth; x <= centreX + TunnelHalfWidth; x++)
        for (int z = 0; z < map.Size.z; z++)
        {
            if (IsInBarrier(z, centreZ)) continue;
            var cell = new IntVec3(x, 0, z);
            if (cell.InBounds(map))
                openCells.Add(cell);
        }
        openCells.Shuffle();

        int lootIdx = 0;
        void TrySpawn(ThingDef def, int count)
        {
            if (def == null || lootIdx >= openCells.Count) return;
            Thing t = ThingMaker.MakeThing(def, GenStuff.DefaultStuffFor(def));
            t.stackCount = Mathf.Min(count, def.stackLimit);
            GenSpawn.Spawn(t, openCells[lootIdx++], map);
        }

        // Core drops — always present.
        TrySpawn(ThingDefOf.Silver, Rand.RangeInclusive(30, 120));
        TrySpawn(ThingDefOf.Steel, Rand.RangeInclusive(20, 80));
        TrySpawn(ThingDefOf.MealSurvivalPack, Rand.RangeInclusive(3, 12));
        TrySpawn(ThingDefOf.MedicineHerbal, Rand.RangeInclusive(2, 8));

        // Bonus drops — chance-gated.
        if (Rand.Chance(0.5f)) TrySpawn(ThingDefOf.ComponentIndustrial, Rand.RangeInclusive(2, 6));
        if (Rand.Chance(0.4f)) TrySpawn(ThingDefOf.Gold, Rand.RangeInclusive(5, 25));
        if (Rand.Chance(0.3f)) TrySpawn(ThingDef.Named("MedicineIndustrial"), Rand.RangeInclusive(1, 4));
        if (Rand.Chance(0.2f)) TrySpawn(ThingDefOf.Plasteel, Rand.RangeInclusive(5, 15));
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static ThingDef GetRockWallDef(Map map)
    {
        ThingDef rockType = Find.World.NaturalRockTypesIn(map.Tile).FirstOrDefault();
        return rockType ?? ThingDef.Named("Sandstone");
    }
}
