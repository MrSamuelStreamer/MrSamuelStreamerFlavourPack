using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(Pawn))]
public static class Pawn_Patches
{
    // [HarmonyPatch(nameof(Pawn.GetGizmos))]
    // [HarmonyPostfix]
    // public static void Pawn_GetGizmos_Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
    // {
    //     List<Gizmo> gizmos = new(__result);
    //     if (__instance.RaceProps.Humanlike)
    //     {
    //         gizmos.Add(
    //             new Command_Action
    //             {
    //                 defaultLabel = "MSSFP_TESTHAUNT".Translate(),
    //                 defaultDesc = "MSSFP_TESTHAUNTDESC".Translate(),
    //                 // icon = ContentFinder<Texture2D>.Get("UI/MSS_FP_Haunts_Toggle"),
    //                 action = delegate
    //                 {
    //                     __instance.Map.GetComponent<HauntAnimationController>().StartHaunting(__instance);
    //                 },
    //             }
    //         );
    //     }
    //
    //     __result = gizmos;
    // }

    // /// <summary>
    // /// Draw haunts
    // /// </summary>
    // /// <param name="__instance"></param>
    // /// <param name="drawLoc"></param>
    // [HarmonyPatch("DrawAt")]
    // [HarmonyPostfix]
    // public static void Pawn_PostDrawAt(Pawn __instance, Vector3 drawLoc)
    // {
    //     if (HauntsCache.Haunts.TryGetValue(__instance.thingIDNumber, out List<HediffComp_Haunt> haunts))
    //     {
    //         foreach (HediffComp_Haunt haunt in haunts)
    //         {
    //             haunt.DrawAt(drawLoc);
    //         }
    //     }
    // }

    /// <summary>
    /// Trigger froggomancer rescue randomly on pawn downed
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPatch(nameof(Pawn.Notify_Downed))]
    [HarmonyPostfix]
    public static void Pawn_Notify_Downed(Pawn __instance)
    {
        if (
            !__instance.Spawned
            || __instance.ageTracker.AgeBiologicalYears < 2
            || __instance.Map == null
            || __instance.Faction == null
            || __instance.RaceProps == null
            || !__instance.Faction.IsPlayer
            || !__instance.RaceProps.Humanlike
            || Rand.Chance(0.9f)
        )
            return;
        IncidentParms iParams = new() { target = __instance.Map };
        Find.Storyteller.TryFire(new FiringIncident(MSSFPDefOf.MSS_FroggomancerRescue, null, iParams), false);
    }
}
