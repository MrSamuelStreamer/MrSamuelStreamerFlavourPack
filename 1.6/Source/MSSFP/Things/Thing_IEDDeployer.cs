using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Things;

/// <summary>
///     Invisible drop-pod cargo that scatters hostile IED traps in a radius
///     around its landing cell, then self-destroys. Used by
///     <see cref="MSSFP.Incidents.IncidentWorker_IEDPodRaid" />.
/// </summary>
public class Thing_IEDDeployer : Thing
{
    public int trapCount = 6;
    public float radius = 8f;
    public ThingDef trapDef;
    public Faction trapFaction;

    // Tracks whether scatter has already run. Scribed so a save taken after
    // scatter (but before Destroy) doesn't re-scatter on load.
    private bool scattered;

    private const float MinDistBetweenTraps = 5f;
    private const float MinDistBetweenTrapsSq = MinDistBetweenTraps * MinDistBetweenTraps;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref trapCount, "trapCount", 6);
        Scribe_Values.Look(ref radius, "radius", 8f);
        Scribe_Defs.Look(ref trapDef, "trapDef");
        Scribe_References.Look(ref trapFaction, "trapFaction");
        Scribe_Values.Look(ref scattered, "scattered", false);

        if (Scribe.mode == LoadSaveMode.PostLoadInit && trapFaction == null)
        {
            trapFaction = Find.FactionManager.RandomEnemyFaction(allowHidden: false, allowDefeated: false, allowNonHumanlike: true);
        }
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        // Already scattered before save → just clean up. Without this guard a
        // save taken between ScatterTraps() and Destroy() would re-scatter on load.
        if (respawningAfterLoad && scattered)
        {
            Destroy(DestroyMode.Vanish);
            return;
        }

        if (trapDef == null || trapFaction == null)
        {
            Log.Warning($"[MSSFP] Thing_IEDDeployer at {Position} missing trapDef or trapFaction; aborting.");
            Destroy(DestroyMode.Vanish);
            return;
        }

        ScatterTraps(map);
        scattered = true;
        Destroy(DestroyMode.Vanish);
    }

    private void ScatterTraps(Map map)
    {
        IntVec3 center = Position;
        List<IntVec3> candidates = GenRadial.RadialCellsAround(center, radius, useCenter: true)
            .Where(c => IsValidScatterCell(c, map))
            .InRandomOrder()
            .ToList();

        List<IntVec3> picked = new(trapCount);
        foreach (IntVec3 c in candidates)
        {
            if (picked.Count >= trapCount) break;

            bool tooClose = false;
            for (int i = 0; i < picked.Count; i++)
            {
                if ((picked[i] - c).LengthHorizontalSquared < MinDistBetweenTrapsSq)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            picked.Add(c);
        }

        foreach (IntVec3 cell in picked)
        {
            Building_TrapExplosive trap = (Building_TrapExplosive)ThingMaker.MakeThing(trapDef);
            // SetFactionDirect on unspawned Thing — otherwise a one-tick
            // factionless window breaks Building_Trap.KnowsOfTrap branches.
            trap.SetFactionDirect(trapFaction);
            GenSpawn.Spawn(trap, cell, map, Rot4.North);
        }
    }

    private static bool IsValidScatterCell(IntVec3 c, Map map)
    {
        if (!c.InBounds(map)) return false;
        if (!c.Standable(map)) return false;
        if (c.GetEdifice(map) != null) return false;
        if (c.GetTerrain(map).IsWater) return false;

        // Skip cells with a non-flying pawn — trap spawned under a pawn would
        // spring within 1 tick (KnowsOfTrap=false vs hostile faction → spring=1.0),
        // which reads as a bug to the player.
        List<Thing> things = c.GetThingList(map);
        for (int i = 0; i < things.Count; i++)
        {
            // Mirror Building_Trap.Tick filter: pawn.Flying is the canonical 1.6
            // "won't trigger ground traps" check. Skip cells with grounded pawns
            // — trap spawning under a pawn would spring within 1 tick.
            if (things[i] is Pawn { Flying: false })
            {
                return false;
            }
        }

        return true;
    }
}
