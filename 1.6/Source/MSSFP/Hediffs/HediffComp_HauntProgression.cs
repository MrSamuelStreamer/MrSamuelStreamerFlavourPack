using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSSFP.Defs;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Hediffs;

/// <summary>
/// Polling-based haunt progression. Every ~2500 ticks, inspects the pawn's records
/// for qualifying trigger events since the last check, and adjusts severity accordingly.
/// Zero new Harmony patches — reads existing record state rather than intercepting actions.
/// </summary>
public class HediffComp_HauntProgression : HediffComp
{
    private int lastCheckTick = -1;
    private int lastTriggerTick = -1;
    private string lastTriggerLabel = null;

    /// <summary>Snapshots of record values from the previous check, used to compute deltas.</summary>
    private Dictionary<RecordDef, float> recordSnapshots = new();

    private HauntProgressionDef _progressionDef;

    private HauntProgressionDef ProgressionDef
    {
        get
        {
            if (_progressionDef == null)
            {
                _progressionDef = DefDatabase<HauntProgressionDef>.AllDefsListForReading
                    .FirstOrDefault(d => d.hauntDef == parent.def);
            }

            return _progressionDef;
        }
    }

    private float ProgressionMultiplier => MSSFPMod.settings.HauntProgressionSpeedMultiplier;
    private float RegressionMultiplier => MSSFPMod.settings.HauntRegressionSpeedMultiplier;

    public string LastTriggerLabel => lastTriggerLabel ?? "none";

    public int TicksSinceLastTrigger =>
        lastTriggerTick < 0 ? int.MaxValue : Find.TickManager.TicksGame - lastTriggerTick;

    public bool IsRegressing =>
        lastTriggerTick >= 0
        && TicksSinceLastTrigger
            > ProgressionDef?.regressionThresholdDays * GenDate.TicksPerDay;

    public override void CompPostTick(ref float severityAdjustment)
    {
        if (ProgressionDef == null)
            return;

        int now = Find.TickManager.TicksGame;

        // Initialise snapshot on first tick
        if (lastCheckTick < 0)
        {
            lastCheckTick = now;
            TakeRecordSnapshot();
            return;
        }

        if (now < lastCheckTick + ProgressionDef.triggerCheckIntervalTicks)
            return;

        int ticksElapsed = now - lastCheckTick;
        lastCheckTick = now;

        float daysElapsed = (float)ticksElapsed / GenDate.TicksPerDay;
        bool triggered = CheckTriggers();

        if (triggered)
        {
            lastTriggerTick = now;
        }

        // Passive growth or regression, scaled by settings multipliers
        bool inRegression =
            lastTriggerTick >= 0
            && (now - lastTriggerTick)
                > ProgressionDef.regressionThresholdDays * GenDate.TicksPerDay;

        if (inRegression)
        {
            severityAdjustment -=
                ProgressionDef.regressionPerDay * daysElapsed * RegressionMultiplier;
        }
        else
        {
            severityAdjustment +=
                ProgressionDef.passiveSeverityPerDay * daysElapsed * ProgressionMultiplier;
        }

        // Clamp to valid range — never decay below 0.01 while hediff exists
        float newSeverity = Mathf.Clamp(parent.Severity + severityAdjustment, 0.01f, 1f);
        severityAdjustment = newSeverity - parent.Severity;

        TakeRecordSnapshot();
    }

    private bool CheckTriggers()
    {
        if (ProgressionDef.triggers.NullOrEmpty())
            return false;

        bool anyFired = false;
        foreach (HauntTriggerConfig trigger in ProgressionDef.triggers)
        {
            if (EvaluateTrigger(trigger))
                anyFired = true;
        }

        return anyFired;
    }

    private bool EvaluateTrigger(HauntTriggerConfig trigger)
    {
        switch (trigger.triggerType)
        {
            case HauntTriggerType.RecordDelta:
                return EvaluateRecordDelta(trigger);
            default:
                return false;
        }
    }

    private bool EvaluateRecordDelta(HauntTriggerConfig trigger)
    {
        if (trigger.recordDef == null || parent.pawn.records == null)
            return false;

        float current = parent.pawn.records.GetValue(trigger.recordDef);
        recordSnapshots.TryGetValue(trigger.recordDef, out float previous);

        if (current > previous)
        {
            float amount = trigger.progressionAmount * ProgressionMultiplier;
            parent.Severity = Mathf.Clamp(parent.Severity + amount, 0.01f, 1f);
            lastTriggerLabel = trigger.recordDef.defName;
            return true;
        }

        return false;
    }

    private void TakeRecordSnapshot()
    {
        if (ProgressionDef == null || parent.pawn.records == null)
            return;

        foreach (HauntTriggerConfig trigger in ProgressionDef.triggers)
        {
            if (trigger.triggerType == HauntTriggerType.RecordDelta && trigger.recordDef != null)
            {
                recordSnapshots[trigger.recordDef] = parent.pawn.records.GetValue(
                    trigger.recordDef
                );
            }
        }
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref lastCheckTick, "lastCheckTick", -1);
        Scribe_Values.Look(ref lastTriggerTick, "lastTriggerTick", -1);
        Scribe_Values.Look(ref lastTriggerLabel, "lastTriggerLabel");
        Scribe_Collections.Look(ref recordSnapshots, "recordSnapshots", LookMode.Def, LookMode.Value);
    }

    public override IEnumerable<Gizmo> CompGetGizmos()
    {
        if (!DebugSettings.ShowDevGizmos)
            yield break;

        yield return new Command_Action
        {
            defaultLabel = "DEV: Haunt +0.1 severity",
            icon = HediffComp_Haunt.icon,
            action = () =>
            {
                parent.Severity = Mathf.Clamp(parent.Severity + 0.1f, 0f, 1f);
                lastTriggerTick = Find.TickManager.TicksGame;
                lastTriggerLabel = "dev override";
            },
        };

        yield return new Command_Action
        {
            defaultLabel = "DEV: Haunt -0.1 severity",
            icon = HediffComp_Haunt.icon,
            action = () =>
            {
                parent.Severity = Mathf.Clamp(parent.Severity - 0.1f, 0.01f, 1f);
            },
        };
    }

    /// <summary>Returns a status string for the dev dashboard.</summary>
    public string DebugStatus()
    {
        if (ProgressionDef == null)
            return "no progression def";

        StringBuilder sb = new();
        sb.Append($"sev={parent.Severity:F2}");
        sb.Append(IsRegressing ? " [REGRESSING]" : " [growing]");
        sb.Append($" last={LastTriggerLabel}");
        if (lastTriggerTick > 0)
        {
            float daysSince = TicksSinceLastTrigger / (float)GenDate.TicksPerDay;
            sb.Append($" {daysSince:F1}d ago");
        }

        return sb.ToString();
    }
}

/// <summary>
/// Properties class for HediffComp_HauntProgression.
/// The haunt def lookup is done at runtime via HauntProgressionDef,
/// so this properties class has no extra fields.
/// </summary>
public class HediffCompProperties_HauntProgression : HediffCompProperties
{
    public HediffCompProperties_HauntProgression()
    {
        compClass = typeof(HediffComp_HauntProgression);
    }
}
