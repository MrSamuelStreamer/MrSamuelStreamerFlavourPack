using System;
using System.Collections.Generic;
using MSSFP.ReversePickpocket;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP;

/// <summary>
/// Right-click "Reverse pick-pocket" on hostile pawns. One option per device the
/// drafted pawn carries: a grenade from a worn grenade belt, or a mortar shell.
/// </summary>
public class FloatMenuOptionProvider_ReversePickpocket : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => false;

    protected override bool Multiselect => false;

    protected override bool RequiresManipulation => true;

    protected override bool MechanoidCanDo => false;

    protected override bool AppliesInt(FloatMenuContext context)
    {
        return ReversePickpocketUtility.Enabled;
    }

    public override bool TargetPawnValid(Pawn pawn, FloatMenuContext context)
    {
        return base.TargetPawnValid(pawn, context)
            && ReversePickpocketUtility.IsValidTarget(context.FirstSelectedPawn, pawn);
    }

    public override IEnumerable<FloatMenuOption> GetOptionsFor(
        Pawn clickedPawn,
        FloatMenuContext context
    )
    {
        Pawn pawn = context.FirstSelectedPawn;
        if (pawn == null)
            yield break;

        bool canReach = pawn.CanReach(clickedPawn, PathEndMode.Touch, Danger.Deadly);
        foreach (ReversePickpocketDevice device in ReversePickpocketUtility.DevicesFor(pawn))
        {
            string label = "MSSFP_ReversePickpocket".Translate(device.Label);
            if (!canReach)
            {
                yield return new FloatMenuOption(
                    label + ": " + "NoPath".Translate().CapitalizeFirst(),
                    null
                );
                continue;
            }

            Thing source = device.Source;
            yield return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(
                    label,
                    (Action)(
                        () =>
                        {
                            Job job = JobMaker.MakeJob(
                                MSSFPDefOf.MSSFP_ReversePickpocket,
                                clickedPawn,
                                source
                            );
                            job.playerForced = true;
                            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        }
                    )
                ),
                pawn,
                clickedPawn
            );
        }
    }
}
