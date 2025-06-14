using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MSSFP.Utils;

public static class MapUtils
{
    public static IEnumerable<Thing> GetThingsInRadius(this Map map, IntVec3 center, int radius)
    {
        ModLog.Debug($"Getting things in radius {radius} from {center}");
        ;
        var cells = GenRadial.RadialCellsAround(center, radius, true);
        return cells.SelectMany(c => map.thingGrid.ThingsAt(c));
    }

    public static IEnumerable<Thing> GetThingsInRadius<T>(this Map map, IntVec3 center, int radius)
        where T : Thing
    {
        return GenRadial.RadialCellsAround(center, radius, true).SelectMany(c => map.thingGrid.ThingsAt(c)).OfType<T>();
    }
}
