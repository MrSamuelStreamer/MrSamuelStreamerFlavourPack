using HarmonyLib;
using MSSFP.Comps.World;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Hide frogge research until the event fires
/// </summary>
[HarmonyPatch(typeof(ResearchProjectDef))]
public static class ResearchProjectDef_Patches
{
    [HarmonyPatch(nameof(ResearchProjectDef.IsHidden), MethodType.Getter)]
    [HarmonyPostfix]
    public static void IsHidden(ResearchProjectDef __instance, ref bool __result)
    {
        if (!MSSFPMod.settings.EnableOskarianTech && __instance == MSSFPDefOf.MSS_Oskarian_Technology)
        {
            __result = true;
            return;
        }

        if (__instance != MSSFPDefOf.MSS_FroggeLeapResearch)
        {
            __result = true;
            FroggeLeapResearchComponent comp = Find.World.GetComponent<FroggeLeapResearchComponent>();

            if (comp is { EventHasFired: true })
                __result = false;
        }
    }
}
