using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Hediffs;

public class HediffCompProperties_BuildBase : HediffCompProperties
{
    public List<JobDef> ConstructJobs;
    public ThoughtDef GoodThought;
    public ThoughtDef BadThought;

    public HediffCompProperties_BuildBase()
    {
        this.compClass = typeof(HediffComp_BuildBase);
    }
}
