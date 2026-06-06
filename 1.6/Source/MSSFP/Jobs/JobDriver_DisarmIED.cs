using System.Collections.Generic;
using MSSFP.Things;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Jobs;

/// <summary>
/// Pawn walks to a cell adjacent to a hostile MSSFP IED, performs a timed disarm
/// with progress bar, then <see cref="Building_IEDTrap.ResolveDisarm"/> rolls the
/// outcome (success / instant spring / delayed fuse). Adjacency matters — an
/// instant spring catches the disarmer via blast radius, not via touch-trigger,
/// which gives delayed-fuse outcomes a chance to be played out via player input.
/// </summary>
public class JobDriver_DisarmIED : JobDriver
{
    private const TargetIndex TrapInd = TargetIndex.A;

    private Building_IEDTrap Trap => (Building_IEDTrap)job.GetTarget(TrapInd).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(Trap, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        // Single gate on the trap target — if it's gone, the designation was lifted,
        // or the pawn lost its skill, end cleanly. Job-level FailOn fires before any
        // toil runs each tick, so this guards the whole driver.
        this.FailOnDespawnedNullOrForbidden(TrapInd);
        this.FailOn(() => !Building_IEDTrap.CanDisarm(pawn));
        this.FailOn(() => Trap.Faction == Faction.OfPlayer);

        yield return Toils_Goto.GotoThing(TrapInd, PathEndMode.Touch);

        int workTicks = Building_IEDTrap.WorkTicksFor(pawn);
        Toil wait = Toils_General.Wait(workTicks, TrapInd);
        wait.WithProgressBarToilDelay(TrapInd);
        wait.activeSkill = () => SkillDefOf.Intellectual;
        wait.handlingFacing = true;
        yield return wait;

        yield return Toils_General.Do(() => Building_IEDTrap.ResolveDisarm(pawn, Trap));
    }
}
