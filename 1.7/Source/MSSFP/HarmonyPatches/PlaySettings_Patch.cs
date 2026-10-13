using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Add haunts toggle to map toggle icons
/// </summary>
[StaticConstructorOnStartup]
[HarmonyPatch(typeof(PlaySettings))]
public static class PlaySettings_Patch
{
    public static readonly Texture2D ToggleTex = ContentFinder<Texture2D>.Get(
        "UI/MSS_FP_Haunts_Toggle"
    );

    [HarmonyPatch(nameof(PlaySettings.DoPlaySettingsGlobalControls))]
    [HarmonyPostfix]
    public static void DoPlaySettingsGlobalControls_Patch(WidgetRow row)
    {
        row.ToggleableIcon(
            ref MSSFPMod.settings.ShowHaunts,
            ToggleTex,
            "MSS_FP_ToggleHaunts".Translate(),
            SoundDefOf.Mouseover_ButtonToggle
        );
    }
}
