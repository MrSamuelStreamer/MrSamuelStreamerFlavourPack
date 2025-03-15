using HarmonyLib;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Patch to mark pawn as illiterate if they have the gene
/// </summary>
[HarmonyPatch(typeof(BookUtility))]
public static class BookUtility_Patch
{
    [HarmonyPatch(nameof(BookUtility.CanReadEver))]
    [HarmonyPrefix]
    public static bool CanReadEver(Pawn reader, ref bool __result)
    {
        if (reader.genes.HasActiveGene(MSSFPDefOf.MSSFP_Illiterate))
        {
            __result = false;
            return false;
        }

        return true;
    }
}
