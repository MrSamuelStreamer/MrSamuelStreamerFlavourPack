using HarmonyLib;
using MSSFP.ModExtensions;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(IncidentWorker_FarmAnimalsWanderIn))]
public static class IncidentWorker_FarmAnimalsWanderIn_Patch
{
    [HarmonyPatch("SelectionChance")]
    [HarmonyPostfix]
    public static void SelectionChancePatch(PawnKindDef pawnKind, ref float __result)
    {
        if (
            !MSSFPMod.settings.EnableFroggeIncidents
            && pawnKind.HasModExtension<FroggeModExtension>()
        )
            __result = 0f;
    }
}
