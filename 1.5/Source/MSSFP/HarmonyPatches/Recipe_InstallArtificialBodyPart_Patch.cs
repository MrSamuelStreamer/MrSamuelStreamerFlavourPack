using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(Recipe_InstallArtificialBodyPart))]
public static class Recipe_InstallArtificialBodyPart_Patch
{
    [HarmonyPatch(nameof(Recipe_InstallArtificialBodyPart.GetPartsToApplyOn))]
    [HarmonyPostfix]
    public static void GetPartsToApplyOn_Patch(Pawn pawn, ref IEnumerable<BodyPartRecord> __result)
    {
        if (pawn.genes.HasActiveGene(MSSFPDefOf.MSSFP_Tuvix))
        {
            __result = new List<BodyPartRecord>();
        }
    }
}
