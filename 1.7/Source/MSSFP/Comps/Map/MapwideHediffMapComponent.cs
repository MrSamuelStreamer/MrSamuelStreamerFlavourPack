using System.Collections.Generic;
using MSSFP.Defs;
using Verse;

namespace MSSFP.Comps.Map;

public class MapwideHediffMapComponent(Verse.Map map) : MapComponent(map)
{
    public HashSet<MapwideHediffDef> _defs;

    public HashSet<MapwideHediffDef> Defs
    {
        get
        {
            _defs ??= [..DefDatabase<MapwideHediffDef>.AllDefsListForReading];
            return _defs;
        }
    }
    public override void MapComponentTick()
    {
        foreach (MapwideHediffDef mapwideHediffDef in Defs)
        {
            if (map.IsHashIntervalTick(mapwideHediffDef.ticksBetweenChecks))
            {
                mapwideHediffDef.Worker.Apply(map);
            }
        }
    }
}
