using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace MSSFP.Tunnels;

public class JobDriver_EnterTunnel : JobDriver
{
    private TargetIndex TunnelInd = TargetIndex.A;
    private const int EnterDelay = 90;

    public Building_Tunnel Tunnel => TargetThingA as Building_Tunnel;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TunnelInd);
        this.FailOn(() => !((Building_Tunnel)TargetThingA).IsEnterable(out string text));
        yield return Toils_Goto.GotoThing(TunnelInd, PathEndMode.Touch);
        Toil toil = Toils_General.Wait(90).FailOnCannotTouch(TunnelInd, PathEndMode.Touch).WithProgressBarToilDelay(TunnelInd, true);
        Toil toil2 = toil;
        toil2.tickIntervalAction = (Action<int>)Delegate.Combine(toil2.tickIntervalAction, new Action<int>(delegate
        {
            pawn.rotationTracker.FaceTarget(TargetA);
        }));
        toil.handlingFacing = true;
        yield return toil;
        Toil toil3 = ToilMaker.MakeToil();
        toil3.initAction = delegate
        {
            Building_Tunnel tunnel = TargetThingA as Building_Tunnel;
            if (tunnel == null)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            pawn.DeSpawn();
            if (pawn.carryTracker.CarriedThing != null)
            {
                Thing carriedThing = pawn.carryTracker.CarriedThing;
                if (!tunnel.TryAcceptThing(carriedThing))
                {
                    Log.Error("Could not add carried thing " + carriedThing + " to tunnel caravan container for " + tunnel + ". Attempting to respawn near tunnel.");
                    GenSpawn.Spawn(carriedThing, tunnel.Position, tunnel.Map);
                }
            }
            if (!tunnel.TryAcceptThing(pawn))
            {
                Log.Error("Could not add pawn " + pawn + " to tunnel caravan container for " + tunnel + ". Attempting to respawn near tunnel.");
                GenSpawn.Spawn(pawn, tunnel.Position, tunnel.Map);
                EndJobWith(JobCondition.Incompletable);
            }
        };
        yield return toil3;
    }
}
