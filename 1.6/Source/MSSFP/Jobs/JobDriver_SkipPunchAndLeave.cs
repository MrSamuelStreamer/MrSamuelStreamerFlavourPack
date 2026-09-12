using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Jobs
{
    /// <summary>
    /// Reusable JobDriver: sprint to TargetA, deal a comedic 1-damage blunt punch,
    /// then skip-teleport away and despawn. Used for the Codex Punch incident but
    /// applicable to any "teleport in, punch, teleport out" scenario.
    /// </summary>
    public class JobDriver_SkipPunchAndLeave : JobDriver
    {
        private const int DramaticPauseTicks = 60;
        private const int TimeoutTicks = 2500;
        private const float PunchDamage = 1f;

        private Pawn Target => (Pawn)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // No reservation needed — we're just running up and punching
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Fail conditions: target dies, despawns, or we take too long
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => Target.Dead);

            // Ensure the Codex is despawned regardless of which path the job ends on
            // (success, fail condition, or interruption), not just the happy-path toils.
            AddFinishAction(_ => SkipDespawn(pawn));

            // Toil 1: Sprint to target
            Toil gotoToil = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            gotoToil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            yield return gotoToil;

            // Toil 2: Punch (1 blunt damage — comedic, not lethal)
            Toil punchToil = ToilMaker.MakeToil("MakeNewToils_Punch");
            punchToil.initAction = () =>
            {
                Pawn target = Target;
                if (target is { Dead: false, Spawned: true })
                {
                    pawn.rotationTracker.FaceTarget(target);
                    target.TakeDamage(new DamageInfo(
                        DamageDefOf.Blunt, PunchDamage, 0f, -1f, pawn));
                    Messages.Message(
                        "MSS_FP_CodexPunch_PunchMsg".Translate(target.LabelShort),
                        new LookTargets(target),
                        MessageTypeDefOf.NeutralEvent);
                }
            };
            punchToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return punchToil;

            // Toil 3: Dramatic pause
            Toil pauseToil = ToilMaker.MakeToil("MakeNewToils_Pause");
            pauseToil.defaultCompleteMode = ToilCompleteMode.Delay;
            pauseToil.defaultDuration = DramaticPauseTicks;
            yield return pauseToil;

            // Toil 4: Skip-despawn (fires at start of a fresh toil, not during cleanup)
            Toil leaveToil = ToilMaker.MakeToil("MakeNewToils_Leave");
            leaveToil.initAction = () => SkipDespawn(pawn);
            leaveToil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return leaveToil;
        }

        public override void Notify_PatherFailed()
        {
            base.Notify_PatherFailed();
            SkipDespawn(pawn);
        }

        /// <summary>
        /// Pawns currently mid-<see cref="SkipUtility.SkipDeSpawn"/>. Reentrancy guard —
        /// see the remarks on <see cref="SkipDespawn"/> for why this is required, not just
        /// defensive.
        /// </summary>
        private static readonly HashSet<Pawn> despawnInProgress = new();

        /// <summary>
        /// Skip-teleport the pawn away. The world pawn system will
        /// garbage-collect the factionless pawn on its own schedule.
        /// We avoid PassToWorld(Discard) because other systems (letters,
        /// battle log) may still hold references this frame.
        ///
        /// MUST be reentrancy-guarded. Both the leave toil and the AddFinishAction in
        /// <see cref="MakeNewToils"/> can call this for the same pawn, and
        /// <c>SkipUtility.SkipDeSpawn</c> -&gt; <c>Thing.DeSpawnOrDeselect</c> -&gt;
        /// <c>Pawn.DeSpawn</c> calls <c>jobs.StopAll()</c> BEFORE it clears
        /// <c>Spawned</c> (see Verse/Pawn.cs). StopAll interrupts the current job, which
        /// fires the finish action, which calls this method again — <c>p.Spawned</c> is
        /// still true at that point, so an unguarded call recurses into
        /// SkipUtility.SkipDeSpawn -&gt; DeSpawnOrDeselect -&gt; DeSpawn -&gt; StopAll -&gt;
        /// finish action -&gt; ... without end, crashing the process with a native stack
        /// overflow (confirmed from a player CTD log, 2026-09-12 — uncatchable, the
        /// existing try/catch below never even runs).
        /// </summary>
        private static void SkipDespawn(Pawn p)
        {
            if (!p.Spawned) return;
            if (!despawnInProgress.Add(p)) return; // already despawning this pawn — reentrant call, no-op

            try
            {
                SkipUtility.SkipDeSpawn(p);
            }
            catch
            {
                // Fallback if skip effect fails for any reason
                if (p.Spawned)
                    p.DeSpawn(DestroyMode.Vanish);
            }
            finally
            {
                despawnInProgress.Remove(p);
            }
        }
    }
}
