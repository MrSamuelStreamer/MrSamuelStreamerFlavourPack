using System.Collections.Generic;
using MSSFP.Hediffs;
using MSSFP.ReversePickpocket;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Jobs;

/// <summary>
/// Walk up to a hostile pawn, spend a short plant wait, then slip a grenade or
/// shell onto them. Always succeeds; nothing is consumed if the job is interrupted.
/// Afterwards the planter sprints clear of the blast radius.
/// </summary>
public class JobDriver_ReversePickpocket : JobDriver
{
    private const TargetIndex VictimInd = TargetIndex.A;
    private const TargetIndex DeviceInd = TargetIndex.B;

    private Pawn Victim => job.GetTarget(VictimInd).Thing as Pawn;

    private Thing Device => job.GetTarget(DeviceInd).Thing;

    // No reservation: several colonists may plant on the same hostile.
    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(VictimInd);
        this.FailOn(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
        this.FailOn(() => !ReversePickpocketUtility.IsValidTarget(pawn, Victim));
        this.FailOn(() => !ReversePickpocketUtility.StillAvailable(pawn, Device));

        yield return Toils_Goto.GotoThing(VictimInd, PathEndMode.Touch);

        Toil plant = Toils_General.Wait(ReversePickpocketUtility.PlantTicks, VictimInd);
        plant.WithProgressBarToilDelay(VictimInd);
        plant.handlingFacing = true;
        yield return plant;

        yield return Toils_General.Do(Plant);
    }

    private void Plant()
    {
        Pawn victim = Victim;
        Thing device = Device;
        ThingDef explosiveDef = ReversePickpocketUtility.ExplosiveDefFor(device);
        if (explosiveDef == null)
            return;

        ReversePickpocketUtility.Consume(device);

        Hediff_ReversePickpocketFuse fuse = (Hediff_ReversePickpocketFuse)
            HediffMaker.MakeHediff(MSSFPDefOf.MSSFP_ReversePickpocketFuse, victim);
        fuse.Arm(explosiveDef, pawn);
        victim.health.AddHediff(fuse);

        Messages.Message(
            "MSSFP_ReversePickpocket_Planted".Translate(
                pawn.LabelShort,
                victim.LabelShort,
                (ReversePickpocketUtility.FuseTicks / GenTicks.TicksPerRealSecond).ToString()
            ),
            new LookTargets(victim),
            MessageTypeDefOf.NeutralEvent,
            false
        );

        ReversePickpocketUtility.QueueFlee(pawn, victim, explosiveDef);
    }
}
