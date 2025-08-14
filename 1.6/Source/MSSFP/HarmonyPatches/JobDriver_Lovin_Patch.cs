using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MSSFP.Comps;
using MSSFP.Comps.Map;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(JobDriver_Lovin))]
public static class JobDriver_Lovin_Patch
{
    [HarmonyPatch("MakeNewToils", [])]
    [HarmonyPostfix]
    public static void MakeNewToils_PatchPostfix(
        JobDriver_Lovin __instance,
        ref IEnumerable<Toil> __result
    )
    {
        List<Toil> toils = __result.ToList();

        if (toils.NullOrEmpty())
            return;

        Building_Bed bed = (Building_Bed)__instance.job.GetTarget(TargetIndex.B);
        Toil lastToil = toils.GetLast();

        lastToil.AddFinishAction(
            delegate
            {
                CompUpgradableBed comp = bed.GetComp<CompUpgradableBed>();
                comp?.Notify_GotSomeLovin(bed.CurOccupants.ToList());
            }
        );

        lastToil.AddFinishAction(
            delegate
            {
                foreach (Pawn pawn in bed.CurOccupants)
                {
                    if (pawn.story.AllBackstories.Any(bs => bs == MSSFPDefOf.MSSFP_Trek))
                    {
                        if (Rand.Chance(0.25f))
                        {
                            pawn.Map.GetComponent<TrekBeamerMapComponent>()?.BeamAwayPawn(pawn);
                        }
                    }
                }
            }
        );

        __result = toils;
    }
}
