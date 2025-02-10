using HarmonyLib;
using RimWorld;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(JobDriver_Reading))]
public static class JobDriver_Reading_Patches
{
    [HarmonyPatch(nameof(JobDriver_Reading.TryMakePreToilReservations))]
    [HarmonyPrefix]
    public static bool JobDriver_TryMakePreToilReservations(JobDriver_Reading __instance, ref bool __result)
    {
        if (__instance.pawn.genes.HasActiveGene(MSSFPDefOf.MSSFP_Illiterate))
        {
            __result = false;
            return false;
        }

        return true;
    }

}
