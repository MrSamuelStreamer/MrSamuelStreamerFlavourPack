using System;
using LudeonTK;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Tunnels.DebugActions;

/// <summary>
/// Dev-mode escape hatch for saves whose world was generated before the tunnel
/// system was enabled in settings/scenario. Retro-enabling here does not
/// retroactively fix the vanilla-parity guarantee that a save's tunnel state is
/// locked at world-gen (see <see cref="TunnelGenData.wasSet"/>) — it is a manual
/// override for testing/recovery, not a supported gameplay path.
/// </summary>
public static class DebugActions_Tunnels
{
    private const string Category = "MSSFP - Tunnels";

    [DebugAction(Category, "Retro-enable tunnels for this save", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnWorld)]
    public static void RetroEnableTunnels()
    {
        TunnelGenData comp = Find.World?.GetComponent<TunnelGenData>();
        if (comp == null)
        {
            Log.Warning("MSSFP: no world loaded");
            return;
        }

        comp.tunnelsEnabledForSave = true;
        comp.wasSet = true;
        TunnelUtilities.RefreshCache();

        Log.Message("MSSFP: tunnels retro-enabled. Running post-hoc world-gen steps...");
        try
        {
            PlanetLayer layer = Find.WorldGrid.Surface;
            new WorldGenStep_TunnelEntrances().GenerateFresh("MSSFP retro-enable", layer);
            new WorldGenStep_Tunnels().GenerateFresh("MSSFP retro-enable", layer);
            Log.Message("MSSFP: tunnels retro-enable complete.");
        }
        catch (Exception ex)
        {
            Log.Error($"MSSFP: retro-enable failed - {ex}");
        }
    }
}
