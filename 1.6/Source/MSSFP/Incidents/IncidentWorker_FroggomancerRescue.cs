using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Incidents;

public class IncidentWorker_FroggomancerRescue : IncidentWorker
{
    public PawnKindDef FroggomancerKind => DefDatabase<PawnKindDef>.GetNamed("MSS_FP_Froggomancer");
    public AbilityDef MSSFP_FroggoHeal => DefDatabase<AbilityDef>.GetNamed("MSSFP_FroggoHeal");

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        return MSSFPMod.settings.EnableFroggeIncidents
            && FroggomancerKind != null
            && MSSFP_FroggoHeal != null
            && parms.target is Map map
            && map.mapPawns.SpawnedDownedPawns.Any(p => p.Faction == Faction.OfPlayer);
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (FroggomancerKind == null && MSSFP_FroggoHeal != null)
            return false;
        if (parms.target is not Map map)
            return false;
        if (!map.mapPawns.SpawnedDownedPawns.Any(p => p.Faction == Faction.OfPlayer))
            return false;

        Pawn targetPawn = map
            .mapPawns.SpawnedDownedPawns.Where(p => p.Faction == Faction.OfPlayer)
            .RandomElement();

        Pawn froggomancer = PawnGenerator.GeneratePawn(
            new PawnGenerationRequest(kind: FroggomancerKind, forceGenerateNewPawn: true)
        );

        IEnumerable<IntVec3> cells = GenRadial
            .RadialCellsAround(targetPawn.Position, 10, true)
            .Where(cell =>
                map.reachability.CanReach(
                    cell,
                    targetPawn,
                    PathEndMode.InteractionCell,
                    TraverseMode.ByPawn,
                    Danger.Deadly
                )
            );

        IntVec3 targetCell = cells.RandomElement();

        SkipUtility.SkipTo(froggomancer, targetCell, map);

        map.effecterMaintainer.AddEffecterToMaintain(
            EffecterDefOf.Skip_EntryNoDelay.Spawn(froggomancer, map),
            targetCell,
            60
        );

        froggomancer.abilities.GainAbility(MSSFP_FroggoHeal);
        Job job = froggomancer
            .abilities.GetAbility(MSSFP_FroggoHeal)
            .GetJob(targetPawn, targetPawn.Position);

        return froggomancer.jobs.TryTakeOrderedJob(job, JobTag.Misc);
    }
}
