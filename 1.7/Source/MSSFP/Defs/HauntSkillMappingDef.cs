using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Defs;

/// <summary>
/// Maps a SkillDef to the archetype, progression trigger, awakening gene, and stat offsets
/// a dynamic colonist haunt produces when that skill was the colonist's highest in life.
///
/// statOffsets represent the full effect at skill level 20, Awakened severity (≥0.67).
/// HauntProfileBuilder scales these by (skillLevel - 8) / 12 and by severity stage at runtime.
/// </summary>
public class HauntSkillMappingDef : Def
{
    /// <summary>The skill this mapping applies to.</summary>
    public SkillDef skill;

    /// <summary>Haunt archetype assigned when this skill is the colonist's top skill.</summary>
    public HauntArchetypeDef archetype;

    /// <summary>Record delta watched for progression triggers.</summary>
    public RecordDef triggerRecord;

    /// <summary>
    /// Awakening gene granted at Awakened severity (≥0.67). Should be one of the five
    /// generic pool genes (MSS_FP_MemoryAwakening_*) from MMSFP_HauntGenes.xml.
    /// </summary>
    public GeneDef awakeningGene;

    /// <summary>
    /// Stat offsets at full effect (skill level 20, Awakened stage).
    /// Scaled down at runtime for lower skill levels and earlier severity stages.
    /// </summary>
    public List<StatModifier> statOffsets = new();
}
