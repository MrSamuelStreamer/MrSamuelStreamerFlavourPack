using Verse;

namespace MSSFP.Hediffs;

public class HediffCompProperties_ApplyHediffWithinRadius : HediffCompProperties
{
    public HediffDef hediffDef;
    public bool targetsPlayer = true;
    public bool targetsAnimals = false;
    public bool targetsHumans = false;
    public bool targetsSubHumans = false;
    public bool targetsEntities = false;
    public bool targetsMechs = false;

    public float initialSeverity = 1f;

    public float proximityRadius = 6;
    public int ticksBetweenChecks = 600;

    public HediffCompProperties_ApplyHediffWithinRadius()
    {
        this.compClass = typeof(HediffComp_ApplyHediffWithinRadius);
    }

}
