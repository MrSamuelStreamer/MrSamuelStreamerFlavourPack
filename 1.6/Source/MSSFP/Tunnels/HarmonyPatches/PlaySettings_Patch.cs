using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Tunnels.HarmonyPatches;

/// <summary>
/// Injects the world-view toggle button that shows/hides the tunnel network overlay
/// (<see cref="WorldDrawLayer_Tunnels.TunnelsVisible"/>).
///
/// Gated on <see cref="TunnelUtilities.IsEnabled"/>: when tunnels are disabled globally
/// or for the current save, the toggle button is omitted entirely rather than shown
/// disabled — there is nothing for it to control.
/// </summary>
[StaticConstructorOnStartup]
[HarmonyPatch(typeof(PlaySettings))]
public static class PlaySettings_Patch
{
    public static readonly Texture2D ToggleTex = ContentFinder<Texture2D>.Get(
        "UI/MSS_TunnelIcon"
    );

    [HarmonyPatch(nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    [HarmonyPostfix]
    public static void DoPlaySettingsGlobalControls_Patch(WidgetRow row, bool worldView)
    {
        if (!TunnelUtilities.IsEnabled()) return;

        if (worldView)
            row.ToggleableIcon(ref WorldDrawLayer_Tunnels.TunnelsVisible, ToggleTex, "Show/Hide tunnels");
    }
}
