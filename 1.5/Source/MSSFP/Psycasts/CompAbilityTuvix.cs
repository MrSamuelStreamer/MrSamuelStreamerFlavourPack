using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Psycasts;

public class CompAbilityTuvix: CompAbilityEffect
{
    public CompProperties_AbilityTuvix Props => (CompProperties_AbilityTuvix)props;

    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
        return base.CanApplyOn(target, dest) && target.Pawn is { genes: not null };
    }

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        if(target.Pawn?.genes == null) return;

        List<GeneDef> GenePool = [];

        GenePool.AddRange(target.Pawn.genes.GenesListForReading.Select(g=>g.def));
        GenePool.AddRange(parent.pawn.genes.GenesListForReading.Select(g=>g.def));

        foreach (GeneDef gene in GenePool.InRandomOrder().Take(Rand.RangeInclusive(Mathf.RoundToInt(GenePool.Count * 0.25f), Mathf.RoundToInt(GenePool.Count * 0.75f))))
        {
            parent.pawn.genes.ClearXenogenes();
            for (int index = parent.pawn.genes.Endogenes.Count - 1; index >= 0; --index)
                parent.pawn.genes.RemoveGene(parent.pawn.genes.Endogenes[index]);

            parent.pawn.genes.AddGene(gene, false);
        }

        foreach (Trait trait in target.Pawn.story.traits.allTraits)
        {
            parent.pawn.story.traits.GainTrait(new Trait(trait.def, forced: true));
            if(Rand.Bool) break;
        }

        foreach (SkillRecord skill in target.Pawn.skills.skills)
        {
            SkillRecord origSkill = parent.pawn.skills.GetSkill(skill.def);
            origSkill.levelInt = (skill.Level + origSkill.levelInt) / 2 + 1;
            if (skill.passion == Passion.Minor && Rand.Bool)
            {
                if(origSkill.passion == Passion.None) origSkill.passion = Passion.Minor;
                if(origSkill.passion == Passion.Minor) origSkill.passion = Passion.Major;
            }

            if (skill.passion == Passion.Major && Rand.Bool)
            {
                origSkill.passion = Passion.Major;
            }
        }

        foreach (Hediff hediff in target.Pawn.health.hediffSet.hediffs)
        {
            if(hediff.def.countsAsAddedPartOrImplant) continue;
            target.Pawn.health.RemoveHediff(hediff);
            parent.pawn.health.AddHediff(hediff);
        }

        DamageInfo dInfo = new(DamageDefOf.Psychic, 0.1f, 0,-1f, parent.pawn);
        parent.pawn.health.AddHediff(HediffDef.Named("PsychicComa"), null, dInfo);

        DamageInfo tdInfo = new DamageInfo(DamageDefOf.Crush, 10000, 1,-1f, parent.pawn);
        target.Pawn.Kill(tdInfo);
    }

}
