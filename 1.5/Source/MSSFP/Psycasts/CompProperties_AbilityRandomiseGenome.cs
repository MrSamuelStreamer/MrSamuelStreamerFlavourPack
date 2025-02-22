using RimWorld;
using Verse;

namespace MSSFP.Psycasts;

public class CompProperties_AbilityRandomiseGenome: CompProperties_AbilityEffect
{
    public IntRange numberOfXenogenes = new IntRange(1, 20);
    public IntRange numberOfEndogenes = new IntRange(1, 20);

    public CompProperties_AbilityRandomiseGenome()
    {
        compClass = typeof(CompAbilityRandomiseGenome);
    }
}
