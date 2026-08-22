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
            Textures.Icon,
            "MSSFP_10SecondsToSpeed_Tooltip".Translate(),
            SoundDefOf.Mouseover_ButtonToggle,
            null
        );
    }

    /// <summary>Loads once on the main thread and is cached for the process lifetime,
    /// avoiding a ContentFinder lookup (which walks the running-mods list) on every
    /// DoPlaySettingsGlobalControls call.</summary>
    [StaticConstructorOnStartup]
    private static class Textures
    {
        public static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("UI/MSS_FP_10Seconds", true);
    }
}
