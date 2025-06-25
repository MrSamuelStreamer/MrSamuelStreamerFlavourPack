using RimWorld;
using Verse;

namespace MSSFP.Incidents;

public class IncidentWorker_TaffRaidEnemy : IncidentWorker_RaidEnemy
{
    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        parms.faction = Find.FactionManager.FirstFactionOfDef(MSSFPDefOf.MSSFP_TaffsFaction);

        if (parms.faction == null || parms.faction.defeated || parms.faction.AllyOrNeutralTo(Faction.OfPlayer))
            return false;

        return base.TryExecuteWorker(parms);
    }
}
