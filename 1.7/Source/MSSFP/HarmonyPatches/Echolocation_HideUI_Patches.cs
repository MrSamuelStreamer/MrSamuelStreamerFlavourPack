using HarmonyLib;
using MSSFP.Comps.Map;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Echolocation blackout: hide designation icons (red X + stack/yield
/// numbers) and item-stack-count GUI overlays while the effect is active
/// on the viewed map — they render on layers/passes our own blackout
/// (world-space MapComponentDraw) doesn't touch.
/// </summary>
[HarmonyPatch(typeof(DesignationManager), nameof(DesignationManager.DrawDesignations))]
public static class DesignationManager_DrawDesignations_HidePatch
{
    [HarmonyPrefix]
    public static bool Prefix(Map ___map)
    {
        return ___map?.GetComponent<EcholocationMapComponent>()?.Active != true;
    }
}

[HarmonyPatch(typeof(ThingOverlays), nameof(ThingOverlays.ThingOverlaysOnGUI))]
public static class ThingOverlays_ThingOverlaysOnGUI_HidePatch
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        return Find.CurrentMap?.GetComponent<EcholocationMapComponent>()?.Active != true;
    }
}
