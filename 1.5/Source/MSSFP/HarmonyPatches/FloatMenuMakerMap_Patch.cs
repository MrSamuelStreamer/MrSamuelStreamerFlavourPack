using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(FloatMenuMakerMap))]
public static class FloatMenuMakerMap_Patch
{
    public static TargetingParameters ForBalloon(Pawn p)
    {
        return new TargetingParameters()
        {
            canTargetPawns = true,
            canTargetMechs = false,
            canTargetBuildings = false,
            onlyTargetIncapacitatedPawns = true,
        };
    }

    [HarmonyPatch("AddDraftedOrders")]
    [HarmonyPostfix]
    public static void AddDraftedOrders_Patch(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts, bool suppressAutoTakeableGoto = false)
    {
        IntVec3 clickCell = IntVec3.FromVector3(clickPos);

        if (!pawn.RaceProps.IsMechanoid && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
        {
            Thing balloon = pawn.inventory.innerContainer.InnerListForReading.FirstOrDefault(thing => thing.def == MSSFPDefOf.MSS_Balloon);
            if (balloon != null)
            {
                foreach (LocalTargetInfo localTargetInfo in GenUI.TargetsAt(clickPos, ForBalloon(pawn), true))
                {
                    if (!pawn.CanReach(localTargetInfo, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        opts.Add(new FloatMenuOption("MSSFP_CannotExtract".Translate((NamedArgument)localTargetInfo.Thing) + ": " + "NoPath".Translate().CapitalizeFirst(), null));
                    }
                    else if (!PawnFlyerBalloon.BedAvailableFor(localTargetInfo.Pawn, out Building_Bed _))
                    {
                        opts.Add(
                            new FloatMenuOption("MSSFP_CannotExtract".Translate((NamedArgument)localTargetInfo.Thing) + ": " + "MSSFP_NoBed".Translate().CapitalizeFirst(), null)
                        );
                    }
                    else
                    {
                        opts.Add(
                            FloatMenuUtility.DecoratePrioritizedTask(
                                new FloatMenuOption(
                                    "MSSFP_Extract".Translate((NamedArgument)localTargetInfo.Thing),
                                    (Action)(
                                        () =>
                                        {
                                            localTargetInfo.Thing.SetForbidden(false, false);
                                            Job job = JobMaker.MakeJob(MSSFPDefOf.MSSFP_ExtractTarget, localTargetInfo);
                                            job.count = 1;
                                            pawn.jobs.TryTakeOrderedJob(job);
                                        }
                                    )
                                ),
                                pawn,
                                localTargetInfo
                            )
                        );
                    }
                }
            }
        }
    }
}
