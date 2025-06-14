using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class CompProperties_AbilityBodyHopImproved : CompProperties_AbilityEffect
{
    public HediffDef hediffOnSelf;

    public CompProperties_AbilityBodyHopImproved()
    {
        compClass = typeof(CompAbilityEffect_BodyHopImproved);
    }
}
