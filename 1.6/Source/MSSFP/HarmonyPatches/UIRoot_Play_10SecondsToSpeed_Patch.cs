using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(UIRoot), nameof(UIRoot.UIRootOnGUI))]
internal static class UIRoot_10SecondsToSpeed_Patch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        if (Current.ProgramState != ProgramState.Playing)
            return;

        if (DefDatabase<KeyBindingDef>.GetNamed("MSSFP_Toggle10SecondsToSpeed").KeyDownEvent)
        {
            MSSFPMod.settings.Enable10SecondsToSpeed = !MSSFPMod.settings.Enable10SecondsToSpeed;
        }
    }
}
