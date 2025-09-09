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
        
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Alpha5)
        {
            MSSFPMod.settings.Enable10SecondsToSpeed = !MSSFPMod.settings.Enable10SecondsToSpeed;
            Event.current.Use();
        }
    }
}
