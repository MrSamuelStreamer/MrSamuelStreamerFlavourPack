using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Tunnels;

public class JobDriver_TakeAndEnterTunnel : JobDriver_EnterTunnel
{
    private Thing ThingToTake => job.GetTarget(TargetIndex.B).Thing;
    private Pawn PawnToTake => ThingToTake as Pawn;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(ThingToTake, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.B);
        this.FailOn(() => PawnToTake is { Downed: false } && PawnToTake.Awake());
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
        yield return Toils_Construct.UninstallIfMinifiable(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
        yield return Toils_Haul.StartCarryThing(TargetIndex.B);
        foreach (Toil toil in base.MakeNewToils())
        {
            yield return toil;
        }
    }

    private const TargetIndex ThingInd = TargetIndex.B;
}
