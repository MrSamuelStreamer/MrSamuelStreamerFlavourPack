using System.Collections.Generic;
using HarmonyLib;
using MSSFP.Hediffs;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(Pawn))]
public static class Pawn_Patches
{

    [HarmonyPatch("DrawAt")]
    [HarmonyPostfix]
    public static void Pawn_PostDrawAt(Pawn __instance, Vector3 drawLoc)
    {
        if (HauntsCache.Haunts.TryGetValue(__instance.thingIDNumber, out List<HediffComp_Haunt> haunts))
        {
            foreach (HediffComp_Haunt haunt in haunts)
            {
                haunt.DrawAt(drawLoc);
            }
        }
    }

    [HarmonyPatch(nameof(Pawn.Notify_Downed))]
    [HarmonyPostfix]
    public static void Pawn_Notify_Downed(Pawn __instance)
    {
        if(!__instance.Spawned) return;
        if(__instance.ageTracker.AgeBiologicalYears < 2) return;
        if(__instance.Map == null) return;
        if(!__instance.Faction.IsPlayer || !__instance.RaceProps.Humanlike || Rand.Chance(0.9f)) return;
        IncidentParms iParams = new IncidentParms { target = __instance.Map };
        Find.Storyteller.TryFire(new FiringIncident(MSSFPDefOf.MSS_FroggomancerRescue, null, iParams), false);
    }
}
