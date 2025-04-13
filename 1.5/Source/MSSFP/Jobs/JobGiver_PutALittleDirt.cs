using System;
using System.Collections.Generic;
using System.Linq;
using MSSFP.Comps;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Jobs;

public class JobGiver_PutALittleDirt : ThinkNode_JobGiver
{
    public override float GetPriority(Pawn pawn) => 5.9f;

    public static Lazy<HashSet<TerrainDef>> validTerrainDefs =>
        new Lazy<HashSet<TerrainDef>>(
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

    public virtual Thing FindDirtHaver(Pawn pawn)
    {
        return pawn
            .Map.spawnedThings.Where(t => t.HasComp<CompDirtHaver>() && pawn.CanReserveAndReach(t.Position, PathEndMode.ClosestTouch, Danger.None))
            .RandomElementWithFallback();
    }

    public virtual IntVec3 FindDirtSpot(Pawn pawn, Thing dirtHaver)
    {
        CellFinder.TryFindRandomReachableCellNearPosition(pawn.Position, dirtHaver.Position, pawn.Map, 50, TraverseParms.For(pawn), Validator, null, out IntVec3 result);
        return result;
        bool Validator(IntVec3 c) => validTerrainDefs.Value.Contains(pawn.Map.terrainGrid.TerrainAt(c)) && pawn.CanReserveAndReach(c, PathEndMode.ClosestTouch, Danger.None);
    }

    protected override Job TryGiveJob(Pawn pawn)
    {
        if (!MSSFPMod.settings.EnableDirtJobs)
            return null;
        if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            return null;
        Thing dirtHaver = FindDirtHaver(pawn);

        if (dirtHaver == null)
            return null;

        IntVec3 dirtSpot = FindDirtSpot(pawn, dirtHaver);

        return dirtSpot == IntVec3.Invalid ? null : MakeDirtJob(dirtHaver, dirtSpot);
    }

    public static Job MakeDirtJob(Thing dirtHaver, IntVec3 dirtSpot)
    {
        Job job = JobMaker.MakeJob(MSSFPDefOf.MSSFP_PutALittleDirtUnderThePillow, (LocalTargetInfo)dirtHaver);
        job.targetB = dirtSpot;
        job.count = 1;
        return job;
    }
}
