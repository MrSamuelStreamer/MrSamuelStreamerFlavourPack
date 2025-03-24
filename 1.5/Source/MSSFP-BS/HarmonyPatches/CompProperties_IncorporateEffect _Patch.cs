using BigAndSmall;
using HarmonyLib;

namespace MSSFP.BS.HarmonyPatches;

[HarmonyPatch(typeof(CompProperties_IncorporateEffect))]
public static class CompProperties_IncorporateEffect__Patch
{
    [HarmonyPatch(nameof(CompProperties_IncorporateEffect.RemoveGenesOverLimit))]
    [HarmonyPrefix]
    public static bool RemoveGenesOverLimit(CompProperties_IncorporateEffect __instance, ref bool __result)
    {
        __result = false;
        return !MSSFPMod.settings.DisableBSIncorporateGeneLimit;
    }
}
