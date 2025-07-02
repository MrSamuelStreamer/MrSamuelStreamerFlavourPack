using UnityEngine;
using Verse;

namespace MSSFP;

public class BalloonPawnFlyerWorker : PawnFlyerWorker
{
    public BalloonPawnFlyerWorker(PawnFlyerProperties properties)
        : base(properties) { }

    public override float AdjustedProgress(float t)
    {
        AnimationCurve progressCurve = properties.ProgressCurve;
        return progressCurve == null || progressCurve.length == 0 ? t : progressCurve.Evaluate(t);
    }

    public override float GetHeight(float t) => t;
}
