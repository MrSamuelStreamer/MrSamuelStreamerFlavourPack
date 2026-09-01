using System.Collections.Generic;
using MSSFP.Lockpicking;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Jobs;

/// <summary>
/// Walk to a hostile-faction door, run a Manipulation-scaled progress bar, then
/// permanently mark it lockpicked so player pawns can open it without claiming it.
/// </summary>
public class JobDriver_LockpickDoor : JobDriver
{
    private const TargetIndex DoorInd = TargetIndex.A;

    private Building_Door Door => job.GetTarget(DoorInd).Thing as Building_Door;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return Door != null && pawn.Reserve(Door, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(DoorInd);
        this.FailOn(() => !LockpickUtility.Enabled);
        this.FailOn(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
        this.FailOn(() => Door == null);
        this.FailOn(() => Door != null && !LockpickUtility.IsFactionBlocked(Door, pawn));

        Toil goToDoor = Toils_Goto.GotoThing(DoorInd, PathEndMode.Touch);
        goToDoor.FailOn(() => LockpickUtility.IsPicked(Door));
        yield return goToDoor;

        int workTicks = LockpickUtility.WorkTicksFor(pawn);
        Toil wait = Toils_General.Wait(workTicks, DoorInd);
        wait.WithProgressBarToilDelay(DoorInd);
        wait.activeSkill = () => SkillDefOf.Crafting;
        wait.handlingFacing = true;
        wait.FailOn(() => LockpickUtility.IsPicked(Door));
        yield return wait;

        yield return Toils_General.Do(() => LockpickUtility.ApplySuccess(pawn, Door));
    }
}
