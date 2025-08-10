using HarmonyLib;
using MSSFP.Hediffs;
using RimWorld;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Aptitudes from haunts - currently not working
/// </summary>
[HarmonyPatch(typeof(SkillRecord))]
public static class SkillRecord_Patch
{
    [HarmonyPatch(nameof(SkillRecord.Aptitude), MethodType.Getter)]
    [HarmonyPostfix]
    public static void AptitudeFor(SkillRecord __instance, ref int __result)
    {
        __result += HauntsCache.BoostForPawnAndSkill(__instance.Pawn, __instance.def);
    }
}
