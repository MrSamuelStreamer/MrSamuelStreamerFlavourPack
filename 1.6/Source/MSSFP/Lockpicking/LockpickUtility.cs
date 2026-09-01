using RimWorld;
using UnityEngine;
using Verse;

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

    /// <summary>
    /// True when this door is a lockpick target for the pawn. Uses
    /// postfix returns true) and anything the pawn can already open drop out.
    /// </summary>
    public static bool IsLockpickTarget(Building_Door door, Pawn pawn)
    {
        if (!Enabled)
            return false;
        if (door == null || pawn == null || !door.Spawned)
            return false;
        if (IsSpecialLockedDoor(door))
            return false;
        return !door.PawnCanOpen(pawn);
    }

    public const int MinigameTumblers = 3;
    public const int MinigameTries = 3;
    public const float MinigameZoneMin = 0.10f;
    public const float MinigameZoneMax = 0.26f;
    public const float MinigameNeedleSpeed = 1.15f;
    public const float MinigameSpeedPerTumbler = 1.08f;
    public const float MinigameWinCraftingXp = 200f;
    public const float MinigameClickDebounce = 0.15f;

    public static float ManipulationLevel(Pawn pawn)
    {
        float manip = pawn?.health?.capacities?.GetLevel(PawnCapacityDefOf.Manipulation) ?? 1f;
        return Mathf.Clamp01(manip);
    }

    public static float MinigameZoneWidth(Pawn pawn)
    {
        return Mathf.Lerp(MinigameZoneMin, MinigameZoneMax, ManipulationLevel(pawn));
    }

    public static void RandomizeZone(float width, out float start)
    {
        start = Rand.Range(0f, Mathf.Max(0f, 1f - width));
    }

    public static float MinigameNeedleSpeedFor(int tumblersDone)
    {
        return MinigameNeedleSpeed * Mathf.Pow(MinigameSpeedPerTumbler, Mathf.Max(0, tumblersDone));
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
