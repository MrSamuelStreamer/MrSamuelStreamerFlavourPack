using HarmonyLib;
using MSSFP.Hediffs;
using RimWorld;
using UnityEngine;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Applies haunt skill boosts via the cache rather than direct levelInt mutation.
/// This is the sole mechanism for haunt skill boosts — no direct Level/levelInt
/// mutation should exist anywhere in the haunt system.
/// </summary>
[HarmonyPatch(typeof(SkillRecord))]
public static class SkillRecord_Patch
{
    [HarmonyPatch(nameof(SkillRecord.GetLevel))]
    [HarmonyPostfix]
    public static void GetLevel_Postfix(SkillRecord __instance, ref int __result)
    {
        int boost = HauntsCache.BoostForPawnAndSkill(__instance.Pawn, __instance.def);
        if (boost != 0)
            __result = Mathf.Clamp(__result + boost, 0, 20);
    }

    [HarmonyPatch(nameof(SkillRecord.GetLevelForUI))]
    [HarmonyPostfix]
    public static void GetLevelForUI_Postfix(SkillRecord __instance, ref int __result)
    {
        int boost = HauntsCache.BoostForPawnAndSkill(__instance.Pawn, __instance.def);
        if (boost != 0)
            __result = Mathf.Clamp(__result + boost, 0, 20);
    }
}
