using System.Collections.Generic;
using Maux36.Rimbody;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MSSFP.RB.JobDrivers;

/// <summary>
/// Lift another colonist as a free weight. Modelled directly on Rimbody's
/// <c>JobDriver_DoChunkLifting</c> so muscle gain, fatigue, workout memory and joy all
/// behave identically to lifting a stone chunk.
///
/// TARGET VALIDITY IS CHECKED BEFORE PICKUP ONLY. <c>StartCarryThing</c> despawns
/// TargetIndex.A, so any despawn- or forbidden-sensitive condition attached job-wide would
/// fire the instant the lift begins and the workout would never complete a single rep.
/// Everything after pickup keys on the liftee's own state instead.
///
/// A CARRIED PAWN IS STILL TICK-LIVE. <c>ThingOwner.ThingOwnerTick</c> keeps running the
/// held pawn's needs, mental breaker, pregnancy and hediffs while <c>Spawned</c> is false
/// and <c>Map</c> is null, so a mental break or a player draft order can land on a pawn the
/// game believes is nowhere. Both cases end the job — dropping the liftee first.
/// </summary>
public class JobDriver_DoPawnLift : JobDriver_RimbodyBaseDriver
{
    private const int Duration = 800;

    /// <summary>Body angle that reads as "lying on their side" while being hoisted.</summary>
    private const float HorizontalBodyAngle = 90f;

    /// <summary>Rimbody treats any negative angle as "no override, render normally".</summary>
    private const float NoBodyAngleOverride = -1f;

    private Vector3 itemOffset = new(0f, 1f / 26f, 0f);

    private Pawn Liftee => job.targetA.Thing as Pawn;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref joygainfactor, "pawnlift_joygainfactor", 1.0f);
        Scribe_Values.Look(ref tickProgress, "pawnlift_tickProgress", 0);
        Scribe_Values.Look(ref memoryFactor, "pawnlift_memoryFactor", 1.0f);
        Scribe_Values.Look(ref workoutEfficiencyValue, "pawnlift_workoutEfficiencyValue", 1.0f);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        CompPhysique compPhysique = pawn.compPhysique();

        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.AddEndCondition(() => Rimbody_Utility.TooTired(pawn) ? JobCondition.InterruptForced : JobCondition.Ongoing);
        this.AddEndCondition(() => compPhysique.gain >= compPhysique.gainMax ? JobCondition.InterruptForced : JobCondition.Ongoing);

        // The liftee cannot run its own think tree while held, so these are the only ways
        // it can signal that it needs to be back on the ground.
        this.AddEndCondition(() =>
        {
            Pawn liftee = Liftee;
            if (liftee == null || liftee.Dead)
                return JobCondition.Incompletable;

            return liftee.Drafted || liftee.InMentalState ? JobCondition.InterruptForced : JobCondition.Ongoing;
        });

        RimbodyDB.JobModExDB.TryGetValue(job.def.shortHash, out ModExtensionRimbodyJob exWorkout);
        memoryFactor = compPhysique.InMemory(exWorkout.id) ? 0.9f : 1f;

        yield return Toils_General.DoAtomic(delegate
        {
            job.count = 1;
            job.SetTarget(TargetIndex.B, TargetLocA);

            // Heavier partner, harder set. The def's strength value is a baseline for an
            // average-sized adult; the base driver multiplies it by this factor.
            Pawn liftee = Liftee;
            float lifterSize = pawn.BodySize <= 0f ? 1f : pawn.BodySize;
            workoutEfficiencyValue = liftee == null ? 1f : Mathf.Clamp(liftee.BodySize / lifterSize, 0.6f, 1.6f);
        });

        // Everything despawn-sensitive belongs here, before the liftee is picked up.
        yield return Toils_Goto
            .GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
            .FailOnDespawnedNullOrForbidden(TargetIndex.A)
            .FailOnSomeonePhysicallyInteracting(TargetIndex.A);

        yield return Toils_Haul.StartCarryThing(TargetIndex.A).FailOnDestroyedNullOrForbidden(TargetIndex.A);
        // Rimbody's own Toils_Rimbody.GotoSpotToWorkout is internal, so walk back to the
        // pickup cell with the vanilla toil. Same destination Rimbody's chunk lifting uses:
        // the cell the load was standing in when the lifter reached it.
        yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);

        Toil workout = ToilMaker.MakeToil(nameof(MakeNewToils));
        workout.initAction = () =>
        {
            pawn.pather.StopDead();
            pawn.rotationTracker.FaceCell(pawn.Position + new IntVec3(0, 0, -1));
            AdjustJoygainFactor();
            StartWorkoutJob(compPhysique, exWorkout);
            SetLifteeBodyAngle(HorizontalBodyAngle);
            GiveLifteeThought();
        };

        float uptime = 0.95f - (0.008f * compPhysique.MuscleMass);
        float cycleDuration = 150f - compPhysique.MuscleMass;
        float jiggleAmount = 0.03f * (1f - (compPhysique.MuscleMass / 50f));

        workout.tickAction = delegate
        {
            tickProgress++;
            float cycleTime = (tickProgress % (int)cycleDuration) / cycleDuration;
            float nudgeMultiplier = cycleTime < uptime
                ? Mathf.Lerp(0.3f, 0f, cycleTime / uptime)
                : Mathf.Lerp(0f, 0.3f, (cycleTime - uptime) / (1f - uptime));

            itemOffset.x = Rand.Range(-jiggleAmount, jiggleAmount);
            itemOffset.z = nudgeMultiplier;
            pawn.needs?.joy?.GainJoy(1.0f * joygainfactor * 0.36f / 2500f, DefOf_Rimbody.Rimbody_WorkoutJoy);
        };

        workout.handlingFacing = true;
        workout.defaultCompleteMode = ToilCompleteMode.Delay;
        workout.defaultDuration = Duration;
        workout.AddFinishAction(delegate
        {
            FinishWorkout(compPhysique);
            Rimbody_Utility.AddMemory(compPhysique, RimbodyWorkoutCategory.Strength, exWorkout.id);
            DropLiftee();
        });

        yield return workout;
    }

    /// <summary>
    /// Puts the liftee back on the ground. Called from the workout's finish action, which
    /// runs on every job end — completion, interruption, and failure alike.
    /// </summary>
    private void DropLiftee()
    {
        // Clear the pose FIRST and unconditionally. pawnBodyAngleOverride is scribed with
        // the save, so a colonist left with it set stays rotated forever — through saves,
        // through this mod being removed. It has to be cleared even on paths where the
        // carry already ended some other way.
        SetLifteeBodyAngle(NoBodyAngleOverride);

        if (pawn.carryTracker?.CarriedThing is not Pawn)
            return;

        pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
    }

    /// <summary>
    /// Rotates the carried pawn horizontal.
    ///
    /// Goes through Rimbody's own <c>pawnBodyAngleOverride</c> rather than a second
    /// PawnRenderer patch: Rimbody already prefixes <c>PawnRenderer.BodyAngle</c> and
    /// returns this value whenever it is non-negative, so patching the renderer ourselves
    /// would mean two mods fighting over one return value.
    ///
    /// The angle is only half the pose — it spins the body while the renderer still lays the
    /// head out as if the pawn were upright, which reads as a rotation about the wrong axis.
    /// The other half is the posture, supplied by
    /// <see cref="MSSFP.RB.HarmonyPatches.PawnUtility_GetPosture_PawnLift_Patch"/>.
    /// </summary>
    private void SetLifteeBodyAngle(float angle)
    {
        Pawn liftee = Liftee;
        if (liftee == null)
            return;

        liftee.SetPawnBodyAngleOverride(angle);
    }

    private void GiveLifteeThought()
    {
        if (!PawnLiftSettingsTab.LifteeThought)
            return;

        Pawn liftee = Liftee;
        if (liftee?.needs?.mood?.thoughts?.memories == null)
            return;
        if (!liftee.RaceProps.Humanlike || liftee.Downed)
            return;

        liftee.needs.mood.thoughts.memories.TryGainMemory(DefOf_MSSFPRB.MSSFP_LiftedAsWeight, pawn);
    }

    public override void Notify_PatherFailed()
    {
        DropLiftee();
        base.Notify_PatherFailed();
    }

    public override bool ModifyCarriedThingDrawPos(ref Vector3 drawPos, ref bool flip)
    {
        if (tickProgress <= 0)
            return false;

        drawPos += itemOffset;
        return true;
    }
}
