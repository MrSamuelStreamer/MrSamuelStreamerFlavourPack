using System.Collections.Generic;
using LudeonTK;
using RimWorld.Planet;
using Verse;
using MSSFP.Comps.World;
using RimWorld;
using UnityEngine;

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
    }

}
