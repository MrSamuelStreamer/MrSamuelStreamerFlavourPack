using System.Collections.Generic;
using HarmonyLib;
using MSSFP.Hediffs;
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
}
