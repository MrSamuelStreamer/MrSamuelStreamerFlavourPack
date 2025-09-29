using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Raids;

public class IncidentWorker_SneakySteal : IncidentWorker_RaidEnemy
{
    public override bool TryResolveRaidArriveMode(IncidentParms parms)
    {
        parms.raidStrategy = MSSFPDefOf.MSSFP_SneakySteal;
        parms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop;

        return true;
    }

  protected override string GetRelatedPawnsInfoLetterText(IncidentParms parms)
  {
      return string.Empty;
  }

  protected override string GetLetterLabel(IncidentParms parms)
  {
      return string.Empty;
  }

  protected override string GetLetterText(IncidentParms parms, List<Pawn> pawns)
  {
      return string.Empty;
  }

  protected override LetterDef GetLetterDef()
  {
      return null;
  }

  protected override bool TryExecuteWorker(IncidentParms parms)
  {
    List<Pawn> pawns;
    if (!TryGenerateRaidInfo(parms, out pawns))
      return false;

    if (parms.controllerPawn == null || parms.controllerPawn.Faction != Faction.OfPlayer)
      parms.raidStrategy.Worker.MakeLords(parms, pawns);

    LessonAutoActivator.TeachOpportunity(ConceptDefOf.EquippingWeapons, OpportunityType.Critical);
    if (!PlayerKnowledgeDatabase.IsComplete(ConceptDefOf.ShieldBelts))
    {
        if (Enumerable.Any(pawns, pawn => pawn.apparel != null && pawn.apparel.WornApparel.Any(ap => ap.def == ThingDefOf.Apparel_ShieldBelt)))
        {
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.ShieldBelts, OpportunityType.Critical);
        }
    }
    if (DebugSettings.logRaidInfo)
      Log.Message($"Raid: {parms.faction.Name} ({parms.faction.def.defName}) {parms.raidArrivalMode.defName} {parms.raidStrategy.defName} c={parms.spawnCenter} p={parms.points}");

    ++Find.StoryWatcher.statsRecord.numRaidsEnemy;
    parms.target.StoryState.lastRaidFaction = parms.faction;
    return true;
  }
}
