using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Defs;

/// <summary>
/// One per-trait override for Void's Embrace. Matches a single degree, or every degree
/// when <see cref="degree"/> is left at <see cref="AnyDegree"/>.
/// </summary>
public class VoidTraitEntry
{
    public const int AnyDegree = int.MinValue;

    public TraitDef trait;

    public int degree = AnyDegree;

    /// <summary>Explicit severity in budget points. Negative means "use the computed value".</summary>
    public float severity = -1f;

    /// <summary>Never given by the void, as a curse or a boon.</summary>
    public bool vetoed;

    /// <summary>Counts as a boon candidate even if its market value offset is not positive.</summary>
    public bool boon;

    public bool Matches(TraitDef def, int degreeValue) =>
        trait == def && (degree == AnyDegree || degree == degreeValue);
}

/// <summary>
/// Tuning for the traits Void's Embrace hands out on each rebirth: a severity budget for
/// the curses, trait overrides (veto, fixed severity, extra boons), and a boon chance.
/// A single instance, <c>MSSFP_VoidsEmbraceConfig</c>, is resolved through <see cref="MSSFPDefOf"/>.
/// </summary>
public class VoidsEmbraceConfigDef : Def
{
    /// <summary>Severity points the curses of one rebirth may spend in total.</summary>
    public float curseBudget = 4f;

    /// <summary>At least this many curses are given, even if the cheapest ones overrun the budget.</summary>
    public int minCurses = 1;

    public int maxCurses = 4;

    /// <summary>Chance of one positive trait on each rebirth.</summary>
    public float boonChance = 1f;

    public List<VoidTraitEntry> traits = new();

    public bool IsVetoed(TraitDef trait, int degree) =>
        traits.Any(e => e.vetoed && e.Matches(trait, degree));

    public bool IsBoonForced(TraitDef trait, int degree) =>
        traits.Any(e => e.boon && !e.vetoed && e.Matches(trait, degree));

    /// <summary>The explicit severity for this trait degree, or null when none is configured.</summary>
    public float? SeverityOverride(TraitDef trait, int degree)
    {
        VoidTraitEntry entry = traits
            .Where(e => e.severity >= 0f && e.Matches(trait, degree))
            .OrderBy(e => e.degree == VoidTraitEntry.AnyDegree)
            .FirstOrDefault();
        return entry?.severity;
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
            yield return error;

        if (curseBudget <= 0f)
            yield return "curseBudget must be positive";
        if (minCurses < 0 || maxCurses < minCurses)
            yield return "need 0 <= minCurses <= maxCurses";
        if (boonChance is < 0f or > 1f)
            yield return "boonChance must be between 0 and 1";
        if (traits.Any(e => e.trait == null))
            yield return "a traits entry has no trait";
    }
}
