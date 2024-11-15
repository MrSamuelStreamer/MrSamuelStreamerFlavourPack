using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP;

public class StorytellerCompFroggeSpawn : StorytellerComp
{
    protected StorytellerCompProperties_FroggeSpawn Props => (StorytellerCompProperties_FroggeSpawn) props;

    public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
    {
        if (Rand.MTBEventOccurs(Props.mtbDays, 60000f, 1000f))
        {
            yield return new FiringIncident(Props.incident, this, GenerateParms(Props.incident.category, target));
        }
    }
}
