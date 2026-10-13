using HarmonyLib;
using RimWorld;

namespace MSSFP.VME.HarmonyPatches;

[HarmonyPatch(typeof(Precept_Ritual), nameof(Precept_Ritual.AddObligation))]
public static class Precept_Ritual_AddObligation_Patch
{
    public static bool Prefix(Precept_Ritual __instance)
    {
        if (!BloodCourtDuelUtility.Enabled)
            return true;
        if (!BloodCourtDuelUtility.IsLeadershipChallenge(__instance))
            return true;
        return WorldComponent_BloodCourtDuelCooldowns.Instance?.IsOnCooldown() != true;
    }
}
