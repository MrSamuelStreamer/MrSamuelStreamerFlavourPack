using System;
using System.Timers;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(TickManager), nameof(TickManager.CurTimeSpeed), MethodType.Setter)]
internal static class TickManager_10SecondsToSpeed_Patch
{
    private static Timer _speedUpTimer;
    private const double TimerIntervalMs = 10000.0;

    [HarmonyPostfix]
    private static void Postfix()
    {
        if (!MSSFPMod.settings.Enable10SecondsToSpeed)
            return;

        if (Find.TickManager.CurTimeSpeed == TimeSpeed.Ultrafast)
            return;

        if (Find.TickManager.CurTimeSpeed == TimeSpeed.Paused)
            return;

        _speedUpTimer?.Dispose();

        _speedUpTimer = new Timer(TimerIntervalMs)
        {
            AutoReset = false,
            Enabled = true,
        };

        _speedUpTimer.Elapsed += OnTimerElapsed;
    }

    private static void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
        if (!MSSFPMod.settings.Enable10SecondsToSpeed)
            return;

        Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;

        _speedUpTimer?.Dispose();
        _speedUpTimer = null;
    }
}
