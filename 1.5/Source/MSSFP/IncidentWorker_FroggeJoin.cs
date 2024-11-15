using RimWorld;
using Verse;

namespace MSSFP;

public class IncidentWorker_FroggeJoin : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms)
    {
        return base.CanFireNowSub(parms) && TryFindEntryCell((Map) parms.target, out IntVec3 _);
    }

    public static bool TryFindEntryCell(Map map, out IntVec3 cell)
    {
        return CellFinder.TryFindRandomEdgeCellWith(c => map.reachability.CanReachColony(c) && !c.Fogged(map), map, CellFinder.EdgeRoadChance_Neutral, out cell);
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        Map target = (Map) parms.target;
        if (!TryFindEntryCell(target, out IntVec3 _))
        {
            return false;
        }

        Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(def.pawnKind, Faction.OfPlayer, forceGenerateNewPawn: true));

        IntVec3 cell;
        TryFindEntryCell(target, out cell);
        GenSpawn.Spawn(pawn, cell, target);

        if (def.pawnHediff != null)
        {
            pawn.health.AddHediff(def.pawnHediff);
        }

        TaggedString text = def.pawnHediff != null
            ? def.letterText.Formatted(pawn.Named("PAWN"), def.pawnHediff.Named("HEDIFF")).AdjustedFor(pawn)
            : def.letterText.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn);
        TaggedString title = def.letterLabel.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn);

        SendStandardLetter(title, text, LetterDefOf.PositiveEvent, parms, (Thing) pawn);
        return true;
    }
}
