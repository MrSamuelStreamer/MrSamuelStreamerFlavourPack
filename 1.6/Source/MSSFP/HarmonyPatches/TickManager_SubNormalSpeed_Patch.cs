using HarmonyLib;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(TickManager), nameof(TickManager.TickRateMultiplier), MethodType.Getter)]
internal static class TickManager_SubNormalSpeed_Patch
{
    [HarmonyPostfix]
    private static void Postfix(TickManager __instance, ref float __result)
    {
        if (!MSSFPMod.settings.EnableSubNormalSpeeds)
            return;

        if (__instance.CurTimeSpeed != TimeSpeed.Normal)
            return;

        if (__instance.slower is { ForcedNormalSpeed: true })
            return;

        SubNormalSpeedComponent comp = SubNormalSpeedComponent.Current;
        if (comp == null || comp.CurrentLevel == SlowLevel.None)
            return;

        __result *= comp.CurrentLevel.Multiplier();
    }
}
