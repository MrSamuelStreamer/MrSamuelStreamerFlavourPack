using System;
using System.Linq;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.DebugActions;

public static class DebugActions_ClearLandmarks
{
    private const string Category = "Landmarks";

    [DebugAction(Category, "Clear All Landmarks", allowedGameStates = AllowedGameStates.PlayingOnWorld)]
    public static void ClearLandmarks()
    {
        foreach (Tile tile in Find.WorldGrid.Surface.Tiles)
        {
            Find.World.landmarks[tile.tile] = null;
        }
    }

    [DebugAction(Category, "Clear All Features", allowedGameStates = AllowedGameStates.PlayingOnWorld)]
    public static void ClearFeatures()
    {
        foreach (Tile tile in Find.WorldGrid.Surface.Tiles)
        {
            tile.feature = null;
        }
        foreach (FeatureDef featureDef in DefDatabase<FeatureDef>.AllDefsListForReading.OrderBy(x => x.order).ThenBy(x => x.index))
        {
            try
            {
                featureDef.Worker.GenerateWhereAppropriate(Find.WorldGrid.Surface);
            }
            catch (Exception ex)
            {
                Log.Error($"Could not generate world features of def {featureDef}: {ex}");
            }
        }
    }

}
