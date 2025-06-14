using System;
using System.Collections.Generic;
using System.Linq;
using MSSFP.Comps;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Jobs;

public class WorkGiver_PutALittleDirt : WorkGiver_Scanner
{
    public static Lazy<HashSet<TerrainDef>> validTerrainDefs =>
        new(
            () =>

                [
                    TerrainDefOf.PackedDirt,
                    TerrainDefOf.Soil,
                    TerrainDefOf.Sand,
                    TerrainDefOf.SoilRich,
                    TerrainDefOf.Gravel,
                    TerrainDefOf.Ice,
                    TerrainDefOf.BrokenAsphalt,
                    TerrainDefOf.FungalGravel,
                ]
        );

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        foreach (
            Thing t in pawn.Map.spawnedThings.Where(t =>
                t.Faction == Faction.OfPlayer
                && pawn.CanReach(pawn.Position, t, PathEndMode, MaxPathDanger(pawn))
                && t.HasComp<CompDirtHaver>()
                && !t.TryGetComp<CompDirtHaver>().HasDirt
            )
        )
        {
            yield return t;
        }
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!MSSFPMod.settings.EnableDirtJobs)
            return false;
        return pawn.CanReserve((LocalTargetInfo)t, 1, 0, ignoreOtherReservations: forced) && !t.TryGetComp<CompDirtHaver>().HasDirt;
    }

    public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

    public override Danger MaxPathDanger(Pawn pawn) => Danger.Some;

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        if (!MSSFPMod.settings.EnableDirtJobs)
            return true;
        return !pawn.Map.spawnedThings.Any(thing => thing.Faction == Faction.OfPlayer && thing.HasComp<CompDirtHaver>() && !thing.TryGetComp<CompDirtHaver>().HasDirt);
    }

    public override bool HasJobOnCell(Pawn pawn, IntVec3 c, bool forced = false)
    {
        if (!MSSFPMod.settings.EnableDirtJobs)
            return false;
        if (!pawn.CanReach(pawn.Position, c, PathEndMode, MaxPathDanger(pawn)))
            return false;

        return !c.IsForbidden(pawn) && pawn.CanReserve(c, ignoreOtherReservations: forced);
    }

    public virtual IntVec3 FindDirtSpot(Pawn pawn, Thing dirtHaver)
    {
        CellFinder.TryFindRandomReachableCellNearPosition(pawn.Position, dirtHaver.Position, pawn.Map, 50, TraverseParms.For(pawn), Validator, null, out IntVec3 result);
        return result;
        bool Validator(IntVec3 c) =>
            validTerrainDefs.Value.Contains(pawn.Map.terrainGrid.TerrainAt(c)) && pawn.CanReach(pawn.Position, c, PathEndMode, MaxPathDanger(pawn)) && pawn.CanReserve(c, 1, 0);
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        IntVec3 DirtSpot = FindDirtSpot(pawn, t);
        Job job = JobMaker.MakeJob(MSSFPDefOf.MSSFP_PutALittleDirtUnderThePillow, (LocalTargetInfo)t);
        job.targetB = DirtSpot;
        job.count = 1;
        pawn.Reserve(t, job, 1, 0, ignoreOtherReservations: forced);
        pawn.Reserve(DirtSpot, job, 1, 0, ignoreOtherReservations: forced);
        return job;
    }
}
