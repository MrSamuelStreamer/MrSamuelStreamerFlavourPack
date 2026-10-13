using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Utils;

/// <summary>
/// Predicates for the Programmable Healer dialog — what's safe to remove/add for a given pawn.
/// Filters out content that the game expects to persist (genetic, backstory-forced, ideoligion-required).
/// </summary>
public static class ProgrammableHealerFilters
{
    /// <summary>Hediffs that would be removed by vanilla "fix worst" semantics — bad, visible, treatable.</summary>
    /// <remarks>
    /// Collapses the missing-part cascade (e.g. leg → foot → toes) by including only root missing parts
    /// (those whose ancestor part is not also missing on this pawn). Restoring the root auto-restores
    /// its children, so listing them individually is noise.
    /// </remarks>
    public static List<Hediff> RemovableAilments(Pawn pawn)
    {
        if (pawn?.health?.hediffSet == null) return new List<Hediff>();
        HashSet<BodyPartRecord> missingParts = pawn.health.hediffSet.hediffs
            .OfType<Hediff_MissingPart>()
            .Select(h => h.Part)
            .Where(p => p != null)
            .ToHashSet();

        return pawn.health.hediffSet.hediffs
            .Where(h => h.def.isBad && h.Visible && h.def.everCurableByItem)
            .Where(h => !(h is Hediff_MissingPart mp && mp.IsFreshNonSolidExtremity))
            .Where(h => !(h is Hediff_MissingPart && AncestorIsMissing(h.Part, missingParts)))
            .ToList();
    }

    /// <summary>True if any ancestor of <paramref name="part"/> is itself in the missing-part set.</summary>
    private static bool AncestorIsMissing(BodyPartRecord part, HashSet<BodyPartRecord> missingParts)
    {
        BodyPartRecord cur = part?.parent;
        while (cur != null)
        {
            if (missingParts.Contains(cur)) return true;
            cur = cur.parent;
        }
        return false;
    }

    /// <summary>Traits the player can safely strip without save corruption or ideoligion break.</summary>
    public static List<Trait> RemovableTraits(Pawn pawn)
    {
        if (pawn?.story?.traits?.allTraits == null) return new List<Trait>();

        // Collect forced-by-backstory trait defs.
        HashSet<TraitDef> forcedByBackstory = new();
        if (pawn.story.Childhood?.forcedTraits != null)
            foreach (BackstoryTrait bt in pawn.story.Childhood.forcedTraits)
                if (bt.def != null) forcedByBackstory.Add(bt.def);
        if (pawn.story.Adulthood?.forcedTraits != null)
            foreach (BackstoryTrait bt in pawn.story.Adulthood.forcedTraits)
                if (bt.def != null) forcedByBackstory.Add(bt.def);

        // NOTE: ideoligion role-required-trait filtering omitted — RimWorld 1.6 RoleRequirement subclass
        // for traits is not directly exposed. Removing an ideo-required trait is recoverable (debuff
        // only, no save corruption), so falling back to user-beware. Add later if needed.

        return pawn.story.traits.allTraits
            .Where(t => t.sourceGene == null)
            .Where(t => !t.ScenForced)
            .Where(t => !forcedByBackstory.Contains(t.def))
            .Where(t => !t.def.defName.StartsWith("MSS_FP_Haunt"))
            .ToList();
    }

    /// <summary>Skills whose passion can be bumped up one tier.</summary>
    public static List<SkillRecord> UpgradablePassions(Pawn pawn)
    {
        if (pawn?.skills?.skills == null) return new List<SkillRecord>();
        return pawn.skills.skills
            .Where(s => !s.TotallyDisabled)
            .Where(s => s.passion == Passion.None || s.passion == Passion.Minor)
            .ToList();
    }
}
