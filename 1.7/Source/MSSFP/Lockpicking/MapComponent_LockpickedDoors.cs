using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Lockpicking;

/// <summary>
/// Persists which doors on this map have been lockpicked. Tracked by
/// </summary>
public class MapComponent_LockpickedDoors : MapComponent
{
    private HashSet<int> pickedDoorIds = [];

    public MapComponent_LockpickedDoors(Map map)
        : base(map) { }

    public bool IsPicked(Building_Door door)
    {
        return door != null && pickedDoorIds.Contains(door.thingIDNumber);
    }

    public void MarkPicked(Building_Door door)
    {
        if (door == null)
            return;

        if (!pickedDoorIds.Add(door.thingIDNumber))
            return;

        map?.reachability?.ClearCache();
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref pickedDoorIds, "pickedDoorIds", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            pickedDoorIds ??= [];
    }
}
