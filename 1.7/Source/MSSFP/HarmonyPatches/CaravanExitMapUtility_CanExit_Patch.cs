using HarmonyLib;
using MSSFP.Holo;
using RimWorld.Planet;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Holos are leashed to their projector and cannot exit the map to form / join a caravan.
/// </summary>
[HarmonyPatch(typeof(CaravanExitMapUtility), nameof(CaravanExitMapUtility.CanExitMapAndJoinOrCreateCaravanNow))]
public static class CaravanExitMapUtility_CanExit_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn pawn, ref bool __result)
    {
        if (MSSFPHoloUtil.IsHolo(pawn))
            __result = false;
    }
}
