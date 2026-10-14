using RimWorld;

namespace MSSFP.Comps;

public class CompProperties_AbilityEcholocation : CompProperties_AbilityEffect
{
    public int durationTicks = 900;
    public CompProperties_AbilityEcholocation()
    {
        compClass = typeof(CompAbilityEffect_Echolocation);
    }
}
