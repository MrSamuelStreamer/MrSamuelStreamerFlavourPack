using System.Collections.Generic;
using HarmonyLib;
using Maux36.Rimbody;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MSSFP.RB.HarmonyPatches;

/// <summary>
/// Offers "lift another colonist" as a Rimbody strength workout for pawns strong enough
/// to make it plausible.
///
/// PATCHES THE STATIC GIVER, NOT THE THINK TREE. Rimbody wires
/// <c>JobGiver_DoStrengthBuilding</c> into five places — the MainColonistBehaviorCore
/// patch, the Humanlike prisoner branch, the Humanlike idle branch, its own
/// Rimbody_Workout / Rimbody_ContinueWorkout ThinkTreeDefs, and a delegate array in
/// JoyGiver_Workout. One postfix on <c>TryGiveJobActual</c> covers all of them, and cannot
/// drift out of sync with Rimbody's think-tree XML the way a parallel set of patch
/// operations would.
///
/// IT COMPETES, IT DOES NOT PRE-EMPT. The obvious implementation — "substitute whenever
/// Rimbody returned null or a chunk job" — displaces 100% of chunk wins deterministically
/// and fires on null, which is precisely the state where Rimbody decided no workout was
/// possible. Worse, our JobDef sets <c>GiverIgnore=true</c> (it has no Thing target Rimbody
/// knows how to find), so <c>RimbodyDB.AddWorkoutJob</c> skips it entirely and its declared
/// strength never enters selection at all. Instead we score the lift with Rimbody's own
/// comparator, score whatever Rimbody picked the same way, and substitute only on a genuine
/// win. Chunk lifting stays alive, workout memory still rotates pawns between exercises,
/// and the strength number in the def actually means something.
/// </summary>
[HarmonyPatch(typeof(JobGiver_DoStrengthBuilding), nameof(JobGiver_DoStrengthBuilding.TryGiveJobActual))]
public static class JobGiver_DoStrengthBuilding_PawnLift_Patch
{
    /// <summary>How far a lifter will walk to find someone to pick up.</summary>
    private const float MaxLifteeDistance = 20f;

    /// <summary>Body size below which a target is not worth lifting (children, small xenos).</summary>
    private const float MinLifteeBodySize = 0.5f;

    private static bool warnedEmptyChunkHash;

    public static void Postfix(Pawn pawn, ref Job __result)
    {
        if (!PawnLiftSettingsTab.Enabled)
            return;

        // Rimbody wires this giver into the prisoner think-tree branch too. Without this
        // gate a muscular prisoner could pick up a colonist or a cellmate and carry them
        // twenty cells to a workout spot.
        if (pawn is not { Spawned: true } || !pawn.IsColonistPlayerControlled)
            return;
        if (pawn.carryTracker?.CarriedThing != null)
            return;

        CompPhysique compPhysique = pawn.compPhysique();
        if (compPhysique is not { HasPhysique: true })
            return;
        if (compPhysique.MuscleMass < PawnLiftSettingsTab.Threshold)
            return;

        if (!RimbodyDB.JobModExDB.TryGetValue(DefOf_MSSFPRB.MSSFP_DoPawnLift.shortHash, out ModExtensionRimbodyJob ourExtension))
            return;
        if (ourExtension?.strengthParts == null)
            return;

        WarnIfChunkHashEmpty();

        Pawn liftee = FindLiftee(pawn, compPhysique, ourExtension, out float bestScore);
        if (liftee == null)
            return;

        if (bestScore <= ScoreOf(compPhysique, __result))
            return;

        __result = JobMaker.MakeJob(DefOf_MSSFPRB.MSSFP_DoPawnLift, liftee);
    }

    /// <summary>
    /// Picks the best-scoring valid liftee. Score varies by target because effective
    /// resistance scales with the liftee's body size, so a hulk is a better workout than
    /// a wiry colonist.
    /// </summary>
    private static Pawn FindLiftee(Pawn lifter, CompPhysique compPhysique, ModExtensionRimbodyJob ourExtension, out float bestScore)
    {
        bestScore = 0f;
        Pawn best = null;

        IReadOnlyList<Pawn> candidates = lifter.Map?.mapPawns?.AllPawnsSpawned;
        if (candidates == null)
            return null;

        float memoryFactor = compPhysique.InMemory(ourExtension.id) ? 0.9f : 1f;

        foreach (Pawn candidate in candidates)
        {
            if (!IsValidLiftee(lifter, candidate))
                continue;

            float score =
                memoryFactor
                * compPhysique.GetStrengthJobScore(ourExtension.strengthParts, ourExtension.strength * ResistanceFactor(lifter, candidate));

            if (score <= bestScore)
                continue;

            bestScore = score;
            best = candidate;
        }

        return best;
    }

    private static bool IsValidLiftee(Pawn lifter, Pawn candidate)
    {
        if (candidate == null || candidate == lifter || !candidate.Spawned || candidate.Dead)
            return false;
        if (candidate.RaceProps is not { Humanlike: true })
            return false;
        if (candidate.BodySize < MinLifteeBodySize)
            return false;
        if (candidate.Drafted)
            return false;
        if (candidate.carryTracker?.CarriedThing != null)
            return false;
        if (candidate.InMentalState)
            return false;
        if (candidate.HostileTo(lifter))
            return false;

        if (!candidate.Position.InHorDistOf(lifter.Position, MaxLifteeDistance))
            return false;
        if (!lifter.CanReserveAndReach(candidate, PathEndMode.ClosestTouch, Danger.None))
            return false;

        bool downed = candidate.Downed;
        bool prisoner = candidate.IsPrisonerOfColony;
        bool idleColonist = !downed && !prisoner && candidate.IsColonist && IsUnoccupied(candidate);

        if (downed && !PawnLiftSettingsTab.DownedAllowed)
            return false;
        if (prisoner && !PawnLiftSettingsTab.PrisonersAllowed)
            return false;
        if (idleColonist && !PawnLiftSettingsTab.IdleColonistsAllowed)
            return false;

        return downed || prisoner || idleColonist;
    }

    /// <summary>Doing nothing that matters: idle, wandering, or on a recreation job.</summary>
    private static bool IsUnoccupied(Pawn candidate)
    {
        if (candidate.CurJob == null)
            return true;
        if (candidate.mindState is { IsIdle: true })
            return true;

        return candidate.CurJob.def?.joyKind != null;
    }

    /// <summary>
    /// How much harder this particular pawn is to lift than the def's baseline assumes.
    /// A starving child and a hulk should not train identically. Clamped so an extreme
    /// body size cannot dominate Rimbody's scoring.
    /// </summary>
    private static float ResistanceFactor(Pawn lifter, Pawn liftee)
    {
        float lifterSize = lifter.BodySize <= 0f ? 1f : lifter.BodySize;
        return Mathf.Clamp(liftee.BodySize / lifterSize, 0.6f, 1.6f);
    }

    /// <summary>
    /// Scores whatever Rimbody chose, using Rimbody's own scoring so the comparison is
    /// like-for-like. Non-target jobs (chunk lifting, push-ups) score off their
    /// ModExtension; the generic building job scores off the best strength workout its
    /// target building offers.
    /// </summary>
    private static float ScoreOf(CompPhysique compPhysique, Job job)
    {
        if (job == null)
            return 0f;

        if (RimbodyDB.JobModExDB.TryGetValue(job.def.shortHash, out ModExtensionRimbodyJob extension) && extension?.strengthParts != null)
        {
            return (compPhysique.InMemory(extension.id) ? 0.9f : 1f)
                * compPhysique.GetStrengthJobScore(extension.strengthParts, extension.strength);
        }

        Thing target = job.targetA.Thing;
        if (target == null || !RimbodyDB.ThingModExDB.TryGetValue(target.def.shortHash, out ModExtensionRimbodyTarget targetExtension))
            return 0f;

        float best = 0f;
        foreach (WorkOut workout in targetExtension.workouts)
        {
            if (workout.Category != RimbodyWorkoutCategory.Strength)
                continue;

            float score =
                (compPhysique.InMemory(workout.id) ? 0.9f : 1f)
                * compPhysique.GetWorkoutScore(RimbodyWorkoutCategory.Strength, workout);

            if (score > best)
                best = score;
        }

        return best;
    }

    /// <summary>
    /// Rimbody populates ChunkJobHash by matching the literal defName prefix
    /// "Rimbody_DoChunk". A rename upstream empties it silently. We no longer depend on
    /// that set, but an empty one is a strong signal that Rimbody's internals moved and
    /// this patch deserves a look.
    /// </summary>
    private static void WarnIfChunkHashEmpty()
    {
        if (warnedEmptyChunkHash || RimbodyDB.ChunkJobHash.Count != 0)
            return;

        warnedEmptyChunkHash = true;
        ModLog.Warn(
            "Rimbody's ChunkJobHash is empty — its chunk-workout defNames may have changed. "
                + "MSSFP pawn lifting still works, but Rimbody internals have moved and the compat layer should be reviewed."
        );
    }
}
