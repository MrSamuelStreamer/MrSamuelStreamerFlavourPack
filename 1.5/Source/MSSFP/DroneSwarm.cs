using System.Collections.Generic;
using System.Linq;
using MSSFP.Comps.Map;
using RimWorld;
using Verse;

namespace MSSFP;

public class DroneSwarm : IExposable
{
    public List<Drone> Drones;
    public int DroneCountTarget = 0;
    public int DronesSpawned = 0;
    public Pawn Owner;

    public int NextAttackAt = -1;

    public DroneSwarm(int droneCount, Pawn owner)
    {
        DroneCountTarget = droneCount;
        Owner = owner;
        Owner.Map.GetComponent<DroneController>()?.Swarms.Add(this);
    }

    public virtual void InitSwarm()
    {
        // for (int i = 0; i < DroneCountTarget; i++)
        // {
        //     SpawnDrone(Owner.Position, Owner.Map);
        // }
    }

    public virtual void SpawnDrone(IntVec3 loc, Map map)
    {
        Drone drone = (Drone)ThingMaker.MakeThing(MSSFPDefOf.MSS_Drone);
        if (drone == null)
        {
            ModLog.Warn("Failed to create drone");
            return;
        }

        Drones ??= [];

        Drones.Add(drone);
        drone.Swarm = this;

        GenSpawn.Spawn(drone, loc, map);
        drone.DroneDrawPos = drone.TrueCenter();
        drone.CurrentTarget = ValidTargets().RandomElementWithFallback();
        drone.Owner = Owner;
    }

    public virtual IEnumerable<Pawn> ValidTargets()
    {
        return Owner.Map.mapPawns.AllPawnsSpawned.Where(p => !p.NonHumanlikeOrWildMan() && p != Owner && p.Faction.HostileTo(Owner.Faction));
    }

    public virtual void UpdateTargets()
    {
        List<Pawn> pawns = ValidTargets().ToList();

        foreach (Drone drone in Drones.Where(d => d.CurrentTarget == null || d.CurrentTarget.Downed || d.CurrentTarget.Dead))
        {
            Pawn furthest = pawns.OrderBy(p => p.Position.DistanceTo(drone.Position)).FirstOrFallback();
            Pawn closest = pawns.OrderBy(p => p.Position.DistanceTo(drone.Position)).Reverse().FirstOrFallback();

            if (furthest != null && closest != null && furthest != closest)
            {
                var closestDist = closest.Position.DistanceTo(drone.Position);
                var furthestDist = furthest.Position.DistanceTo(drone.Position);
                drone.CurrentTarget = pawns.RandomElementByWeightWithFallback(p => furthestDist - p.Position.DistanceTo(drone.Position) + closestDist);
            }
            else
            {
                drone.CurrentTarget = pawns.RandomElementWithFallback();
            }
        }

        foreach (Drone drone in Drones)
        {
            drone.Position = drone.CurrentTarget?.Position ?? drone.Position;
        }
    }

    public virtual void Tick()
    {
        if (NextAttackAt < 0)
            NextAttackAt = Find.TickManager.TicksAbs + Rand.Range(10, 300);

        if (DronesSpawned < DroneCountTarget && Find.TickManager.TicksAbs % 2 == 0)
        {
            SpawnDrone(Owner.Position, Owner.Map);
            DronesSpawned++;
            SpawnDrone(Owner.Position, Owner.Map);
            DronesSpawned++;
        }

        if (Find.TickManager.TicksAbs % 30 == 0)
        {
            UpdateTargets();
        }

        if (NextAttackAt < Find.TickManager.TicksAbs)
        {
            NextAttackAt = Find.TickManager.TicksAbs + Rand.Range(5, 60);

            for (int i = 0; i < Rand.Range(1, 10); i++)
            {
                Drones.RandomElementWithFallback()?.BeginAttack();
            }
        }

        Drones.Where(d => d.TargetReached).ToList().ForEach(d => d.ReachedTarget());
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref Drones, "Drones", LookMode.Reference);
        Scribe_Values.Look(ref DroneCountTarget, "DroneCountTarget");
        Scribe_Values.Look(ref DronesSpawned, "DronesSpawned");
        Scribe_References.Look(ref Owner, "Owner");
        Scribe_Values.Look(ref NextAttackAt, "NextAttackAt");
    }
}
