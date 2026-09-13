using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using VanillaMemesExpanded;
using Verse;

namespace MSSFP.VME.HarmonyPatches;

[HarmonyPatch(typeof(RitualObligationTrigger_StrongerLeader), nameof(RitualObligationTrigger_StrongerLeader.Tick))]
public static class RitualObligationTrigger_StrongerLeader_Patch
{
    public static void Postfix(RitualObligationTrigger_StrongerLeader __instance)
    {
        if (__instance.tickCounter != 0)
            return;
        if (!BloodCourtDuelUtility.Enabled)
            return;
        if (WorldComponent_BloodCourtDuelCooldowns.Instance?.IsOnCooldown() != true)
            return;
        if (__instance.ritual?.activeObligations == null)
            return;

        // Sweeps obligations queued in older saves; new ones are blocked by Precept_Ritual_AddObligation_Patch.
        List<RitualObligation> toRemove = __instance.ritual.activeObligations.ToList();
        foreach (RitualObligation obligation in toRemove)
            __instance.ritual.RemoveObligation(obligation);
    }
}
