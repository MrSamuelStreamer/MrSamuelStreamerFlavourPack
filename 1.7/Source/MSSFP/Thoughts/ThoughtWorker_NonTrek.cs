using RimWorld;
using Verse;

namespace MSSFP.Thoughts;

public class ThoughtWorker_NonTrek : ThoughtWorker
{
    protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn other)
    {
        if (!p.RaceProps.Humanlike)
            return false;
        if (p.story is not { Adulthood.defName: "MSSFP_Trek" })
            return false;
        return other.RaceProps.Humanlike && RelationsUtility.PawnsKnowEachOther(p, other);
    }
}
