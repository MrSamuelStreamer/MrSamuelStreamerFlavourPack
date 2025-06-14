using RimWorld;
using Verse;

namespace MSSFP.Thoughts;

public class ThoughtWorker_Precept_HasMechProsthetic : ThoughtWorker_Precept_HasProsthetic
{
    public BodyModExtension ext => def.GetModExtension<BodyModExtension>();

    protected override ThoughtState ShouldHaveThought(Pawn p)
    {
        return ext.UnnaturalImplantCount(p) > 0;
    }
}
