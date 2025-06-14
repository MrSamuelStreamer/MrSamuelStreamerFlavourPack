using RimWorld;

namespace MSSFP.Thoughts;

public class Thought_NonTrekThought : Thought_SituationalSocial
{
    public override float OpinionOffset()
    {
        if (ThoughtUtility.ThoughtNullified(pawn, def))
            return 0.0f;

        if (pawn.story is not { Adulthood.defName: "MSSFP_Trek" })
            return 0.0f;

        return otherPawn?.story?.Adulthood?.defName != "MSSFP_Trek" ? 100f : 0f;
    }
}
