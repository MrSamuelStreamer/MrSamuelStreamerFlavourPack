using HarmonyLib;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(Pawn))]
public static class Pawn_Patch
{
    [HarmonyPatch(nameof(Pawn.DrawGUIOverlay))]
    [HarmonyPrefix]
    public static bool DrawGUIOverlay_Prefix(Pawn __instance)
    {
        // hide pawn labels and overlays if sneaking
        if (!(__instance.health?.hediffSet?.hediffs?.Any(h => h.def == MSSFPDefOf.MSSFP_Sneaking) ?? false)) return true;

        foreach (ThingComp t in __instance.AllComps)
            t.DrawGUIOverlay();

        return false;

    }
}
