using System;
using System.Reflection;
using HarmonyLib;
using MSSFP.Utils;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(DateNotifier))]
public static class DateNotifier_Patch
{
    public static Lazy<FieldInfo> lastSeason = new(() => AccessTools.Field(typeof(DateNotifier), "lastSeason"));

    [HarmonyPatch(typeof(DateNotifier), nameof(DateNotifier.DateNotifierTick))]
    [HarmonyPrefix]
    public static void Prefix(DateNotifier __instance, out Season __state)
    {
        __state = (Season)lastSeason.Value.GetValue(__instance);
    }

    [HarmonyPatch(typeof(DateNotifier), nameof(DateNotifier.DateNotifierTick))]
    [HarmonyPostfix]
    public static void Postfix(DateNotifier __instance, Season __state)
    {
        if(__state == Season.Undefined) return;
        if (__state != (Season) lastSeason.Value.GetValue(__instance))
        {
            Find.SignalManager.SendSignal(new Signal(Signals.MSS_SeasonChanged));
        }
    }
}
