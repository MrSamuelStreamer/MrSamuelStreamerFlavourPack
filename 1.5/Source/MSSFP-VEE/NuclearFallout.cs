using System.Collections.Generic;
using System.Linq;
using RimWorld;
using VEE;
using Verse;

namespace MSSFP.VEE;

public class NuclearFallout : GameCondition
{
    public SkyColorSet FalloutRainColors = new(new ColorInt(20, 91, 17).ToColor, new ColorInt(7, 54, 5).ToColor, new ColorInt(25, 149, 21).ToColor, 0.85f);
    public readonly List<SkyOverlay> overlays = [new WeatherOverlay_Rain()];

    public override bool AllowEnjoyableOutsideNow(Map map) => false;

    public override WeatherDef ForcedWeather()
    {
        Map currentMap = Find.CurrentMap;
        int num1;
        if (currentMap == null)
        {
            num1 = 0;
        }
        else
        {
            float? outdoorTemp = currentMap.mapTemperature?.OutdoorTemp;
            float num2 = 0.0f;
            num1 = outdoorTemp.GetValueOrDefault() <= (double)num2 & outdoorTemp.HasValue ? 1 : 0;
        }
        return num1 != 0 ? VEE_DefOf.SnowHard : VEE_DefOf.Rain;
    }

    public override void Init()
    {
        LessonAutoActivator.TeachOpportunity(ConceptDefOf.ForbiddingDoors, OpportunityType.Critical);
        LessonAutoActivator.TeachOpportunity(ConceptDefOf.AllowedAreas, OpportunityType.Critical);
    }

    public override void GameConditionTick()
    {
        foreach (Map affectedMap in AffectedMaps)
        {
            foreach (SkyOverlay t in overlays)
                t.TickOverlay(affectedMap);
        }
    }

    public override void GameConditionDraw(Map map)
    {
        foreach (SkyOverlay t in overlays)
            t.DrawOverlay(map);
    }

    public override float SkyTargetLerpFactor(Map map)
    {
        return GameConditionUtility.LerpInOutValue(this, 5000f, 0.5f);
    }

    public override SkyTarget? SkyTarget(Map map)
    {
        return new SkyTarget(0.85f, this.FalloutRainColors, 1f, 1f);
    }

    public override List<SkyOverlay> SkyOverlays(Map map) => overlays;
}
