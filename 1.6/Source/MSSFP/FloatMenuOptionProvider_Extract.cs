using System;
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
        return base.SelectedPawnValid(pawn, context) && (pawn.Downed || (pawn.guilt?.IsGuilty ?? false));
    }

    protected override FloatMenuOption GetSingleOptionFor(
        Pawn clickedPawn,
        FloatMenuContext context
    )
    {
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
