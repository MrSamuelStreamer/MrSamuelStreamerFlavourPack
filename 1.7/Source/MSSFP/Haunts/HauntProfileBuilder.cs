using System.Collections.Generic;
using System.Linq;
using MSSFP.Defs;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Haunts;

/// <summary>
/// Derives a HauntProfile from a dead colonist's skills at grave-proximity assignment time.
/// Called once by HauntedMapComponent; the result is serialized on HediffComp_DynamicHaunt
/// and never re-derived.
/// </summary>
public static class HauntProfileBuilder
{
    /// <summary>
    /// First names of characters with pre-authored named haunts. If the dead colonist's
    /// nick or short label matches one of these, dynamic generation is skipped entirely
    /// to avoid overlapping with the authored haunt system.
    /// </summary>
    private static readonly HashSet<string> NamedHauntSourceNames = new(
        System.StringComparer.OrdinalIgnoreCase
    )
    {
        "Grignr",
        "Frog",
        "Kekvit",
        "Oskar",
        "Phil",
        "Jade",
    };

    /// <summary>
    /// Attempts to build a HauntProfile from the given dead colonist.
    /// Returns null if the colonist matches a named haunt source (collision guard),
    /// or if the colonist reference is invalid.
    /// </summary>
    public static HauntProfile TryBuild(Pawn colonist, bool isGood = true)
    {
        if (colonist?.skills == null)
            return null;

        // Named haunt collision guard — only for good haunts (bad haunts come from kills, not graves)
        if (isGood)
        {
            string nick = (colonist.Name as NameTriple)?.Nick ?? colonist.LabelShort;
            if (NamedHauntSourceNames.Contains(nick))
                return null;
        }

        // Find top non-disabled skill
        SkillRecord topSkill = colonist.skills.skills
            .Where(s => !s.TotallyDisabled)
            .OrderByDescending(s => s.Level)
            .FirstOrDefault();

        bool isFocused = topSkill != null && topSkill.Level >= 8;

        HauntSkillMappingDef mapping = isFocused
            ? DefDatabase<HauntSkillMappingDef>.AllDefsListForReading
                .FirstOrDefault(m => m.skill == topSkill.def)
            : null;

        // scaleFactor: 0 at skill 8, 1.0 at skill 20.
        // Restless spirits (no focused skill) get 0.1 — weak, generic effects.
        float scaleFactor = isFocused
            ? Mathf.Clamp01((topSkill.Level - 8f) / 12f)
            : 0.1f;

        HauntProfile profile = new()
        {
            archetype = mapping?.archetype ?? MSSFPDefOf.MSS_FP_Archetype_Brooding,
            primarySkill = topSkill?.def,
            triggerRecordDef = mapping?.triggerRecord,
            awakeningGeneDef = isGood ? mapping?.awakeningGene : null,
            sourceColonistId = colonist.thingIDNumber,
            scaleFactor = scaleFactor,
            isGood = isGood,
        };

        // Copy stat offsets from mapping into parallel lists.
        // StatModifier is not IExposable, so we store as StatDef + float pairs.
        // Bad haunts negate all offsets — debuff instead of buff.
        float sign = isGood ? 1f : -1f;
        if (!mapping?.statOffsets.NullOrEmpty() ?? false)
        {
            foreach (StatModifier mod in mapping.statOffsets)
            {
                if (mod.stat == null)
                    continue;
                profile.statDefs.Add(mod.stat);
                profile.statValues.Add(mod.value * sign);
            }
        }

        return profile;
    }
}
