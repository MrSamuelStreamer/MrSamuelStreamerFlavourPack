using Verse;

namespace MSSFP.Hediffs;

public class HediffCompProperties_ExplodeOnDeath : HediffCompProperties
{
    public EffecterDef effecter;

    public HediffCompProperties_ExplodeOnDeath()
    {
        compClass = typeof(HediffComp_ExplodeOnDeath);
    }
}
