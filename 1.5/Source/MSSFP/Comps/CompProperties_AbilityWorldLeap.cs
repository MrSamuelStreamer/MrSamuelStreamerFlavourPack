using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class CompProperties_AbilityWorldLeap : CompProperties_AbilityFarskip
{
    public List<ThingDef> EdibleGems;
    public int ValueRequired = 1000;
    public int LeapRange = 8;

    public CompProperties_AbilityWorldLeap()
    {
        compClass = typeof(CompAbilityWorldLeap);
    }
}
