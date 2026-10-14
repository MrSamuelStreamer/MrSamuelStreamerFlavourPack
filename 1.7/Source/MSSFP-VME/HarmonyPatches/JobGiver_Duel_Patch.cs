using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace MSSFP.VME.HarmonyPatches;

[HarmonyPatch(typeof(JobGiver_Duel), "MeleeAttackJob")]
public static class JobGiver_Duel_MeleeAttackJob_Patch
{
    public static void Postfix(Pawn pawn, Job __result)
    {
        if (__result == null || !BloodCourtDuelUtility.IsNonLethalLeadershipDuelist(pawn))
            return;
        __result.killIncappedTarget = false;
        if (pawn.CurJob != null)
            pawn.CurJob.killIncappedTarget = false;
    }
}

[HarmonyPatch(typeof(JobGiver_Duel), "UpdateEnemyTarget")]
public static class JobGiver_Duel_UpdateEnemyTarget_Patch
{
    public static void Postfix(Pawn pawn)
    {
        if (!BloodCourtDuelUtility.IsNonLethalLeadershipDuelist(pawn))
            return;
        if (pawn.GetLord()?.LordJob is not LordJob_Ritual_Duel duel)
            return;

        Pawn opponent = duel.Opponent(pawn);
        if (opponent == null || opponent.Dead || opponent.Downed)
        {
            pawn.mindState.enemyTarget = null;
            if (pawn.CurJob?.def == JobDefOf.AttackMelee)
                pawn.jobs.EndCurrentJob(JobCondition.Succeeded, false);
        }
    }
}
