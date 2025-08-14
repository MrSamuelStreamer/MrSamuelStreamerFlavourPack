using System.Collections.Generic;
using MSSFP.Comps;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Jobs;

public class JobDriver_PutALittleDirt : JobDriver
{
    private const TargetIndex DirtableInd = TargetIndex.A;
    private const TargetIndex FuelInd = TargetIndex.B;
    public const int DirtGatheringDuration = 240;
    protected Thing Dirtable => job.GetTarget(TargetIndex.A).Thing;

    protected CompDirtHaver DirtHaverComp => Dirtable.TryGetComp<CompDirtHaver>();

    protected IntVec3 DirtLoc => job.GetTarget(TargetIndex.B).Cell;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (!MSSFPMod.settings.EnableDirtJobs)
            return false;
        if (
            pawn.CanReserveAndReach(Dirtable, PathEndMode.ClosestTouch, Danger.None)
            && pawn.CanReserveAndReach(DirtLoc, PathEndMode.ClosestTouch, Danger.None)
        )
            return pawn.Reserve((LocalTargetInfo)Dirtable, job, errorOnFailed: errorOnFailed)
                && pawn.Reserve(DirtLoc, job, errorOnFailed: errorOnFailed);
        return false;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        job.count = 1;
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        AddEndCondition(() =>
            !DirtHaverComp.HasDirt ? JobCondition.Ongoing : JobCondition.Succeeded
        );

        Toil reserveDirt = Toils_Reserve.Reserve(TargetIndex.B, 1, -1, null, false);
        yield return reserveDirt;

        yield return Toils_Goto
            .GotoThing(TargetIndex.B, PathEndMode.ClosestTouch, false)
            .FailOnDespawnedNullOrForbidden(TargetIndex.B)
            .FailOnSomeonePhysicallyInteracting(TargetIndex.B);
        yield return Toils_General.Wait(DirtGatheringDuration, TargetIndex.None);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, false);

        Toil dirtTheThing = Toils_General
            .Wait(240, TargetIndex.None)
            .FailOnDestroyedNullOrForbidden(TargetIndex.B)
            .FailOnDestroyedNullOrForbidden(TargetIndex.A)
            .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
            .WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);

        dirtTheThing.AddFinishAction(
            delegate
            {
                DirtHaverComp.Notify_HasDirt();
            }
        );

        yield return dirtTheThing;
    }
}
