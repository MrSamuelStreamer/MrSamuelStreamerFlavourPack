using System.Collections.Generic;
using MSSFP.Defs;
using RimWorld;
using Verse;

namespace MSSFP.Haunts;

/// <summary>
/// Derived identity of a dynamic colonist haunt. Computed once at assignment by
/// HauntProfileBuilder from the dead colonist's skills, then serialized on
/// HediffComp_DynamicHaunt. Never re-derived on load — stored values are canonical.
///
/// Stat offsets are stored as parallel lists (StatDef + float) because StatModifier
/// is not IExposable and cannot be saved via Scribe_Collections.Look with LookMode.Deep.
/// </summary>
public class HauntProfile : IExposable
{
    public HauntArchetypeDef archetype;
    public SkillDef primarySkill;
    public RecordDef triggerRecordDef;

    /// <summary>StatDefs for stat offsets at full effect (parallel with statValues).</summary>
    public List<StatDef> statDefs = new();

    /// <summary>Offset values at skill level 20, Awakened stage (parallel with statDefs).</summary>
    public List<float> statValues = new();

    public GeneDef awakeningGeneDef;
    public int sourceColonistId;

    /// <summary>
    /// Scale factor computed at build time: (skillLevel - 8) / 12, clamped [0, 1].
    /// Persisted so algorithm changes don't affect existing haunts in active saves.
    /// </summary>
    public float scaleFactor;

    /// <summary>
    /// Returns the stat offset this profile contributes for the given stat at the given severity.
    /// Stage scaling: Whisper (≤0.33) = 33%, Presence (≤0.66) = 67%, Awakened = 100%.
    /// </summary>
    public float GetStatOffset(StatDef stat, float severity)
    {
        if (statDefs.NullOrEmpty())
            return 0f;

        int idx = statDefs.IndexOf(stat);
        if (idx < 0)
            return 0f;

        return statValues[idx] * scaleFactor * StageScale(severity);
    }

    private static float StageScale(float severity) =>
        severity <= 0.33f ? 0.33f
        : severity <= 0.66f ? 0.67f
        : 1.0f;

    public void ExposeData()
    {
        Scribe_Defs.Look(ref archetype, "archetype");
        Scribe_Defs.Look(ref primarySkill, "primarySkill");
        Scribe_Defs.Look(ref triggerRecordDef, "triggerRecordDef");
        Scribe_Collections.Look(ref statDefs, "statDefs", LookMode.Def);
        Scribe_Collections.Look(ref statValues, "statValues", LookMode.Value);
        Scribe_Defs.Look(ref awakeningGeneDef, "awakeningGeneDef");
        Scribe_Values.Look(ref sourceColonistId, "sourceColonistId", 0);
        Scribe_Values.Look(ref scaleFactor, "scaleFactor", 0f);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            statDefs ??= new List<StatDef>();
            statValues ??= new List<float>();
        }
    }
}
