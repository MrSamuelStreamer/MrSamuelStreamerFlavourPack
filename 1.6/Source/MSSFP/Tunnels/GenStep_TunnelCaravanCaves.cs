using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.Noise;

namespace MSSFP.Tunnels;

/// <summary>
/// Carves underground cave corridors directly into MapGenerator.Caves so that
/// Underground_RocksFromGrid (order 200) leaves walkable passages.
///
/// WHY THIS EXISTS:
///   pocketMapProperties.tileMutators (including the UndergroundCave mutator that
///   carves corridors via MapGenCavesUtility) are only applied inside
///   MapGenerator.GenerateMap when isPocketMap=true. Caravan attack maps are
///   generated with isPocketMap=false (they sit on a real world tile), so no
///   mutators are ever added to map.TileInfo and MutatorPostElevationFertility
///   does nothing. Without cave carving, MapGenerator.Caves stays all-zero and
///   Underground_RocksFromGrid spawns solid rock on every cell.
///
/// WHAT IT DOES:
///   Replicates TileMutatorWorker_UndergroundCave.GeneratePostElevationFertility:
///   repeatedly calls MapGenCavesUtility.Dig (with UndergroundCave branch/width
///   parameters) until the carved region contains at least MinCaveSize walkable
///   cells. The final MapGenerator.Caves grid is what Underground_RocksFromGrid
///   reads to skip rock placement.
///
/// ORDER: 15 — after ElevationFertility (10), before Underground_RocksFromGrid (200).
/// </summary>
public class GenStep_TunnelCaravanCaves : GenStep
{
    // Mirror TileMutatorWorker_UndergroundCave constants exactly.
    private const float DirectionNoiseFrequency = 0.00205f;
    private const int MinCaveSize = 2000;
    private const int MaxTries = 100;
    private const float MinDigWidth = 3.2f;
    private const float MaxDigWidth = 4f;
    private const float TunnelMinWidth = 1.6f;
    private const float TunnelWidthOffsetPerCell = 0.01f;
    private const float TunnelBranchChance = 0.5f;
    private const int AllowBranchingAfterCells = 10;
    private const float RockElevationThreshold = 0.7f;

    public override int SeedPart => 384750293;

    public override void Generate(Map map, GenStepParams parms)
    {
        MapGenFloatGrid elevation = MapGenerator.Elevation;

        ModuleBase directionNoise = new Perlin(
            DirectionNoiseFrequency, 2.0, 0.5, 4, Rand.Int, QualityMode.Medium);

        MapGenCavesUtility.CaveGenParms caveParms = MapGenCavesUtility.CaveGenParms.Default;
        caveParms.widthOffsetPerCell = TunnelWidthOffsetPerCell;
        caveParms.minTunnelWidth = TunnelMinWidth;
        caveParms.branchChance = TunnelBranchChance;
        caveParms.allowBranchingAfterThisManyCells = AllowBranchingAfterCells;

        HashSet<IntVec3> visited = new HashSet<IntVec3>();
        List<IntVec3> allCells = map.AllCells.ToList();
        int tries = 0;

        // Retry until the carved cave has at least MinCaveSize open cells.
        // Each iteration clears and rebuilds from scratch (mirrors the vanilla logic).
        while (visited.Count < MinCaveSize)
        {
            tries++;
            visited.Clear();
            MapGenerator.Caves.Clear();

            IntVec3 start = allCells.RandomElement();
            float width = Rand.Range(MinDigWidth, MaxDigWidth);

            MapGenCavesUtility.Dig(
                start,
                Rand.Range(0f, 360f),
                width,
                allCells,
                map,
                closed: true,
                directionNoise,
                caveParms,
                isRock: c => c.InBounds(map) && elevation[c] > RockElevationThreshold,
                visited);

            if (tries > MaxTries)
            {
                Log.Error(
                    $"[MSSFP] GenStep_TunnelCaravanCaves: cave generation " +
                    $"exceeded {MaxTries} tries — map may be solid rock");
                return;
            }
        }
    }
}
