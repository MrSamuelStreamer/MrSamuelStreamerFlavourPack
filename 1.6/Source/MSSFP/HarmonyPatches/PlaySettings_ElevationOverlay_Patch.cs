using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
internal static class PlaySettings_ElevationOverlay_Patch
{
    private static readonly Texture2D ElevationTex = ContentFinder<Texture2D>.Get("UI/MSS_FP_Elevation_Toggle", true);

    [HarmonyPostfix]
    private static void Postfix(WidgetRow row, bool worldView)
    {
        if (!worldView)
            return;

        row.ToggleableIcon(
            ref MSSFPMod.settings.ShowElevationOverlay,
            ElevationTex,
            "MSSFP_ElevationOverlay_Tooltip".Translate(),
            SoundDefOf.Mouseover_ButtonToggle,
            null
        );
    }
}
