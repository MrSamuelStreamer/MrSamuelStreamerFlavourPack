using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace MSSFP.Jobs;

public class JobGiver_AIFroggeDefendMaster : JobGiver_AIDefendMaster
{
    public List<ThingDef> rangedOnlyPawns;
    public int meleeDistance = 5;

    protected override IntRange ExpiryInterval_Melee => new IntRange(30, 480);

    public Lazy<MethodInfo> MI_GetAbilityJob = new Lazy<MethodInfo>(() =>
        AccessTools.Method(typeof(JobGiver_AIDefendPawn), "GetAbilityJob")
    );

    protected override Job TryGiveJob(Pawn pawn)
    {
        Pawn defendee = GetDefendee(pawn);
        if (defendee == null)
        {
            Log.Error(GetType() + " has null defendee. pawn=" + pawn.ToStringSafe());
            return null;
        }

        if (defendee.CarriedBy != null)
        {
            if (
                !pawn.CanReach(
                    (LocalTargetInfo)(Thing)defendee.CarriedBy,
                    PathEndMode.OnCell,
                    Danger.Deadly
                )
            )
                return null;
        }
        else if (
            !defendee.Spawned
            || !pawn.CanReach((LocalTargetInfo)(Thing)defendee, PathEndMode.OnCell, Danger.Deadly)
        )
            return null;

        if (
            (pawn.IsColonist || pawn.IsMutant)
            && pawn.playerSettings.hostilityResponse != HostilityResponseMode.Attack
            && (
                pawn.GetLord()?.LordJob is not LordJob_Ritual_Duel lordJob
                || !lordJob.duelists.Contains(pawn)
            )
        )
            return null;

        UpdateEnemyTarget(pawn);

        switch (pawn.mindState.enemyTarget)
        {
            case null:
            case Pawn pawn1 when pawn1.IsPsychologicallyInvisible():
                return null;
        }

        bool allowManualCastWeapons = !pawn.IsColonist && !pawn.IsMutant && !DisableAbilityVerbs;
        if (allowManualCastWeapons)
        {
            if (
                MI_GetAbilityJob.Value.Invoke(this, [pawn, pawn.mindState.enemyTarget])
                is Job abilityJob
            )
                return abilityJob;
        }

        if (OnlyUseAbilityVerbs)
        {
            if (!TryFindShootingPosition(pawn, out IntVec3 dest))
                return null;
            if (dest == pawn.Position)
                return JobMaker.MakeJob(
                    JobDefOf.Wait_Combat,
                    ExpiryInterval_Ability.RandomInRange,
                    true
                );
            Job job = JobMaker.MakeJob(JobDefOf.Goto, dest);
            job.expiryInterval = ExpiryInterval_Ability.RandomInRange;
            job.checkOverrideOnExpire = true;
            return job;
        }

        bool inRange =
            (pawn.Position - pawn.mindState.enemyTarget.Position).LengthHorizontalSquared
            < meleeDistance * meleeDistance;
        Verb attackVerb = pawn.TryGetAttackVerb(
            pawn.mindState.enemyTarget,
            allowManualCastWeapons,
            allowTurrets
        );
        if (attackVerb.verbProps.IsMeleeAttack)
        {
            if (inRange)
                return MeleeAttackJob(pawn, pawn.mindState.enemyTarget);
            attackVerb = pawn.abilities.abilities[0].verb;
        }

        bool blockChance =
            CoverUtility.CalculateOverallBlockChance(
                (LocalTargetInfo)(Thing)pawn,
                pawn.mindState.enemyTarget.Position,
                pawn.Map
            ) > 0.0099999997764825821;
        bool canStandAtPos =
            pawn.Position.Standable(pawn.Map)
            && pawn.Map.pawnDestinationReservationManager.CanReserve(
                pawn.Position,
                pawn,
                pawn.Drafted
            );
        bool canHitTarget = attackVerb.CanHitTarget((LocalTargetInfo)pawn.mindState.enemyTarget);

        if ((blockChance && canStandAtPos && canHitTarget) || inRange && canHitTarget)
            return JobMaker.MakeJob(
                JobDefOf.Wait_Combat,
                ExpiryInterval_ShooterSucceeded.RandomInRange,
                true
            );

        if (!TryFindShootingPosition(pawn, out IntVec3 shootPos, attackVerb))
            return null;
        if (shootPos == pawn.Position)
            return JobMaker.MakeJob(
                JobDefOf.Wait_Combat,
                ExpiryInterval_ShooterSucceeded.RandomInRange,
                true
            );
        Job job1 = JobMaker.MakeJob(JobDefOf.Goto, shootPos);
        job1.expiryInterval = ExpiryInterval_ShooterSucceeded.RandomInRange;
        job1.checkOverrideOnExpire = true;
        return job1;
    }
}
