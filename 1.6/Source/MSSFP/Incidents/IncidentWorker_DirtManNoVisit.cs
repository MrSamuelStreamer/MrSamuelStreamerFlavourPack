using MSSFP.Comps;
using RimWorld;
using Verse;

namespace MSSFP.Incidents;

public class IncidentWorker_DirtManNoVisit : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (parms.target is not Map map)
            return false;

        return map.listerThings.AllThings.Count(t =>
                t.TryGetComp(out CompDirtHaver dh) && !dh.hasDirt
            ) <= 1;
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        SendStandardLetter(
            def.letterLabel.Translate(),
            def.letterText.Translate(),
            LetterDefOf.NeutralEvent,
            parms,
            null
        );
        return true;
    }
}
