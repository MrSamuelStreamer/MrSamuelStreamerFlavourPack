using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.VOE.Hediffs;

public class HediffCompProperties_OutpostSuppression : HediffCompProperties
{
    public List<WorldObjectDef> OutpostDefs = new List<WorldObjectDef>();

    public HediffCompProperties_OutpostSuppression()
    {
        compClass = typeof(HediffCompOutpostSuppression);
    }
}
