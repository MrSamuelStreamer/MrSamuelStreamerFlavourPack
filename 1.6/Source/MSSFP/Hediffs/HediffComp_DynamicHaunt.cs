using MSSFP.Haunts;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Hediffs;

/// <summary>
/// Companion comp on MSS_FP_PawnDisplayer for grave-proximity dynamic haunts.
/// Stores the HauntProfile derived from the dead colonist's skills and owns
/// progression (passive growth, trigger detection, regression, awakening gene).
///
/// Stat offsets from the profile are applied externally by DynamicHauntStat_Patch.
/// </summary>
public class HediffComp_DynamicHaunt : HediffComp
{
    public HauntProfile Profile;

    private int lastCheckTick = -1;
    private int lastTriggerTick = -1;
    private float lastRecordValue = -1f;
    private bool awakeningGeneFired = false;

    // Uniform progression rates for all dynamic haunts.
    // Deliberately gentler than named haunts — dynamic haunts are emergent, not authored.
    private const float PassiveSeverityPerDay = 0.04f;
    private const float RegressionPerDay = 0.015f;
    private const int RegressionThresholdDays = 5;
    private const int CheckIntervalTicks = 2500;
    private const float TriggerProgressionAmount = 0.02f;

    public void SetProfile(HauntProfile profile)
    {
        Profile = profile;
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        if (Profile == null)
            return;

        int now = Find.TickManager.TicksGame;

        if (lastCheckTick < 0)
        {
            lastCheckTick = now;
            SnapshotRecord();
            return;
        }

        if (now < lastCheckTick + CheckIntervalTicks)
            return;

        int ticksElapsed = now - lastCheckTick;
        lastCheckTick = now;
        float daysElapsed = (float)ticksElapsed / GenDate.TicksPerDay;

        float mult = MSSFPMod.settings.HauntProgressionSpeedMultiplier;
        Pawn p = parent.pawn;
        if (p.genes?.HasActiveGene(MSSFPDefOf.MSS_FP_Gene_HauntSensitive) == true)
            mult *= 2f;
        if (p.genes?.HasActiveGene(MSSFPDefOf.MSS_FP_Gene_HauntResistant) == true)
            mult *= 0.5f;

        bool triggered = CheckTrigger();
        if (triggered)
            lastTriggerTick = now;

        bool inRegression =
            lastTriggerTick >= 0
            && (now - lastTriggerTick) > RegressionThresholdDays * GenDate.TicksPerDay;

        if (inRegression)
        {
            severityAdjustment -=
                RegressionPerDay * daysElapsed * MSSFPMod.settings.HauntRegressionSpeedMultiplier;
        }
        else
        {
            severityAdjustment += PassiveSeverityPerDay * daysElapsed * mult;
        }

        float newSeverity = Mathf.Clamp(parent.Severity + severityAdjustment, 0.01f, 1f);
        severityAdjustment = newSeverity - parent.Severity;

        if (!awakeningGeneFired && newSeverity >= 0.67f)
            TryFireAwakeningGene();
    }

    private bool CheckTrigger()
    {
        if (Profile.triggerRecordDef == null || parent.pawn.records == null)
            return false;

        float current = parent.pawn.records.GetValue(Profile.triggerRecordDef);
        bool fired = false;

        if (current > lastRecordValue && lastRecordValue >= 0f)
        {
            parent.Severity = Mathf.Clamp(
                parent.Severity
                    + TriggerProgressionAmount
                    * MSSFPMod.settings.HauntProgressionSpeedMultiplier,
                0.01f,
                1f
            );
            fired = true;
        }

        SnapshotRecord();
        return fired;
    }

    private void SnapshotRecord()
    {
        if (Profile?.triggerRecordDef != null && parent.pawn.records != null)
            lastRecordValue = parent.pawn.records.GetValue(Profile.triggerRecordDef);
    }

    private void TryFireAwakeningGene()
    {
        awakeningGeneFired = true;

        GeneDef geneDef = Profile?.awakeningGeneDef;
        if (geneDef == null)
            return;

        Pawn pawn = parent.pawn;
        if (pawn.genes == null || pawn.genes.HasActiveGene(geneDef))
            return;

        pawn.genes.AddGene(geneDef, xenogene: false);

        string skillLabel = Profile.primarySkill?.label ?? "an unknown skill";
        Messages.Message(
            "MSS_FP_AwakeningGene_Msg".Translate(pawn.LabelShort, skillLabel),
            pawn,
            MessageTypeDefOf.PositiveEvent,
            historical: false
        );
    }

    /// <summary>Returns a one-line status string for the dev dashboard.</summary>
    public string DebugStatus()
    {
        if (Profile == null)
            return "no profile";

        string skillName = Profile.primarySkill?.defName ?? "none";
        string archName = Profile.archetype?.defName ?? "none";
        string regressing =
            lastTriggerTick >= 0
            && (Find.TickManager.TicksGame - lastTriggerTick)
                > RegressionThresholdDays * GenDate.TicksPerDay
                ? " [REGRESSING]"
                : string.Empty;

        return $"skill={skillName} arch={archName} scale={Profile.scaleFactor:F2}{regressing}";
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Deep.Look(ref Profile, "profile");
        Scribe_Values.Look(ref lastCheckTick, "lastCheckTick", -1);
        Scribe_Values.Look(ref lastTriggerTick, "lastTriggerTick", -1);
        Scribe_Values.Look(ref lastRecordValue, "lastRecordValue", -1f);
        Scribe_Values.Look(ref awakeningGeneFired, "awakeningGeneFired", false);
    }
}

public class HediffCompProperties_DynamicHaunt : HediffCompProperties
{
    public HediffCompProperties_DynamicHaunt()
    {
        compClass = typeof(HediffComp_DynamicHaunt);
    }
}
