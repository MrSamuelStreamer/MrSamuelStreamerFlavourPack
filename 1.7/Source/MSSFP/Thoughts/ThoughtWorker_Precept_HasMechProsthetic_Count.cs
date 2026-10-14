using RimWorld;
using Verse;

namespace MSSFP.Thoughts;

public class ThoughtWorker_Precept_HasMechProsthetic_Count
    : ThoughtWorker_Precept_HasProsthetic_Count
{
    public BodyModExtension ext => def.GetModExtension<BodyModExtension>();

    protected override ThoughtState ShouldHaveThought(Pawn p)
    {
        int num = ext.UnnaturalImplantCount(p);
        return num > 0 ? ThoughtState.ActiveAtStage(num - 1) : false;
    }
}
