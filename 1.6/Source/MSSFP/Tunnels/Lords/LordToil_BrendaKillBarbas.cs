using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace MSSFP.Tunnels.Lords;

/// <summary>
/// Phase 1 lord toil: forces Brenda to melee-attack and execute Barbas.
/// Runs every 60 ticks and re-issues the kill job whenever Brenda's current job
/// is not already targeting Barbas.
/// </summary>
public class LordToil_BrendaKillBarbas : LordToil
{
    private readonly Pawn _barbas;

    public LordToil_BrendaKillBarbas(Pawn barbas)
    {
        _barbas = barbas;
    }

    public override void UpdateAllDuties()
    {
        // Explicit jobs in LordToilTick drive the attack; Idle suppresses the
        // ThinkNode_Duty / ThinkNode_DutyConstant "no duty" errors in the think tree.
        foreach (Pawn pawn in lord.ownedPawns)
            if (pawn.mindState != null)
                pawn.mindState.duty = new PawnDuty(DutyDefOf.Idle);
    }

    public override void LordToilTick()
    {
        if (Find.TickManager.TicksGame % 60 != 0)
            return;

        if (_barbas == null || _barbas.Dead || !_barbas.Spawned)
            return;

        foreach (Pawn pawn in lord.ownedPawns)
        {
            if (pawn.Dead || !pawn.Spawned || pawn.jobs == null)
                continue;

            Job current = pawn.jobs.curJob;
            if (current != null && current.targetA.Thing == _barbas)
                continue; // already attacking Barbas

            Job killJob = JobMaker.MakeJob(JobDefOf.AttackMelee, _barbas);
            killJob.killIncappedTarget = true;
            pawn.jobs.StartJob(killJob, JobCondition.InterruptForced,
                resumeCurJobAfterwards: false, cancelBusyStances: true);
        }
    }
}
