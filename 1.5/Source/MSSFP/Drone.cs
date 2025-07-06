using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Random = UnityEngine.Random;

namespace MSSFP;

public class Drone : ThingWithComps
{
    public DroneSwarm _swarm;

    public DroneSwarm Swarm
    {
        get { return _swarm; }
        set { _swarm = value; }
    }

    public Sustainer sustainer;

    public override void Tick()
    {
        base.Tick();
        if (sustainer == null || sustainer.Ended)
            sustainer = MSSFPDefOf.MSS_DroneFly.TrySpawnSustainer(SoundInfo.InMap((TargetInfo)(Thing)this, MaintenanceType.PerTick));
        sustainer.Maintain();
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        Swarm.Drones.Remove(this);
        sustainer?.End();
        base.DeSpawn(mode);
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_References.Look(ref Owner, "owner");
        Scribe_References.Look(ref CurrentTarget, "currentTarget");
        Scribe_Values.Look(ref CurrentTargetPosOffset, "currentTargetPosOffset");
        Scribe_Values.Look(ref moveSpeed, "moveSpeed");
        Scribe_Values.Look(ref elapsedTime, "elapsedTime");
        Scribe_Values.Look(ref TargetReached, "targetReached");
        Scribe_Values.Look(ref IsAttacking, "isAttacking");
    }

    public Pawn Owner;
    public Pawn CurrentTarget;
    public Vector3 CurrentTargetPosOffset = Vector3.zero;

    public Vector3 CurrentTargetPos
    {
        get
        {
            if (CurrentTarget == null)
                return Vector3.zero;

            if (CurrentTargetPosOffset != Vector3.zero)
            {
                return CurrentTarget.DrawPos + CurrentTargetPosOffset;
            }

            CurrentTargetPosOffset.x = Random.Range(-1.5f, 1.5f);
            CurrentTargetPosOffset.y = Random.Range(-1.5f, 1.5f);

            return CurrentTarget.DrawPos + CurrentTargetPosOffset;
        }
    }

    public Vector3 OwnerPos
    {
        get
        {
            if (CurrentTargetPosOffset == Vector3.zero)
            {
                CurrentTargetPosOffset.x = Random.Range(-1.5f, 1.5f);
                CurrentTargetPosOffset.y = Random.Range(-1.5f, 1.5f);
            }

            return Owner.DrawPos + CurrentTargetPosOffset;
        }
    }

    public float moveSpeed = 12f;

    public float elapsedTime;

    public Vector3 DroneDrawPos = Vector3.zero;

    public override Vector3 DrawPos
    {
        get
        {
            if (DroneDrawPos == Vector3.zero)
            {
                DroneDrawPos = this.TrueCenter();
            }
            return DroneDrawPos;
        }
    }

    public bool IsAttacking = false;

    public void BeginAttack()
    {
        if (CurrentTarget == null || CurrentTarget.Downed || CurrentTarget.Dead)
        {
            IsAttacking = false;
            return;
        }
        IsAttacking = true;
    }

    public Lazy<List<DamageDef>> PossibleDamageDefs = new(
        () =>

            [
                DamageDefOf.Blunt,
                DamageDefOf.Burn,
                DamageDefOf.Cut,
                DamageDefOf.ElectricalBurn,
                DamageDefOf.Flame,
                DamageDefOf.ToxGas,
                DamageDefOf.Stab,
                DamageDefOf.Scratch,
                DamageDefOf.Smoke,
            ]
    );

    public void ReachedTarget()
    {
        if (CurrentTarget == null)
            return;
        try
        {
            GenExplosion.DoExplosion(
                CurrentTarget.Position,
                CurrentTarget.Map,
                1.5f,
                PossibleDamageDefs.Value.RandomElement(),
                this,
                2,
                2,
                MSSFPDefOf.MSS_DroneExplode,
                null,
                chanceToStartFire: 0.5f,
                screenShakeFactor: 0.25f
            );
            CurrentTarget.stances.stunner.StunFor(60, this);
        }
        finally
        {
            Destroy();
        }
    }

    public bool TargetReached = false;

    public float initialEllipseAngle = Random.Range(0f, 360f);
    public float swarmingSpeedMultiplier = Random.Range(3f, 6f);

    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        Vector3 targetPosition = CurrentTargetPos;
        if (targetPosition == Vector3.zero)
        {
            targetPosition = OwnerPos;
        }

        if (Current.ProgramState == ProgramState.Playing && Find.TickManager.CurTimeSpeed != TimeSpeed.Paused && targetPosition != Vector3.zero)
        {
            Vector3 currentPosition = drawLoc;
            float speed = moveSpeed * Time.deltaTime;
            Vector3 newPosition;

            if (IsAttacking)
            {
                newPosition = Vector3.MoveTowards(currentPosition, targetPosition, speed);
                float distanceToTarget = Vector3.Distance(newPosition, targetPosition);
                if (distanceToTarget <= 0.5f)
                {
                    TargetReached = true;
                }
            }
            else
            {
                newPosition = Vector3.MoveTowards(currentPosition, targetPosition, speed);
                float distanceToTarget = Vector3.Distance(newPosition, targetPosition);
                float transitionDistance = 2f;

                if (distanceToTarget <= transitionDistance)
                {
                    elapsedTime += Time.deltaTime;
                    float angle = initialEllipseAngle + elapsedTime * moveSpeed * swarmingSpeedMultiplier;
                    float x = 2f * Mathf.Cos(angle * Mathf.Deg2Rad);
                    float z = 2f * Mathf.Sin(angle * Mathf.Deg2Rad);
                    Vector3 circlingOffset = new Vector3(x, 0f, z);

                    float transitionFactor = distanceToTarget / transitionDistance;
                    newPosition = Vector3.Lerp(targetPosition + circlingOffset, newPosition, transitionFactor);
                }
            }

            base.DrawAt(newPosition, flip);
            DroneDrawPos = newPosition;
        }
        else
        {
            base.DrawAt(drawLoc, flip);
        }
    }
}
