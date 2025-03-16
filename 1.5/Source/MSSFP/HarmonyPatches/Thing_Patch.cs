using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MSSFP.Animation;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(Thing))]
public static class Thing_Patch
{
    [HarmonyPatch(nameof(Thing.GetGizmos))]
    [HarmonyPostfix]
    public static void GetGizmos_Postfix(Thing __instance, ref IEnumerable<Gizmo> __result)
    {
        if (__instance is Pawn or AnimatedThingHolder)
            return;

        List<Gizmo> gizmos = __result.ToList();

        gizmos.Add(
            new Command_Action
            {
                defaultLabel = "MSS_FP_Animate".Translate(),
                defaultDesc = "MSS_FP_AnimateDesc".Translate(),
                action = delegate
                {
                    __instance.TryAnimate(MSSFPDefOf.MSSFPTestAnim);
                },
            }
        );

        __result = gizmos;
    }
}
