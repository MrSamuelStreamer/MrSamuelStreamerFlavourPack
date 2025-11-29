using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(PawnGenerator))]
public static class PawnGenerator_Patch
{
    public static Lazy<MethodInfo> PawnGenerator_IsValidCandidateToRedress = new(()=>AccessTools.Method(typeof(PawnGenerator), "IsValidCandidateToRedress"));

    public static bool IsValidCandidateToRedress(Pawn pawn, PawnGenerationRequest request) => (bool)PawnGenerator_IsValidCandidateToRedress.Value.Invoke(null, [pawn, request]);

    public static HashSet<WorldPawnSituation> ValidSituations = [WorldPawnSituation.Free, WorldPawnSituation.FactionLeader];

    [HarmonyPatch("GetValidCandidatesToRedress")]
    [HarmonyPrefix]
    private static bool GetValidCandidatesToRedress(PawnGenerationRequest request, ref IEnumerable<Pawn> __result)
    {
        if (!MSSFPMod.settings.OverrideFactionLeaderSpawn) return true;
        IEnumerable<Pawn> pawns = Find.WorldPawns.AllPawnsAliveOrDead.Where(x => ValidSituations.Contains(Find.WorldPawns.GetSituation(x)));
        if (request.KindDef.factionLeader && request.Faction != null)
            pawns = pawns.Concat(Find.WorldPawns.GetPawnsBySituation(WorldPawnSituation.FactionLeader).Where(x => x.Faction == request.Faction));
        __result = pawns.Where(x => IsValidCandidateToRedress(x, request));
        return false;
    }

    [HarmonyPatch("ChanceToRedressAnyWorldPawn")]
    [HarmonyPostfix]
    public static void ChanceToRedressAnyWorldPawn_Postfix(ref float __result)
    {
        if (!MSSFPMod.settings.BoostChanceToSpawnExistingPawns) return;
        __result = Mathf.Max(0.8f, __result * 20f);
    }

}
