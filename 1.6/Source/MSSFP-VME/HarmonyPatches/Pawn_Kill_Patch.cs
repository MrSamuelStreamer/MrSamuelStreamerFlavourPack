using HarmonyLib;
using Verse;

namespace MSSFP.VME.HarmonyPatches;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
public static class Pawn_Kill_Patch
{
    public static bool Prefix(Pawn __instance)
    {
        return !BloodCourtDuelUtility.IsNonLethalLeadershipDuelist(__instance);
    }
}
