using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(TickManager), nameof(TickManager.CurTimeSpeed), MethodType.Setter)]
internal static class TickManager_10SecondsToSpeed_Patch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        if (!MSSFPMod.settings.Enable10SecondsToSpeed || !MSSFPMod.settings.Active10SecondsToSpeed)
            return;

        TimeSpeed currentSpeed = Find.TickManager.CurTimeSpeed;

        if (currentSpeed == TimeSpeed.Ultrafast)
            return;

        if (!MSSFPMod.settings.IsSpeedMonitored(currentSpeed))
            return;

        SpeedChangeTask.CancelPendingSpeedChange();

        float targetTime =
            UnityEngine.Time.realtimeSinceStartup + MSSFPMod.settings.TenSecondsToSpeedDelay;
        SpeedChangeTask speedChangeTask = new SpeedChangeTask(targetTime);
        MSSFPGameManager.RegisterRealTimeSpeedChangeTask(speedChangeTask);
    }
}
