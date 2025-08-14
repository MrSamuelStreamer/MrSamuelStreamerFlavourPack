using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Incidents;

public class IncidentWorker_RaidEnemySkylantern : IncidentWorker_RaidEnemy
{
    protected override bool CanFireNowSub(IncidentParms parms) =>
        MSSFPMod.settings.EnableSkylanternRaids;

    protected override void PostProcessSpawnedPawns(IncidentParms parms, List<Pawn> pawns)
    {
        foreach (Pawn pawn in pawns)
        {
            foreach (
                BodyPartRecord bodyPartRecord in pawn.def.race.body.AllParts.OfType<BodyPartRecord>()
            )
            {
                if (!Rand.Chance(.05f))
                    continue;

                Hediff hediff = pawn.health.AddHediff(MSSFPDefOf.Burn, bodyPartRecord);
                hediff.Severity = Rand.Range(.01f, 0.8f);
            }
        }
    }
}
