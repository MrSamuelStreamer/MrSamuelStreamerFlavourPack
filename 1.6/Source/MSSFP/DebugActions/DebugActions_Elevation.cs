using System.Collections.Generic;
using LudeonTK;
using RimWorld.Planet;
using Verse;
using MSSFP.Comps.World;
using RimWorld;

namespace MSSFP.DebugActions;

public static class DebugActions_Elevation
{
    private const string Category = "Elevation Painting";

    [DebugAction(Category, "Paint Elevation +250 (Single)", actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.Playing)]
    public static void PaintElevationPlus100Single() => ChangeElevation(250f, 0);

    [DebugAction(Category, "Paint Elevation -250 (Single)", actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.Playing)]
    public static void PaintElevationMinus100Single() => ChangeElevation(-250f, 0);

    [DebugAction(Category, "Paint Elevation +250 (Radius 5)", actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.Playing)]
    public static void PaintElevationPlus100Radius5() => ChangeElevation(250f, 5);

    [DebugAction(Category, "Paint Elevation -250 (Radius 5)", actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.Playing)]
    public static void PaintElevationMinus100Radius5() => ChangeElevation(-250f, 5);

    [DebugAction(Category, "Paint Elevation +250 (Radius 10)", actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.Playing)]
    public static void PaintElevationPlus100Radius10() => ChangeElevation(250f, 10);

    [DebugAction(Category, "Paint Elevation -250 (Radius 10)", actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.Playing)]
    public static void PaintElevationMinus100Radius10() => ChangeElevation(-250f, 10);

    [DebugAction(Category, "Reset Elevation to 0 (Single)", actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.Playing)]
    public static void ResetElevationSingle() => ResetElevation(0);

    [DebugAction(Category, "Reset Elevation to 0 (Radius 5)", actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.Playing)]
    public static void ResetElevationRadius5() => ResetElevation(5);

    [DebugAction(Category, "Reset Elevation to 0 (Radius 10)", actionType = DebugActionType.ToolWorld, allowedGameStates = AllowedGameStates.Playing)]
    public static void ResetElevationRadius10() => ResetElevation(10);

    private static void ResetElevation(int radius)
    {
        PlanetTile mouseTile = GenWorld.MouseTile();

        if (mouseTile < 0) return;

        WorldGrid grid = Find.WorldGrid;
        if (radius <= 0)
        {
            ApplyElevationReset(grid, mouseTile);
        }
        else
        {
            HashSet<int> tiles = GetTilesInRadius(grid, mouseTile, radius);
            foreach (int tId in tiles)
            {
                ApplyElevationReset(grid, tId);
            }
        }

        Find.World.renderer.GetLayer<WorldDrawLayer_ElevationOverlay>(grid.Surface).SetDirty();
    }

    private static HashSet<int> GetTilesInRadius(WorldGrid grid, int mouseTile, int radius)
    {
        HashSet<int> tiles = new HashSet<int> { mouseTile };
        List<int> currentEdge = new List<int> { mouseTile };
        List<PlanetTile> neighbors = new List<PlanetTile>();
        for (int i = 0; i < radius; i++)
        {
            List<int> nextEdge = new List<int>();
            foreach (int t in currentEdge)
            {
                neighbors.Clear();
                grid.GetTileNeighbors(t, neighbors);
                foreach (PlanetTile n in neighbors)
                {
                    if (tiles.Add(n.tileId))
                    {
                        nextEdge.Add(n.tileId);
                    }
                }
            }
            currentEdge = nextEdge;
        }
        return tiles;
    }

    private static void ChangeElevation(float amount, int radius)
    {
        PlanetTile mouseTile = GenWorld.MouseTile();

        if (mouseTile < 0) return;

        WorldGrid grid = Find.WorldGrid;
        if (radius <= 0)
        {
            ApplyElevationChange(grid, mouseTile, amount);
        }
        else
        {
            HashSet<int> tiles = GetTilesInRadius(grid, mouseTile, radius);
            foreach (int tId in tiles)
            {
                ApplyElevationChange(grid, tId, amount);
            }
        }

        Find.World.renderer.GetLayer<WorldDrawLayer_ElevationOverlay>(grid.Surface).SetDirty();
    }

    private static void ApplyElevationReset(WorldGrid grid, int tileId)
    {
        Tile tile = grid[tileId];
        tile.elevation = 0f;
        SetTile(grid, tileId, tile);
    }

    private static void ApplyElevationChange(WorldGrid grid, int tileId, float amount)
    {
        Tile tile = grid[tileId];
        tile.elevation += amount;
        SetTile(grid, tileId, tile);
    }

    private static void SetTile(WorldGrid grid, int tileId, Tile tile)
    {
        // Use reflection to set the tile back as the indexer might be read-only
        var field = typeof(WorldGrid).GetField("tiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            var tilesArray = (Tile[])field.GetValue(grid);
            tilesArray[tileId] = tile;
        }
    }
}
