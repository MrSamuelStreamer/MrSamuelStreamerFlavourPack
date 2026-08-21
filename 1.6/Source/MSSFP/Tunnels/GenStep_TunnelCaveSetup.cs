using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Tunnels;

/// <summary>
/// Converts the map to cave terrain: stone floor with thick mountain roofing on all cells.
/// Runs after RocksFromGrid (order 200) and Terrain (order 210) to override the biome
/// surface terrain with an underground cave aesthetic.
/// </summary>
public class GenStep_TunnelCaveSetup : GenStep
{
    public override int SeedPart => 938471234;

    public override void Generate(Map map, GenStepParams parms)
    {
        TerrainDef caveFloor = ResolveCaveFloorTerrain(map);

        foreach (IntVec3 cell in map.AllCells)
        {
            map.terrainGrid.SetTerrain(cell, caveFloor);
            map.roofGrid.SetRoof(cell, RoofDefOf.RoofRockThick);
        }
    }

    /// <summary>
    /// Picks the rough stone floor terrain matching the tile's natural rock type.
    /// e.g. Granite → Granite_Rough, Sandstone → Sandstone_Rough.
    /// Falls back to Gravel if the lookup fails.
    /// </summary>
    private static TerrainDef ResolveCaveFloorTerrain(Map map)
    {
        ThingDef rockType = Find.World.NaturalRockTypesIn(map.Tile).FirstOrDefault();
        if (rockType != null)
        {
            TerrainDef roughTerrain = DefDatabase<TerrainDef>.GetNamedSilentFail(rockType.defName + "_Rough");
            if (roughTerrain != null)
                return roughTerrain;
        }
        return TerrainDefOf.Gravel;
    }
}
