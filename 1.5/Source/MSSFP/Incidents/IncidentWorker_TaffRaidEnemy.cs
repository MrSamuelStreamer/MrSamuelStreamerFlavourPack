using RimWorld;
using Verse;

namespace MSSFP.Incidents;

public class IncidentWorker_TaffRaidEnemy : IncidentWorker_RaidEnemy
{
    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        parms.faction = Find.FactionManager.FirstFactionOfDef(MSSFPDefOf.MSSFP_TaffsFaction);
        if (parms.faction == null)
        {
            FactionGenerator.CreateFactionAndAddToManager(MSSFPDefOf.MSSFP_TaffsFaction);
            parms.faction = Find.FactionManager.FirstFactionOfDef(MSSFPDefOf.MSSFP_TaffsFaction);
        }

        return base.TryExecuteWorker(parms);
    }
}
