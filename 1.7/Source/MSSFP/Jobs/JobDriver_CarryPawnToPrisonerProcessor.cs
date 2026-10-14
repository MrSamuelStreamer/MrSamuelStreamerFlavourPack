using System.Collections.Generic;
using MSSFP.Comps;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Jobs;

public class JobDriver_CarryPawnToPrisonerProcessor : JobDriver
{
    private const TargetIndex PrisonerInd = TargetIndex.A;
    private const TargetIndex ProcessorInd = TargetIndex.B;

    private Pawn Prisoner => (Pawn)job.GetTarget(PrisonerInd).Thing;
    private Thing Processor => job.GetTarget(ProcessorInd).Thing;
    private CompPrisonerProcessor ProcessorComp => (Processor as ThingWithComps)?.GetComp<CompPrisonerProcessor>();

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(Prisoner, job, 1, -1, null, errorOnFailed)
            && pawn.Reserve(Processor, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(PrisonerInd);
        this.FailOnDestroyedOrNull(ProcessorInd);
        this.FailOn(() => ProcessorComp?.CanAcceptPawn(Prisoner) != true);

        yield return Toils_Goto.GotoThing(PrisonerInd, PathEndMode.Touch)
            .FailOnDespawnedNullOrForbidden(PrisonerInd)
            .FailOnSomeonePhysicallyInteracting(PrisonerInd);
        yield return Toils_Haul.StartCarryThing(PrisonerInd);

        yield return Toils_Goto.GotoThing(ProcessorInd, PathEndMode.InteractionCell).FailOnDespawnedNullOrForbidden(ProcessorInd);

        Toil insert = ToilMaker.MakeToil("InsertPrisonerIntoProcessor");
        insert.initAction = delegate
        {
            CompPrisonerProcessor comp = ProcessorComp;
            if (comp == null || !comp.TryAcceptPawn(Prisoner))
                EndJobWith(JobCondition.Incompletable);
        };
        insert.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return insert;
    }
}
