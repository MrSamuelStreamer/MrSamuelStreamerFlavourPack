using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.HarmonyPatches;

// Modifies how long pawns wait before moving to a new spot while wandering
[HarmonyPatch(typeof(JobGiver_Wander), "TryGiveJob")]
public static class JobGiver_Wander_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Job __result)
    {
        // Skip if no job was created or feature is disabled
        if (__result == null || !MSSFPMod.settings.EnableWanderDelayModification)
            return;

        // Only modify wander wait jobs
        if (__result.def != JobDefOf.Wait_Wander)
            return;

        // Add the configured delay
        int delayTicks = MSSFPMod.settings.WanderDelayTicks;
        __result.expiryInterval += delayTicks;

        // Keep it above minimum (30 ticks = 0.5 seconds)
        if (__result.expiryInterval < 30)
            __result.expiryInterval = 30;
    }
}
