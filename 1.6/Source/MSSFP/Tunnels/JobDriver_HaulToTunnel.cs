using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace MSSFP.Tunnels;

public class JobDriver_HaulToTunnel : JobDriver_HaulToContainer
{
    private const int DepositDuration = 90;
    public int initialCount;

    public Building_Tunnel Tunnel => Container as Building_Tunnel;

    protected override int Duration => 90;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref initialCount, "initialCount");
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.A), job);
        pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job);
        return true;
    }

    public override void Notify_Starting()
    {
        base.Notify_Starting();
        ThingCount thingCount = !job.targetA.IsValid ? TunnelUtilities.FindThingToLoad(pawn, Tunnel) : new ThingCount(job.targetA.Thing, job.targetA.Thing.stackCount);
        if (job.playerForced && pawn.carryTracker.CarriedThing != null && pawn.carryTracker.CarriedThing != thingCount.Thing)
            pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out Thing _);
        job.targetA = (LocalTargetInfo)thingCount.Thing;
        job.count = thingCount.Count;
        initialCount = thingCount.Count;
        pawn.Reserve((LocalTargetInfo)thingCount.Thing, job);
    }
}
