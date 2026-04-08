using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
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

        // Count distribution: base 2–4, with a 15% cascading chance of extras (≥5 is rare)
        int traitCount = Rand.RangeInclusive(2, 4);
        while (Rand.Chance(0.15f)) traitCount++;

        var addedLabels = new List<string>();
        for (int i = 0; i < traitCount; i++)
        {
            TraitDef picked = TryPickVoidTrait(pawn);
            if (picked == null) break;

            TraitDegreeData degreeData = picked.degreeDatas
                .Where(dd => dd.marketValueFactorOffset < 0f)
                .RandomElement();
            pawn.story.traits.GainTrait(new Trait(picked, degreeData.degree));
            voidsEmbraceGene.lastVoidTraits.Add(picked);
            addedLabels.Add(degreeData.label);
        }

        if (addedLabels.Count > 0)
        {
            letterText += "\n" + "MSS_VoidsEmbraceTraitGained"
                .Translate(pawn.Named("PAWN"), addedLabels.ToCommaList().Named("TRAITS"));
        }

        voidsEmbraceGene.lastSkillReductionTick = Find.TickManager.TicksGame;
        return false;
    }

    private static TraitDef TryPickVoidTrait(Pawn pawn)
    {
        var candidates = DefDatabase<TraitDef>.AllDefsListForReading
            .Where(t =>
                t.degreeDatas.Any(dd => dd.marketValueFactorOffset < 0f) &&
                !pawn.story.traits.HasTrait(t) &&
                !pawn.story.traits.allTraits.Any(existing => t.ConflictsWith(existing.def)))
            .ToList();

        return candidates.TryRandomElement(out TraitDef result) ? result : null;
    }
}
