using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class CompProperties_AbilitySummonPawn : CompProperties_EffectWithDest
{
    public PawnKindDef pawnDef;
    public int count;

    public CompProperties_AbilitySummonPawn()
    {
        compClass = typeof(CompAbilitySummonPawn);
    }
}
