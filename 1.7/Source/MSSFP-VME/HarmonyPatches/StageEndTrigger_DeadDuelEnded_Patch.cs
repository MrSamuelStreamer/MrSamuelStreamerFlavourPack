using HarmonyLib;
using RimWorld;
using VanillaMemesExpanded;
using Verse;

namespace MSSFP.VME.HarmonyPatches;

[HarmonyPatch(typeof(StageEndTrigger_DeadDuelEnded), "Trigger")]
public static class StageEndTrigger_DeadDuelEnded_Patch
{
    public static void Postfix(
        LordJob_Ritual ritual,
        StageEndTrigger_DeadDuelEnded __instance,
        ref bool __result
    )
    {
        if (__result)
            return;
        if (!BloodCourtDuelUtility.IsNonLethalLeadershipDuel(ritual))
            return;
        if (__instance.roleIds == null)
            return;

        foreach (string roleId in __instance.roleIds)
        {
            foreach (Pawn pawn in ritual.assignments.AssignedPawns(roleId))
            {
                if (pawn.Downed)
                {
                    __result = true;
                    return;
                }
            }
        }
    }
}
