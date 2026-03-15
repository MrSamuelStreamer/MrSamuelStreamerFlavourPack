using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(PawnGenerator))]
public static class PawnGenerator_Patch
{
    // Boost the chance that an existing Free world pawn is reused for any spawn request,
    // creating "familiar faces" continuity. Explicitly skips faction-leader-kind requests
    // so leader generation always produces a fresh pawn (preserved by Faction_Patch).
    [HarmonyPatch("ChanceToRedressAnyWorldPawn")]
    [HarmonyPostfix]
    public static void ChanceToRedressAnyWorldPawn_Postfix(PawnGenerationRequest request, ref float __result)
    {
        if (!MSSFPMod.settings.BoostChanceToSpawnExistingPawns) return;
        if (request.KindDef.factionLeader) return;
        __result = Mathf.Max(0.8f, __result * 20f);
    }
}
