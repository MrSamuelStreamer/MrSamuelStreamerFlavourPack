using Verse;

namespace MSSFP.Comps;

public class CompProperties_UpgradableBed : CompProperties
{
    public SimpleCurve wellRestedCurve = new SimpleCurve()
    {
        new CurvePoint(0f, 0.025f),
        new CurvePoint(5f, 0.05f),
        new CurvePoint(10f, 0.11f),
        new CurvePoint(50f, 0.70f),
        new CurvePoint(100f, 1f),
    };

    public CompProperties_UpgradableBed()
    {
        compClass = typeof(CompUpgradableBed);
    }
}
