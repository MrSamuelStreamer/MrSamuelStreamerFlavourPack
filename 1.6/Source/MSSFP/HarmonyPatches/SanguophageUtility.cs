using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MSSFP.Defs;
using MSSFP.Genes;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(SanguophageUtility))]
public static class SanguophageUtility_Patch
{
    public static Lazy<FieldInfo> XPLossPercentFromDeathrestRange = new(() => AccessTools.Field(typeof(SanguophageUtility), "XPLossPercentFromDeathrestRange"));

    [HarmonyPatch(nameof(SanguophageUtility.ShouldBeDeathrestingOrInComaInsteadOfDead))]
    [HarmonyPostfix]
    public static void ShouldBeDeathrestingOrInComaInsteadOfDeadPostfix(Pawn pawn, ref bool __result)
    {
        if (__result) return;
        if (pawn.genes?.GetFirstGeneOfType<Gene_VoidsEmbrace>() == null) return;

        BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
        if (brain != null && !pawn.health.hediffSet.PartIsMissing(brain) && pawn.health.hediffSet.GetPartHealth(brain) > 0f)
            __result = true;
    }

    [HarmonyPatch(nameof(SanguophageUtility.DoXPLossFromDamage))]
    [HarmonyPrefix]
    public static bool DoXPLossFromDamagePrefix(Pawn pawn, ref TaggedString letterText)
    {
        Gene_VoidsEmbrace voidsEmbraceGene = pawn.genes?.GetFirstGeneOfType<Gene_VoidsEmbrace>();
        if (voidsEmbraceGene == null) return true;

        // Pawns carrying the gene can still lack a skills/story tracker (sapient
        // animals, mutants/ghouls, other gene-copying mods) — let vanilla handle
        // those rather than NRE mid-deathrest processing.
        if (pawn.skills?.skills == null || pawn.story?.traits == null) return true;

        // Partial skill loss: 0–5 points per skill
        foreach (SkillRecord skill in pawn.skills.skills)
        {
            int loss = Rand.RangeInclusive(0, 5);
            if (loss > 0)
            {
                skill.Level = Mathf.Max(0, skill.Level - loss);
                skill.xpSinceLastLevel = 0f;
                skill.xpSinceMidnight = 0f;
            }
        }

        letterText += "\n\n" + "MSS_VoidsEmbraceSkillsReset".Translate(pawn.Named("PAWN"));
        letterText = letterText.Replace("deathless", "Void's Embrace");

        // Trait cycling: remove all previously void-granted traits, then add a fresh batch
        foreach (TraitDef old in voidsEmbraceGene.lastVoidTraits)
        {
            Trait existing = pawn.story?.traits?.GetTrait(old);
            if (existing != null)
                pawn.story.traits.RemoveTrait(existing);
        }
        voidsEmbraceGene.lastVoidTraits.Clear();

        VoidsEmbraceConfigDef config = MSSFPDefOf.MSSFP_VoidsEmbraceConfig;

        List<string> curseLabels = RollCurses(pawn, voidsEmbraceGene, config);
        if (curseLabels.Count > 0)
        {
            letterText += "\n" + "MSS_VoidsEmbraceTraitGained"
                .Translate(pawn.Named("PAWN"), curseLabels.ToCommaList().Named("TRAITS"));
        }

        string boonLabel = TryRollBoon(pawn, voidsEmbraceGene, config);
        if (boonLabel != null)
        {
            letterText += "\n" + "MSS_VoidsEmbraceBoonGained"
                .Translate(pawn.Named("PAWN"), boonLabel.Named("TRAIT"));
        }

        voidsEmbraceGene.lastSkillReductionTick = Find.TickManager.TicksGame;
        return false;
    }

    private sealed class VoidCandidate
    {
        public TraitDef Trait;
        public TraitDegreeData Degree;
        public float Severity;
    }

    // Spends the config's curse budget on random negative traits. Heavy traits (work
    // disabling, mental-state causing) cost more, so the void hands out several mild
    // curses or one harsh one, not a pile of harsh ones.
    private static List<string> RollCurses(Pawn pawn, Gene_VoidsEmbrace gene, VoidsEmbraceConfigDef config)
    {
        var labels = new List<string>();
        float remaining = config.curseBudget;
        while (labels.Count < config.maxCurses)
        {
            List<VoidCandidate> pool = VoidCandidates(pawn, config, boons: false);
            VoidCandidate pick = PickCurse(pool, remaining, mustPick: labels.Count < config.minCurses);
            if (pick == null) break;

            labels.Add(GrantTrait(pawn, gene, pick));
            remaining -= pick.Severity;
        }

        return labels;
    }

    private static VoidCandidate PickCurse(List<VoidCandidate> pool, float remaining, bool mustPick)
    {
        if (pool.Where(c => c.Severity <= remaining).TryRandomElement(out VoidCandidate affordable))
            return affordable;
        if (!mustPick || pool.Count == 0) return null;

        // Below minCurses: take the cheapest curse even if it overruns the budget
        float cheapest = pool.Min(c => c.Severity);
        return pool.Where(c => Mathf.Approximately(c.Severity, cheapest)).RandomElement();
    }

    private static string TryRollBoon(Pawn pawn, Gene_VoidsEmbrace gene, VoidsEmbraceConfigDef config)
    {
        if (!Rand.Chance(config.boonChance)) return null;

        return VoidCandidates(pawn, config, boons: true).TryRandomElement(out VoidCandidate pick)
            ? GrantTrait(pawn, gene, pick)
            : null;
    }

    private static string GrantTrait(Pawn pawn, Gene_VoidsEmbrace gene, VoidCandidate candidate)
    {
        pawn.story.traits.GainTrait(new Trait(candidate.Trait, candidate.Degree.degree));
        gene.lastVoidTraits.Add(candidate.Trait);
        return candidate.Degree.GetLabelFor(pawn);
    }

    // Every (trait, degree) the void may give this pawn right now: positive ones for the
    // boon pool, negative ones for the curse pool. Skips vetoed, held, conflicting and
    // commonality-0 traits (status traits like VFE Pirates' shellcasket).
    private static List<VoidCandidate> VoidCandidates(Pawn pawn, VoidsEmbraceConfigDef config, bool boons)
    {
        var result = new List<VoidCandidate>();
        foreach (TraitDef trait in DefDatabase<TraitDef>.AllDefsListForReading)
        {
            if (!CanReceive(pawn, trait)) continue;

            foreach (TraitDegreeData degree in trait.degreeDatas)
            {
                bool wanted = boons ? IsBoon(trait, degree, config) : IsCurse(trait, degree, config);
                if (!wanted || config.IsVetoed(trait, degree.degree)) continue;

                result.Add(new VoidCandidate
                {
                    Trait = trait,
                    Degree = degree,
                    Severity = boons ? 0f : SeverityOf(trait, degree, config),
                });
            }
        }

        return result;
    }

    private static bool CanReceive(Pawn pawn, TraitDef trait) =>
        trait.GetGenderSpecificCommonality(pawn.gender) > 0f &&
        !pawn.story.traits.HasTrait(trait) &&
        !pawn.story.traits.allTraits.Any(existing => trait.ConflictsWith(existing.def));

    private static bool IsBoon(TraitDef trait, TraitDegreeData degree, VoidsEmbraceConfigDef config) =>
        degree.marketValueFactorOffset > 0f || config.IsBoonForced(trait, degree.degree);

    private static bool IsCurse(TraitDef trait, TraitDegreeData degree, VoidsEmbraceConfigDef config) =>
        !config.IsBoonForced(trait, degree.degree) &&
        (degree.marketValueFactorOffset < 0f || config.SeverityOverride(trait, degree.degree) is > 0f);

    // Budget cost of a curse: the config's explicit value, else market value scaled up,
    // plus extra for traits that disable work or trigger mental states.
    private static float SeverityOf(TraitDef trait, TraitDegreeData degree, VoidsEmbraceConfigDef config)
    {
        float? explicitSeverity = config.SeverityOverride(trait, degree.degree);
        if (explicitSeverity.HasValue) return explicitSeverity.Value;

        float severity = Mathf.Abs(degree.marketValueFactorOffset) * 10f;
        if (trait.disabledWorkTags != WorkTags.None || !trait.disabledWorkTypes.NullOrEmpty())
            severity += 3f;
        if (degree.randomMentalState != null || degree.forcedMentalState != null)
            severity += 2f;
        return Mathf.Max(0.5f, severity);
    }
}
