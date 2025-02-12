using RimWorld;
using Verse;

namespace MSSFP.Thoughts;

public class ThoughtWorker_Precept_HasMechProsthetic_Social: ThoughtWorker_Precept_HasProsthetic_Social
{
    public BodyModExtension ext => def.GetModExtension<BodyModExtension>();

    protected override ThoughtState ShouldHaveThought(Pawn p, Pawn otherPawn)
    {
        return ext.UnnaturalImplantCount(otherPawn) > 0;
    }
}
