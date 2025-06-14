using MSSFP.Needs;
using RimWorld;
using Verse;

namespace MSSFP.Thoughts;

public class ThoughtWorker_GeneStealer : ThoughtWorker
{
    public const float MinLevelForThought = 0.3f;

    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (!ModsConfig.BiotechActive)
            return ThoughtState.Inactive;
        Need_GeneStealer need = p.needs?.TryGetNeed<Need_GeneStealer>();
        return need != null && need.CurLevel <= 0.30000001192092896;
    }
}
