using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MSSFP.Lockpicking;

/// <summary>
/// Shared eligibility, duration, and success logic for hostile-faction door lockpicking.
/// </summary>
public static class LockpickUtility
{
    /// <summary>Work duration at 100% Manipulation (8 seconds).</summary>
    public const int BaseWorkTicks = 480;

    /// <summary>Floor so missing fingers slow the job instead of stalling it.</summary>
    public const float MinManipulation = 0.15f;

    public static bool Enabled => MSSFPMod.settings?.EnableLockpicking ?? true;

    public static MapComponent_LockpickedDoors Comp(Map map)
    {
        return map?.GetComponent<MapComponent_LockpickedDoors>();
    }

    public static bool IsPicked(Building_Door door)
    {
        if (door?.Map == null)
            return false;
        return Comp(door.Map)?.IsPicked(door) == true;
    }

    public static bool IsSpecialLockedDoor(Building_Door door)
    {
        return door is Building_HackableDoor || door is Building_JammedDoor;
    }

    public static bool IsFactionBlocked(Building_Door door, Pawn pawn)
    {
        if (door.Faction == null)
            return false;
        if (pawn.Faction != null && door.Faction == pawn.Faction)
            return false;
        return !GenAI.MachinesLike(door.Faction, pawn);
    }

    /// <summary>
    /// True when this door is a hostile-faction lockpick target for the pawn
    /// (not already picked, not a DLC hack/jam door).
    /// </summary>
    public static bool IsLockpickTarget(Building_Door door, Pawn pawn)
    {
        if (!Enabled)
            return false;
        if (door == null || pawn == null || !door.Spawned)
            return false;
        if (IsSpecialLockedDoor(door))
            return false;
        if (IsPicked(door))
            return false;
        return IsFactionBlocked(door, pawn);
    }

    public static bool PlayerCanUsePickedDoor(Pawn pawn)
    {
        if (pawn?.Faction != Faction.OfPlayer)
            return false;
        if (!pawn.CanOpenDoors)
            return false;
        if (pawn.FenceBlocked)
            return false;
        return true;
    }

    public static int WorkTicksFor(Pawn pawn)
    {
        float manip =
            pawn?.health?.capacities?.GetLevel(PawnCapacityDefOf.Manipulation) ?? 1f;
        manip = Mathf.Max(manip, MinManipulation);
        return Mathf.Max(60, Mathf.RoundToInt(BaseWorkTicks / manip));
    }

    public static void ApplySuccess(Pawn pawn, Building_Door door)
    {
        if (door == null || !door.Spawned)
            return;

        Comp(door.Map)?.MarkPicked(door);
        door.StartManualOpenBy(pawn);
    }
}
