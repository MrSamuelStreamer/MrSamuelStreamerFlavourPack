using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Jobs;

public class JobDriver_Extract : JobDriver
{
    private const TargetIndex TakeeIndex = TargetIndex.A;

    protected Pawn Takee => (Pawn)job.GetTarget(TargetIndex.A).Thing;

    public Building_Bed bed;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (
            pawn.Reserve((LocalTargetInfo)(Thing)Takee, job, errorOnFailed: errorOnFailed)
            && PawnFlyerBalloon.BedAvailableFor(Takee, out bed)
        )
        {
            Takee.Reserve(bed, job);
            return true;
        }
        return false;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOnAggroMentalStateAndHostile(TargetIndex.A);
        yield return Toils_Goto
            .GotoThing(TargetIndex.A, PathEndMode.ClosestTouch, false)
            .FailOnDespawnedNullOrForbidden(TargetIndex.A)
            .FailOn(() => !Takee.Downed)
            .FailOnSomeonePhysicallyInteracting(TargetIndex.A);

        Toil toil = ExtractThing(TargetIndex.A);
        toil.AddPreInitAction(CheckMakeTakeeGuest);
        yield return toil;
    }

    public Toil ExtractThing(TargetIndex haulableInd)
    {
        Toil toil = ToilMaker.MakeToil(nameof(ExtractThing));
        toil.initAction = (Action)(
            () =>
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                Pawn thing = curJob.GetTarget(haulableInd).Pawn;

                Map map = thing.Map;
                IntVec3 Position = thing.Position;
                IntVec3 PositionOrig = Position;
                Position.z = map.info.Size.z - 1;

                PawnFlyerBalloon newThing = (PawnFlyerBalloon)
                    PawnFlyer.MakeFlyer(
                        MSSFPDefOf.MSS_PawnFlyer_Balloon,
                        thing,
                        Position,
                        null,
                        null
                    );
                if (newThing == null)
                    return;
                newThing.SetBed(bed);
                GenSpawn.Spawn(newThing, PositionOrig, map);

                actor.inventory.RemoveCount(MSSFPDefOf.MSS_Balloon, 1);
                ;
            }
        );
        return toil;
    }

    private void CheckMakeTakeeGuest()
    {
        if (Takee.HostileTo(Faction.OfPlayer))
        {
            Takee.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
            return;
        }

        if (
            job.def.makeTargetPrisoner
            || Takee.HostileTo(Faction.OfPlayer)
            || Takee.Faction == Faction.OfPlayer
            || Takee.HostFaction == Faction.OfPlayer
            || Takee.guest == null
            || Takee.IsWildMan()
        )
            return;
        Takee.guest.SetGuestStatus(Faction.OfPlayer);
    }
}
