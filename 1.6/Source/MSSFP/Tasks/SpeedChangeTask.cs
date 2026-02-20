using System;
using MSSFP.Interfaces;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

public class SpeedChangeTask : IOnThreadTask
{
    private static SpeedChangeTask _pendingTask;
    private float _targetTime;

    public static void CancelPendingSpeedChange()
    {
        if (_pendingTask != null)
        {
            MSSFPGameManager.UnregisterRealTimeSpeedChangeTask(_pendingTask);
            _pendingTask = null;
        }
    }

    public SpeedChangeTask(float targetTime)
    {
        _targetTime = targetTime;
        _pendingTask = this;
    }

    public void OnThreadTask(MSSFPGameManager manager)
    {
        if (_pendingTask == this)
            _pendingTask = null;

        if (!MSSFPMod.settings.Enable10SecondsToSpeed || !MSSFPMod.settings.Active10SecondsToSpeed)
            return;

        TimeSpeed currentSpeed = Find.TickManager.CurTimeSpeed;

        if (!MSSFPMod.settings.IsSpeedMonitored(currentSpeed))
            return;

        if (currentSpeed == TimeSpeed.Ultrafast)
            return;

        Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
    }

    public bool ShouldExecute(float currentTime)
    {
        return currentTime >= _targetTime;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref _targetTime, "targetTime");
    }
}
