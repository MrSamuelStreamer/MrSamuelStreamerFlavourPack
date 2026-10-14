using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Defs;

/// <summary>
/// Maps a named haunt HediffDef to its trigger conditions and progression rates.
/// XML-driven — adding new haunts with progression requires no C# changes.
/// </summary>
public class HauntProgressionDef : Def
{
    /// <summary>The haunt hediff this progression config applies to.</summary>
    public HediffDef hauntDef;

    /// <summary>Conditions that cause the haunt to grow stronger when met.</summary>
    public List<HauntTriggerConfig> triggers = new();

    /// <summary>Passive severity gain per day when triggered recently.</summary>
    public float passiveSeverityPerDay = 0.05f;

    /// <summary>Severity decay per day after regressionThresholdDays without a trigger.</summary>
    public float regressionPerDay = 0.02f;

    /// <summary>Days without a trigger before regression begins.</summary>
    public int regressionThresholdDays = 5;

    /// <summary>How often triggers are checked, in ticks (~1 in-game hour = 2500 ticks).</summary>
    public int triggerCheckIntervalTicks = 2500;

    /// <summary>
    /// Gene imprinted on the host pawn once the haunt reaches Awakened severity (≥0.67).
    /// Optional — no gene fires if null.
    /// </summary>
    public GeneDef awakeningGeneDef;
}

/// <summary>
/// A single trigger condition for haunt progression.
/// </summary>
public class HauntTriggerConfig
{
    /// <summary>Which type of check to perform.</summary>
    public HauntTriggerType triggerType = HauntTriggerType.RecordDelta;

    /// <summary>
    /// For RecordDelta: the record to watch for increases since last check.
    /// Populated via XML def reference.
    /// </summary>
    public RecordDef recordDef;

    /// <summary>Severity added each time this trigger fires in a check interval.</summary>
    public float progressionAmount = 0.02f;
}

public enum HauntTriggerType
{
    /// <summary>Fires when a pawn record (e.g. KillsHumanlikes) has increased since last check.</summary>
    RecordDelta,
}
