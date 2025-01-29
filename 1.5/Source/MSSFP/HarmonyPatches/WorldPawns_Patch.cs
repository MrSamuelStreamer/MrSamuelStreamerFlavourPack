using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(WorldPawns))]
public static class WorldPawns_Patch
{
    public static Lazy<FieldInfo> hediffPawns = new Lazy<FieldInfo>(()=>AccessTools.Field(typeof(WorldPawns), "hediffPawns"));
    [HarmonyPatch(nameof(WorldPawns.RemovePreservedPawnHediff))]
    [HarmonyPrefix]
    public static bool RemovePreservedPawnHediff(WorldPawns __instance, Pawn pawn)
    {
        /*
         * Odd bug here https://gist.github.com/HugsLibRecordKeeper/c256a65a643aeafe2337df5552481cdc#file-output_log-txt-L10693
         * Pawn referenced isn't in the dict. Patch the method to skip if not in the list
         */
        Dictionary<Pawn, List<Hediff>> dict = (Dictionary<Pawn, List<Hediff>>)hediffPawns.Value.GetValue(__instance);
        return dict == null || dict.ContainsKey(pawn);
    }
}
