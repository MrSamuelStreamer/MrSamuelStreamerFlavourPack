using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Projectiles;

public class Projectile_RoofCollapse : Projectile
{
    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        Map map = Map;
        IntVec3 center = hitThing?.Position ?? Position;
        if (map == null || !center.InBounds(map) || map.roofGrid.Roofed(center))
        {
            base.Impact(hitThing, blockedByShield);
            return;
        }

        List<IntVec3> cells = GenRadial
            .RadialCellsAround(center, 4f, true)
            .Where(cell => cell.InBounds(map))
            .ToList();

        foreach (IntVec3 cell in cells)
        {
            if (!map.roofGrid.Roofed(cell))
                map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
        }

        map.roofGrid.RoofGridUpdate();
        RoofCollapserImmediate.DropRoofInCells(cells, map);
        base.Impact(hitThing, blockedByShield);
    }
}
