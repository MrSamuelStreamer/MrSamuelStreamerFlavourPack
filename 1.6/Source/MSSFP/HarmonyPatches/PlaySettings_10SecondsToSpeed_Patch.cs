using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(PlaySettings), nameof(PlaySettings.DoPlaySettingsGlobalControls))]
internal static class PlaySettings_10SecondsToSpeed_Patch
{
    [HarmonyPostfix]
    private static void Postfix(WidgetRow row, bool worldView)
     {
        if (worldView || !MSSFPMod.settings.Enable10SecondsToSpeed)
            return;

        row.ToggleableIcon(
            ref MSSFPMod.settings.Active10SecondsToSpeed,
            ContentFinder<Texture2D>.Get("UI/MSS_FP_10Seconds", true),
            "MSSFP_10SecondsToSpeed_Tooltip".Translate(),
            SoundDefOf.Mouseover_ButtonToggle,
            null
        );
    }
}
