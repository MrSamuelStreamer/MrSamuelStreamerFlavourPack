using HarmonyLib;
using MSSFP.Questing;
using RimWorld;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(ScenarioMaker))]
public static class ScenarioMaker_Patch
{
    [HarmonyPatch(nameof(ScenarioMaker.MakeScenPart))]
    [HarmonyPostfix]
    public static void MakeScenPart_Postfix(ref ScenPart __result)
    {
        if(__result is ScenPart_Pursuers pursuers) pursuers.PostLoad();
    }
}
