﻿using System.Collections.Generic;
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
    [HarmonyPatch("MakeNewToils", [])]
    [HarmonyPostfix]
    public static void MakeNewToils_PatchPostfix(JobDriver_Lovin __instance, ref IEnumerable<Toil> __result)
    {
        List<Toil> toils = __result.ToList();

        if (toils.NullOrEmpty())
            return;

        Building_Bed bed = (Building_Bed)__instance.job.GetTarget(TargetIndex.B);

        toils
            .GetLast()
            .AddFinishAction(
                delegate
                {
                    CompUpgradableBed comp = bed.GetComp<CompUpgradableBed>();
                    comp?.Notify_GotSomeLovin(bed.CurOccupants.ToList());
                }
            );

        __result = toils;
    }
}
