using RimWorld;
using Verse;

namespace MSSFP.Incidents;

public class IncidentWorker_Possession : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms)
    {
        return !MSSFPMod.settings.DisablePossession && base.CanFireNowSub(parms) && parms.target is Map;
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (MSSFPMod.settings.DisablePossession)
            return false;

        Map target = (Map)parms.target;

        Pawn targetPawn = target.mapPawns.FreeColonistsAndPrisonersSpawned.RandomElementWithFallback();

        if (targetPawn == null)
            return false;

        targetPawn.health.AddHediff(MSSFPDefOf.MSS_FP_PossessionHaunt);

        // SendStandardLetter(def.letterLabel.Formatted(target.Named("MAP")), def.letterText.Formatted(target.Named("MAP")), LetterDefOf.PositiveEvent, parms, new LookTargets(targetPawn));
        return true;
    }
}
