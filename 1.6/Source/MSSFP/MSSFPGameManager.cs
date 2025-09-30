using System;
using System.Collections.Generic;
using System.Linq;
using MSSFP.HarmonyPatches;
using MSSFP.Interfaces;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP;

public class MSSFPGameManager : GameComponent
{
    public static MSSFPGameManager Manager;

    public MSSFPGameManager(Game game)
    {
        Manager = this;
        Game = game;
    }

    public Game Game { get; }

    protected Dictionary<int, List<IOnThreadTask>> ScheduledOnThreadTasks;
    protected Dictionary<int, List<IOffThreadTask>> ScheduledOffThreadTasks;
    protected List<SpeedChangeTask> RealTimeSpeedChangeTasks;

    public override void ExposeData()
    {
        Scribe_Collections.Look(
            ref ScheduledOnThreadTasks,
            "ScheduledOnThreadTasks",
            LookMode.Value,
            LookMode.Deep
        );
        Scribe_Collections.Look(
            ref ScheduledOffThreadTasks,
            "ScheduledOffThreadTasks",
            LookMode.Value,
            LookMode.Deep
        );
        Scribe_Collections.Look(
            ref RealTimeSpeedChangeTasks,
            "RealTimeSpeedChangeTasks",
            LookMode.Deep
        );
    }

    private static TimeSpeed _lastKnownSpeed = TimeSpeed.Normal;

    public override void GameComponentUpdate()
    {
        if (Current.ProgramState != ProgramState.Playing)
            return;

        // Fallback detection for pause button (doesn't trigger Harmony patch)
        TimeSpeed currentSpeed = Find.TickManager.CurTimeSpeed;
        if (currentSpeed != _lastKnownSpeed)
        {
            if (
                MSSFPMod.settings.Enable10SecondsToSpeed
                && MSSFPMod.settings.IsSpeedMonitored(currentSpeed)
                && currentSpeed != TimeSpeed.Ultrafast
            )
            {
                SpeedChangeTask.CancelPendingSpeedChange();

                float targetTime =
                    Time.realtimeSinceStartup + MSSFPMod.settings.TenSecondsToSpeedDelay;
                SpeedChangeTask speedChangeTask = new SpeedChangeTask(targetTime);
                RegisterRealTimeSpeedChangeTask(speedChangeTask);
            }

            _lastKnownSpeed = currentSpeed;
        }

        // Execute ready tasks
        if (RealTimeSpeedChangeTasks != null && RealTimeSpeedChangeTasks.Count > 0)
        {
            float currentTime = Time.realtimeSinceStartup;
            var tasksToExecute = RealTimeSpeedChangeTasks
                .Where(task => task.ShouldExecute(currentTime))
                .ToList();

            foreach (var task in tasksToExecute)
            {
                try
                {
                    task.OnThreadTask(this);
                }
                catch (Exception e)
                {
                    Log.Error($"Exception occurred in SpeedChangeTask: {e}");
                }
                RealTimeSpeedChangeTasks.Remove(task);
            }
        }
    }

    public override void GameComponentOnGUI()
    {
        if (Current.ProgramState != ProgramState.Playing)
            return;

        if (
            DefDatabase<KeyBindingDef>.GetNamed("MSSFP_Toggle10SecondsToSpeed")?.KeyDownEvent
            ?? false
        )
        {
            MSSFPMod.settings.Enable10SecondsToSpeed = !MSSFPMod.settings.Enable10SecondsToSpeed;
            Event.current?.Use();
        }
    }

    public override void GameComponentTick()
    {
        if (
            !ScheduledOffThreadTasks.NullOrEmpty()
            && ScheduledOffThreadTasks.TryGetValue(
                Find.TickManager.TicksGame,
                out List<IOffThreadTask> offThreadTasks
            )
        )
        {
            GenThreading.ParallelForEach(
                offThreadTasks,
                task =>
                {
                    task.OffThreadTask(this);
                }
            );

            ScheduledOffThreadTasks.Remove(Find.TickManager.TicksGame);
        }

        if (
            !ScheduledOnThreadTasks.NullOrEmpty()
            && ScheduledOnThreadTasks.TryGetValue(
                Find.TickManager.TicksGame,
                out List<IOnThreadTask> tasks
            )
        )
        {
            foreach (IOnThreadTask onThreadTask in tasks)
            {
                try
                {
                    onThreadTask.OnThreadTask(this);
                }
                catch (Exception e)
                {
                    Log.Error($"Exception occured in OnThreadTask[{onThreadTask.ToString()}]: {e}");
                }
            }
            ScheduledOnThreadTasks.Remove(Find.TickManager.TicksGame);
        }
    }

    public static void RegisterOnThreadTask(IOnThreadTask onThreadTask, int tick)
    {
        if (Manager.ScheduledOnThreadTasks == null)
            Manager.ScheduledOnThreadTasks = new Dictionary<int, List<IOnThreadTask>>();
        if (!Manager.ScheduledOnThreadTasks.TryGetValue(tick, out List<IOnThreadTask> tasks))
        {
            tasks = [];
            Manager.ScheduledOnThreadTasks.Add(tick, tasks);
        }

        tasks.Add(onThreadTask);
    }

    public static void RegisterOffThreadTask(IOffThreadTask offThreadTask, int tick)
    {
        if (Manager.ScheduledOffThreadTasks == null)
            Manager.ScheduledOffThreadTasks = new Dictionary<int, List<IOffThreadTask>>();
        if (!Manager.ScheduledOffThreadTasks.TryGetValue(tick, out List<IOffThreadTask> tasks))
        {
            tasks = [];
            Manager.ScheduledOffThreadTasks.Add(tick, tasks);
        }

        tasks.Add(offThreadTask);
    }

    public static void UnregisterAllOnThreadTask(IOnThreadTask onThreadTask)
    {
        Manager
            .ScheduledOnThreadTasks.SelectMany(kv => kv.Value)
            .ToList()
            .RemoveAll(t => t == onThreadTask);
    }

    public static void UnregisterAllOffThreadTask(IOffThreadTask offThreadTask)
    {
        Manager
            .ScheduledOffThreadTasks.SelectMany(kv => kv.Value)
            .ToList()
            .RemoveAll(t => t == offThreadTask);
    }

    public static void RegisterRealTimeSpeedChangeTask(SpeedChangeTask task)
    {
        if (Manager?.RealTimeSpeedChangeTasks == null)
        {
            if (Manager != null)
                Manager.RealTimeSpeedChangeTasks = new List<SpeedChangeTask>();
            else
                return;
        }

        Manager.RealTimeSpeedChangeTasks.Add(task);
    }

    public static void UnregisterRealTimeSpeedChangeTask(SpeedChangeTask task)
    {
        Manager?.RealTimeSpeedChangeTasks?.Remove(task);
    }

    public override void FinalizeInit()
    {
        // ReSharper disable once RedundantCheckBeforeAssignment
        if (Manager != this)
            Manager = this;
    }
}
