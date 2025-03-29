using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MSSFP.Comps;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(JobDriver_Lovin))]
public static class JobDriver_Lovin_Patch
{
    public static Dictionary<Pawn, Building_Bed> LovinBeds = new();

    [HarmonyPatch("MakeNewToils", [])]
    [HarmonyPrefix]
    public static void MakeNewToils_PatchPrefix(JobDriver_Lovin __instance)
    {
        LovinBeds[__instance.pawn] = (Building_Bed)__instance.job.GetTarget(TargetIndex.B);
    }

    [HarmonyPatch("MakeNewToils", [])]
    [HarmonyPostfix]
    public static void MakeNewToils_PatchPostfix(JobDriver_Lovin __instance, ref IEnumerable<Toil> __result)
    {
        List<Toil> toils = __result.ToList();

        if (toils.NullOrEmpty())
            return;

        Building_Bed bed = (Building_Bed)__instance.job.GetTarget(TargetIndex.B);

        // something went wrong
        if (LovinBeds.TryGetValue(__instance.pawn, null) == null)
            return;

        toils
            .GetLast()
            .AddFinishAction(
                delegate
                {
                    CompUpgradableBed comp = bed.GetComp<CompUpgradableBed>();
                    comp?.Notify_GotSomeLovin(bed.CurOccupants.ToList());
                    LovinBeds.Remove(__instance.pawn);
                }
            );

        __result = toils;
    }
}
