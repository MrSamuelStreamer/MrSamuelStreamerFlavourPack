using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Prefix to allow the ageing slider in difficulty settings to go to 0.
/// Additionally to allow decimals to one point in the slider, if necessary.
/// </summary>

[HarmonyPatch]
public static class AgeingStorytellerPatch
{
    public static MethodBase TargetMethod()
    {
        return AccessTools.GetDeclaredMethods(typeof(StorytellerUI))
            .First(method =>
                method.Name == "DrawCustomDifficultySlider"
                && method.GetParameters().Any(p => p.Name == "optionName")
                && method.GetParameters().Any(p => p.Name == "min")
                && method.GetParameters().Any(p => p.Name == "precision"));
    }
    [HarmonyPrefix]
    public static void Prefix(
        string optionName,
        ref float min,
        ref float precision)
    {
        if (optionName != "childAgingRate" && optionName != "adultAgingRate")
            return;

        min = 0f;
        precision = 0.1f;
    }
}
