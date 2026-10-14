using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Utils;

/// <summary>
/// Shared "guaranteed downside" helper used by healer + resurrector mech serums.
/// Picks one of: drop a passion / gain a curated negative trait / fallback mood memory.
/// </summary>
public static class MechSerumDownsides
{
    // Curated (TraitDef defName, degree) pairs. Validated against vanilla 1.6.
    private static readonly (string defName, int degree)[] CuratedNegativeTraits =
    {
        ("Pyromaniac", 0),
        ("Industriousness", -1), // lazy
        ("Industriousness", -2), // slothful
        ("Neurotic", -1),        // nervous
        ("Neurotic", -2),        // volatile
        ("Wimp", 0),
        ("Abrasive", 0),
        ("Greedy", 0),
        ("Jealous", 0),
        ("Bloodlust", 0),
    };

    /// <summary>
    /// Apply one guaranteed downside to pawn. 50/50 between passion-loss and trait-add paths.
    /// Falls through to the other path if the chosen one is invalid. Falls through to a mood
    /// memory thought if both fail. Sends a Letter when any downside lands.
    /// </summary>
    public static void ApplyGuaranteedDownside(Pawn pawn, string sourceLabel)
    {
        if (pawn?.story == null) return;

        bool tryPassionFirst = Rand.Bool;

        if (tryPassionFirst)
        {
            if (TryDropPassion(pawn, out string passionMsg))
            {
                SendDownsideLetter(pawn, sourceLabel, passionMsg);
                return;
            }
            if (TryAddCuratedNegativeTrait(pawn, out string traitMsg))
            {
                SendDownsideLetter(pawn, sourceLabel, traitMsg);
                return;
            }
        }
        else
        {
            if (TryAddCuratedNegativeTrait(pawn, out string traitMsg))
            {
                SendDownsideLetter(pawn, sourceLabel, traitMsg);
                return;
            }
            if (TryDropPassion(pawn, out string passionMsg))
            {
                SendDownsideLetter(pawn, sourceLabel, passionMsg);
                return;
            }
        }

        // Last resort — guaranteed mood memory so contract holds.
        ApplyFallbackMoodMemory(pawn);
        SendDownsideLetter(pawn, sourceLabel, "PAWN_NAME felt a wave of nausea from the serum.".Replace("PAWN_NAME", pawn.LabelShort));
    }

    public static bool TryDropPassion(Pawn pawn, out string message)
    {
        message = null;
        if (pawn.skills?.skills == null) return false;

        List<SkillRecord> passionate = pawn.skills.skills
            .Where(s => s.passion == Passion.Minor || s.passion == Passion.Major)
            .ToList();

        if (passionate.Count == 0) return false;

        SkillRecord pick = passionate.RandomElement(); // equal weight
        Passion before = pick.passion;
        pick.passion = before == Passion.Major ? Passion.Minor : Passion.None;
        message = $"{pawn.LabelShort} lost {before.ToString().ToLowerInvariant()} passion in {pick.def.LabelCap}.";
        return true;
    }

    public static bool TryAddCuratedNegativeTrait(Pawn pawn, out string message)
    {
        message = null;
        TraitSet traits = pawn.story?.traits;
        if (traits == null) return false;

        List<(TraitDef def, int degree)> candidates = new();
        foreach ((string defName, int degree) in CuratedNegativeTraits)
        {
            TraitDef tdef = DefDatabase<TraitDef>.GetNamedSilentFail(defName);
            if (tdef == null) continue;

            // Skip if pawn already has any degree of this trait def.
            if (traits.HasTrait(tdef)) continue;

            // Skip if conflicts with any existing trait.
            bool conflict = false;
            foreach (Trait existing in traits.allTraits)
            {
                if (tdef.ConflictsWith(existing) || existing.def.ConflictsWith(new Trait(tdef, degree, true)))
                {
                    conflict = true;
                    break;
                }
            }
            if (conflict) continue;

            // Validate degree exists in TraitDef.
            if (tdef.degreeDatas != null && !tdef.degreeDatas.Any(d => d.degree == degree)) continue;

            candidates.Add((tdef, degree));
        }

        if (candidates.Count == 0) return false;

        (TraitDef pickDef, int pickDegree) = candidates.RandomElement();
        Trait gained = new(pickDef, pickDegree, forced: false);
        traits.GainTrait(gained);
        message = $"{pawn.LabelShort} gained the trait: {gained.LabelCap}.";
        return true;
    }

    public static void ApplyFallbackMoodMemory(Pawn pawn)
    {
        if (pawn.needs?.mood?.thoughts?.memories == null) return;
        ThoughtDef tdef = DefDatabase<ThoughtDef>.GetNamedSilentFail("MSSFP_SerumDownside_Memory");
        if (tdef == null) return;
        pawn.needs.mood.thoughts.memories.TryGainMemory(tdef);
    }

    private static void SendDownsideLetter(Pawn pawn, string sourceLabel, string body)
    {
        string label = $"Mech serum side effect ({sourceLabel})";
        TaggedString text = $"{body}\n\nThe mechanites left a lasting mark.";
        Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.NegativeEvent, pawn);
    }
}
