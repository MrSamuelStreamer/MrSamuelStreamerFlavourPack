using System.Collections.Generic;
using MSSFP.Comps;
using RimWorld;
using Verse;

namespace MSSFP.Incidents;

public class IncidentWorker_DirtManVisit : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (parms.target is not Map map)
            return false;

        return map.listerThings.AllThings.Count(t => t.TryGetComp(out CompDirtHaver dh) && !dh.hasDirt) > 0;
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        Map target = (Map)parms.target;
        PawnKindDef kind = MSSFPDefOf.MSSFP_Dirtman;

        IntVec3 result = parms.spawnCenter;
        if (!result.IsValid && !RCellFinder.TryFindRandomPawnEntryCell(out result, target, CellFinder.EdgeRoadChance_Animal))
            return false;

        List<Pawn> dirtMen = AggressiveAnimalIncidentUtility.GenerateAnimals(kind, target.Tile, parms.points * 1f, 1);

        Rot4 rot = Rot4.FromAngleFlat((target.Center - result).AngleFlat);

        Pawn newThing = dirtMen[0];
        QuestUtility.AddQuestTag(GenSpawn.Spawn(newThing, CellFinder.RandomClosewalkCellNear(result, target, 10), target, rot), parms.questTag);
        newThing.health.AddHediff(HediffDefOf.Scaria);
        newThing.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);
        newThing.mindState.exitMapAfterTick = Find.TickManager.TicksGame + Rand.Range(60000, 120000);

        SendStandardLetter(def.letterLabel.Translate(), def.letterText.Translate(), LetterDefOf.ThreatBig, parms, (Thing)dirtMen[0]);

        Find.TickManager.slower.SignalForceNormalSpeedShort();

        return true;
    }
}
