using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class CompProperties_FroggoHeal : CompProperties_AbilityEffect
{
    public CompProperties_FroggoHeal()
    {
        this.compClass = typeof (CompFroggoHeal);
    }
}
