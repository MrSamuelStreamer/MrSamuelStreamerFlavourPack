using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Incidents;

public class IncidentWorker_FroggeResearch : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms)
    {
        return base.CanFireNowSub(parms) && MSSFPMod.settings.EnableFroggeIncidents;
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        Pawn bestResearcher = Find
            .CurrentMap.mapPawns.FreeColonistsSpawned.OrderByDescending(p =>
                p.skills.GetSkill(SkillDefOf.Intellectual)
            )
            .FirstOrDefault();

        if (bestResearcher == null)
            return false;

        SendIncidentLetter(
            (TaggedString)def.letterLabel,
            (TaggedString)def.letterText,
            LetterDefOf.PositiveEvent,
            parms,
            (Thing)bestResearcher,
            def,
            bestResearcher.NameFullColored,
            bestResearcher.NameShortColored
        );
        return true;
    }
}
