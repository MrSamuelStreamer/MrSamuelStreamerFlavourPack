using RimWorld;
using Verse;

namespace MSSFP.Incidents;

public class IncidentWorker_FroggeJoin : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms)
    {
        return base.CanFireNowSub(parms) && TryFindEntryCell((Map)parms.target, out IntVec3 _);
    }

    public static bool TryFindEntryCell(Map map, out IntVec3 cell)
    {
        return CellFinder.TryFindRandomEdgeCellWith(c => map.reachability.CanReachColony(c) && !c.Fogged(map), map, CellFinder.EdgeRoadChance_Neutral, out cell);
    }

    public virtual Pawn SpawnPawn(Map map, Gender gender)
    {
        Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(def.pawnKind, Faction.OfPlayer, forceGenerateNewPawn: true, fixedGender: gender));

        IntVec3 cell;
        TryFindEntryCell(map, out cell);
        GenSpawn.Spawn(pawn, cell, map);

        if (def.pawnHediff != null)
        {
            pawn.health.AddHediff(def.pawnHediff);
        }

        return pawn;
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (MSSFPMod.settings.disableFroggeNom)
            return false;

        Map target = (Map)parms.target;
        if (!TryFindEntryCell(target, out IntVec3 _))
        {
            return false;
        }

        Pawn pawnMale = SpawnPawn(target, Gender.Male);
        Pawn pawnFemale = SpawnPawn(target, Gender.Female);

        SendStandardLetter(def.letterLabel.Formatted(), def.letterText.Formatted(), LetterDefOf.PositiveEvent, parms, new(pawnMale, pawnFemale));
        return true;
    }
}
