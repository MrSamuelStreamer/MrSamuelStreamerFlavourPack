using System.Collections.Generic;
using Verse;

namespace MSSFP.VEE;

public class IncidentDefModExtension : DefModExtension
{
    public List<GameConditionDef> extraConditions = new List<GameConditionDef>();

    public IncidentDefModExtension() { }
}
