using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(Ability))]
public static class AbilityPatches
{
    [HarmonyPatch(nameof(Ability.GetGizmos))]
    [HarmonyPostfix]
    public static void GetGizmos_Postfix(Ability __instance, ref IEnumerable<Gizmo> __result)
    {
        if (__instance.def != MSSFPDefOf.MSS_FP_WorldLeap) return;

        if (!MSSFPDefOf.MSS_FroggeLeapResearch.IsFinished)
        {
            __result = [];
        }
    }
}
