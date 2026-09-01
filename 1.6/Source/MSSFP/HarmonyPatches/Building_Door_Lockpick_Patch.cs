using HarmonyLib;
using MSSFP.Lockpicking;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// After a door is lockpicked, player pawns that can use doors may open it
/// even though it still belongs to a hostile faction. Inspect string notes
/// the picked state.
/// </summary>
[HarmonyPatch(typeof(Building_Door), nameof(Building_Door.PawnCanOpen))]
public static class Building_Door_Lockpick_Patch
{
    [HarmonyPostfix]
    public static void PawnCanOpen_Postfix(Building_Door __instance, Pawn p, ref bool __result)
    {
        if (__result)
            return;
        if (!LockpickUtility.Enabled)
            return;
        if (!LockpickUtility.PlayerCanUsePickedDoor(p))
            return;
        if (!LockpickUtility.IsPicked(__instance))
            return;

        __result = true;
    }
}

[HarmonyPatch(typeof(Building_Door), nameof(Building_Door.GetInspectString))]
public static class Building_LockpickInspectString_Patch
{
    [HarmonyPostfix]
    public static void GetInspectString_Postfix(Building_Door __instance, ref string __result)
    {
        if (!LockpickUtility.Enabled || !LockpickUtility.IsPicked(__instance))
            return;

        string line = "MSSFP_DoorLockpicked".Translate();
        __result = __result.NullOrEmpty() ? line : __result + "\n" + line;
    }
}
