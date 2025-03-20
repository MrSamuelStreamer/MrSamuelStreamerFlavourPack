using RimWorld;
using Verse;

namespace MSSFP.Thoughts;

public class ThoughtWorker_Precept_HasNoMechProsthetic : ThoughtWorker_Precept_HasNoProsthetic
{
    public BodyModExtension ext => def.GetModExtension<BodyModExtension>();

    protected override ThoughtState ShouldHaveThought(Pawn p)
    {
        return ext.UnnaturalImplantCount(p) <= 0;
    }
}
