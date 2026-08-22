using HarmonyLib;
using MSSFP.Utils;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(DateNotifier))]
public static class DateNotifier_Patch
{
    private static readonly AccessTools.FieldRef<DateNotifier, Season> LastSeasonRef =
        AccessTools.FieldRefAccess<DateNotifier, Season>("lastSeason");

    [HarmonyPatch(typeof(DateNotifier), nameof(DateNotifier.DateNotifierTick))]
    [HarmonyPrefix]
    public static void Prefix(DateNotifier __instance, out Season __state)
    {
        __state = LastSeasonRef(__instance);
    }

    [HarmonyPatch(typeof(DateNotifier), nameof(DateNotifier.DateNotifierTick))]
    [HarmonyPostfix]
    public static void Postfix(DateNotifier __instance, Season __state)
    {
        if(__state == Season.Undefined) return;
        if (__state != LastSeasonRef(__instance))
        {
            Find.SignalManager.SendSignal(new Signal(Signals.MSS_SeasonChanged));
        }
    }
}
