using HarmonyLib;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(WorkTypeDef))]
public static class WorkTypeDef_Patch
{
    [HarmonyPatch(nameof(WorkTypeDef.VisibleNow))]
    [HarmonyPostfix]
    public static void VisibleNow_Patch(WorkTypeDef __instance, ref bool __result)
    {
        if (__instance != MSSFPDefOf.MSS_AnomalyPrevention)
            return;

        if (!MSSFPMod.settings.EnableDirtJobs)
            __result = false;
    }
}
