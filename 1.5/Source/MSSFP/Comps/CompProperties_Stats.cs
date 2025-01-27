using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class CompProperties_Stats: CompProperties
{
    public List<StatModifier> offsets;
    public List<StatModifier> factors;
    public string explanation;

    public CompProperties_Stats()
    {
        compClass = typeof(CompStats);
    }
}
