using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Incidents;

public class IncidentWorker_Mogus : IncidentWorker
{
    public static List<PawnKindDef> ValidPawnKindDef =>
        [
            MSSFPDefOf.MSSFP_MogusKind_Blue,
            MSSFPDefOf.MSSFP_MogusKind_Red,
            MSSFPDefOf.MSSFP_MogusKind_Green,
            // MSSFPDefOf.MSSFP_MogusKind_Yellow
        ];

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        return !MSSFPMod.settings.disableMogus && base.CanFireNowSub(parms) && TryFindEntryCell((Map)parms.target, out IntVec3 _);
    }

    public static bool TryFindEntryCell(Map map, out IntVec3 cell)
    {
        return CellFinder.TryFindRandomEdgeCellWith(c => map.reachability.CanReachColony(c) && !c.Fogged(map), map, CellFinder.EdgeRoadChance_Neutral, out cell);
    }

    public virtual Pawn SpawnPawn(Map map)
    {
        Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(ValidPawnKindDef.RandomElement(), Faction.OfPlayer, forceGenerateNewPawn: true));

        IntVec3 cell;
        TryFindEntryCell(map, out cell);
        GenSpawn.Spawn(pawn, cell, map);

        pawn.guest.Notify_PawnRecruited();
        pawn.caller.DoCall();

        if (def.pawnHediff != null)
        {
            pawn.health.AddHediff(def.pawnHediff);
        }

        return pawn;
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (MSSFPMod.settings.disableMogus)
            return false;

        Map target = (Map)parms.target;
        if (!TryFindEntryCell(target, out IntVec3 _))
        {
            return false;
        }

        SpawnPawn(target);

        return true;
    }
}
