using System;
using MSSFP;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP;

public class FloatMenuOptionProvider_Extract : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool RequiresManipulation => true;

    public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context)
    {
        // Check if the selected pawn has a balloon in their inventory
        var hasBalloon =
            pawn?.inventory?.innerContainer?.Contains(MSSFPDefOf.MSS_FP_Balloon) ?? false;
        return base.SelectedPawnValid(pawn, context) && hasBalloon;
    }

    protected override FloatMenuOption GetSingleOptionFor(
        Pawn clickedPawn,
        FloatMenuContext context
    )
    {
        // Check if the clicked pawn is downed or guilty
        if (!clickedPawn.Downed && !clickedPawn.guilt.IsGuilty)
        {
            return new FloatMenuOption(
                "MSSFP_CannotExtract".Translate((NamedArgument)clickedPawn)
                + ": "
                + "MSSFP_NotDowned".Translate().CapitalizeFirst(),
                null
            );
        }

        if (
            !context.FirstSelectedPawn.CanReach(
                clickedPawn,
                PathEndMode.ClosestTouch,
                Danger.Deadly
            )
        )
        {
            return new FloatMenuOption(
                "MSSFP_CannotExtract".Translate((NamedArgument)clickedPawn)
                    + ": "
                    + "NoPath".Translate().CapitalizeFirst(),
                null
            );
        }

        if (!PawnFlyerBalloon.BedAvailableFor(clickedPawn, out Building_Bed _))
        {
            return new FloatMenuOption(
                "MSSFP_CannotExtract".Translate((NamedArgument)clickedPawn)
                    + ": "
                    + "MSSFP_NoBed".Translate().CapitalizeFirst(),
                null
            );
        }

        return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(
                "MSSFP_Extract".Translate((NamedArgument)clickedPawn),
                (Action)(
                    () =>
                    {
                        clickedPawn.SetForbidden(false, false);
                        Job job = JobMaker.MakeJob(MSSFPDefOf.MSSFP_ExtractTarget, clickedPawn);
                        job.count = 1;
                        context.FirstSelectedPawn.jobs.TryTakeOrderedJob(job);
                    }
                )
            ),
            context.FirstSelectedPawn,
            clickedPawn
        );
    }
}
