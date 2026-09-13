using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.VME.HarmonyPatches;

[HarmonyPatch(typeof(RitualBehaviorWorker), nameof(RitualBehaviorWorker.CanStartRitualNow))]
public static class RitualBehaviorWorker_CanStartRitualNow_Patch
{
    public static void Postfix(Precept_Ritual ritual, ref string __result)
    {
        if (__result != null)
            return;
        if (!BloodCourtDuelUtility.Enabled)
            return;
        if (!BloodCourtDuelUtility.IsLeadershipChallenge(ritual))
            return;

        WorldComponent_BloodCourtDuelCooldowns cooldowns = WorldComponent_BloodCourtDuelCooldowns.Instance;
        if (cooldowns?.IsOnCooldown() != true)
            return;

        __result = "MSS_FP_BloodCourt_RitualOnCooldown".Translate(cooldowns.DaysRemaining()).CapitalizeFirst();
    }
}
