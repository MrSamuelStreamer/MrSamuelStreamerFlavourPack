using MSSFP.Needs;
using RimWorld;
using Verse;

namespace MSSFP.Thoughts;

public class ThoughtWorker_Situational_GeneStealer : Thought_Situational
{
    private static readonly SimpleCurve MoodOffsetCurve = new()
    {
        { new CurvePoint(0.301f, 0.0f), true },
        { new CurvePoint(0.3f, -14f), true },
        { new CurvePoint(0.0f, -50f), true },
    };

    public override float MoodOffset()
    {
        Need_GeneStealer need = pawn.needs?.TryGetNeed<Need_GeneStealer>();
        return need == null ? 0.0f : MoodOffsetCurve.Evaluate(need.CurLevel);
    }
}
