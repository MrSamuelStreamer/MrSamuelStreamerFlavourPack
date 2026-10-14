using Verse;

namespace MSSFP.Hediffs;

/// <summary>
/// Properties for <see cref="HediffComp_CubeImplant"/>. Mirrors the cadence of
/// vanilla <c>RimWorld.CompGoldenCube</c>: every <see cref="tickInterval"/> ticks
/// each eligible colonist/slave gets an MTB roll to gain CubeInterest.
/// </summary>
public class HediffCompProperties_CubeImplant : HediffCompProperties
{
    public int tickInterval = 2500;
    public float mtbDaysAddInterest = 1.0f;

    public HediffCompProperties_CubeImplant()
    {
        compClass = typeof(HediffComp_CubeImplant);
    }
}
