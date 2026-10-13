using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace MSSFP.Tunnels.Lords;

/// <summary>
/// Phase 2 lord toil: Brenda turns on the player's caravan after executing Barbas.
///
/// DutyDefOf.AssaultColony cannot be used here because its think-tree relies on
/// GenHostility.HostileTo to find targets, which always returns false when the
/// attacker's faction is null. Instead we drive Brenda via explicit AttackMelee
/// jobs every 60 ticks, mirroring the phase-1 pattern of LordToil_BrendaKillBarbas.
/// </summary>
public class LordToil_BrendaAssaultCaravan : LordToil
{
    public override bool ForceHighStoryDanger => true;
    public override bool AllowSatisfyLongNeeds => false;

    public override void UpdateAllDuties()
    {
        // AssaultColony duty enables the vanilla think-tree raider AI as a supplement
        // to our explicit job assignments, and silences ThinkNode_Duty/DutyConstant errors.
        foreach (Pawn pawn in lord.ownedPawns)
            if (pawn.mindState != null)
                pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
    }

    public override void LordToilTick()
    {
        if (Find.TickManager.TicksGame % 60 != 0)
            return;

        foreach (Pawn pawn in lord.ownedPawns)
        {
            if (pawn.Dead || !pawn.Spawned || pawn.jobs == null)
                continue;

            // Don't interrupt an ongoing attack against a still-valid target.
            Job curJob = pawn.jobs.curJob;
            if (curJob?.def == JobDefOf.AttackMelee)
            {
                Pawn curTarget = curJob.targetA.Thing as Pawn;
                if (curTarget != null && !curTarget.Dead && curTarget.Spawned)
                    continue;
            }

            // Find the nearest reachable player pawn (colonist or caravan animal).
            Pawn target = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                PathEndMode.Touch,
                TraverseParms.For(pawn, Danger.Deadly),
                maxDistance: 9999f,
                validator: t => t is Pawn p
                    && !p.Dead && p.Spawned
                    && p.Faction == Faction.OfPlayer
            ) as Pawn;

            if (target == null)
                continue;

            Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            job.killIncappedTarget = true;
            pawn.jobs.StartJob(job, JobCondition.InterruptForced,
                resumeCurJobAfterwards: false, cancelBusyStances: true);
        }
    }
}
