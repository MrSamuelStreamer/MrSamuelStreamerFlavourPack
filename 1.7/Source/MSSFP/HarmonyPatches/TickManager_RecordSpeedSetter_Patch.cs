using HarmonyLib;
using Verse;

namespace MSSFP.HarmonyPatches;

// Records every CurTimeSpeed setter call that happens INSIDE
// TimeControls.DoTimeControlsGUI. Used by TimeControls_SubNormalSpeed_Patch to
// distinguish user-driven speed changes (which clear slow level) from
// programmatic ones outside the UI loop (which preserve it).
//
// Note: TogglePaused() mutates the private curTimeSpeed field directly without
// invoking the setter, so Pause clicks/keybinds correctly do NOT trip this flag.
[HarmonyPatch(typeof(TickManager), nameof(TickManager.CurTimeSpeed), MethodType.Setter)]
internal static class TickManager_RecordSpeedSetter_Patch
{
    [HarmonyPrefix]
    private static void Prefix(TimeSpeed value)
    {
        if (!TimeControls_SubNormalSpeed_Patch.InTimeControlsGUI)
            return;
        TimeControls_SubNormalSpeed_Patch.SetterFiredDuringGUI = true;
        TimeControls_SubNormalSpeed_Patch.SetterValue = value;
    }
}
