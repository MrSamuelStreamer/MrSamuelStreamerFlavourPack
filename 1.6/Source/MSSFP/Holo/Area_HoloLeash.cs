using UnityEngine;
using Verse;

namespace MSSFP.Holo;

/// <summary>
/// Per-projector private leash area (P0 decision: Layer 1 — comp-owned, NOT registered with
/// AreaManager.areas). Constructed via <c>new Area_HoloLeash(map.areaManager)</c>; areaManager
/// is set so base internals (BoolGrid sizing, Map property) resolve, but the area is never
/// added to <c>areaManager.areas</c> so it does not appear in any vanilla area UI.
///
/// Scribed deep by <see cref="CompHoloProjector"/> in PostExposeData.
/// </summary>
public class Area_HoloLeash : Area
{
    /// <summary>Unique suffix per projector so two on the same map don't collide on LoadID.</summary>
    public int projectorThingID = -1;

    public override string Label => "Holo leash";

    public override Color Color => new(0.4f, 0.6f, 1.0f);

    public override int ListPriority => 0;

    /// <summary>Hidden from the player area-assignment UI (private to the projector).</summary>
    public override bool AssignableAsAllowed() => false;

    public Area_HoloLeash() { }

    public Area_HoloLeash(AreaManager areaManager) : base(areaManager) { }

    public override string GetUniqueLoadID()
    {
        return $"Area_{ID}_HoloLeash_{projectorThingID}";
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref projectorThingID, "projectorThingID", -1);
    }

    /// <summary>
    /// Repaint the BoolGrid as a filled circle of <paramref name="radius"/> cells around
    /// <paramref name="center"/>. Caller is responsible for picking a sensible center (the
    /// projector's current cell). Safe to call repeatedly — clears prior state first.
    /// </summary>
    /// <remarks>
    /// Setting cells via the indexer routes through <c>Area.Set</c>, which marks the drawer
    /// dirty AND notifies the map pathfinder + regions, so existing Goto jobs inside the
    /// new area remain valid and pawns react to the boundary change.
    /// </remarks>
    public void RefreshFromCenter(IntVec3 center, float radius)
    {
        if (areaManager == null)
            return;
        Map map = Map;
        if (map == null)
            return;
        Clear();
        int r = Mathf.CeilToInt(radius);
        float rSq = radius * radius;
        for (int dx = -r; dx <= r; dx++)
        for (int dz = -r; dz <= r; dz++)
        {
            if (dx * dx + dz * dz > rSq)
                continue;
            IntVec3 c = new IntVec3(center.x + dx, 0, center.z + dz);
            if (!c.InBounds(map))
                continue;
            this[c] = true;
        }
    }
}
