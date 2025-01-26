using HarmonyLib;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(ResearchProjectDef))]
public static class ResearchProjectDef_Patches
{
    [HarmonyPatch(nameof(ResearchProjectDef.IsHidden), MethodType.Getter)]
    [HarmonyPostfix]
    public static void IsHidden(ResearchProjectDef __instance, ref bool __result)
    {
        if(__instance != MSSFPDefOf.MSS_FroggeLeapResearch) return;

        __result = true;
        FroggeLeapResearchComponent comp = Find.World.GetComponent<FroggeLeapResearchComponent>();

        if (comp is { EventHasFired: true }) __result = false;
    }
}
