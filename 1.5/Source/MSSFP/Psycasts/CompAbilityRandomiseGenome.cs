using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Psycasts;

public class CompAbilityRandomiseGenome: CompAbilityEffect
{
    public static List<GeneDef> allValidGeneDefs = new List<GeneDef>();

    public static List<GeneDef> AllValidGeneDefs
    {
        get {
            if (allValidGeneDefs.NullOrEmpty())
            {
                allValidGeneDefs = DefDatabase<GeneDef>.AllDefs.Where(def => def.GetType().Name != "AndroidGeneDef ").ToList();
            }

            return allValidGeneDefs;
        }
    }


    public new CompProperties_AbilityRandomiseGenome Props => props as CompProperties_AbilityRandomiseGenome;

    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
        return base.CanApplyOn(target, dest) && target.Pawn is { genes: not null };
    }

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        if(target.Pawn?.genes == null) return;

        List<GeneDef> endogenes = AllValidGeneDefs.TakeRandom(Props.numberOfEndogenes.RandomInRange).ToList();
        List<GeneDef> xenogenes = AllValidGeneDefs.TakeRandom(Props.numberOfXenogenes.RandomInRange).ToList();

        target.Pawn.genes.ClearXenogenes();

        for (int index = target.Pawn.genes.Endogenes.Count - 1; index >= 0; --index)
            target.Pawn.genes.RemoveGene(target.Pawn.genes.Endogenes[index]);

        foreach (GeneDef endogene in GetDependencies(endogenes))
        {
            target.Pawn.genes.AddGene(endogene, false);
        }

        foreach (GeneDef xenogene in GetDependencies(xenogenes))
        {
            target.Pawn.genes.AddGene(xenogene, true);
        }

        DamageInfo dInfo = new DamageInfo(DamageDefOf.Psychic, 0, 0,-1f, parent.pawn);

        parent.pawn.health.AddHediff(HediffDef.Named("PsychicComa"), null, dInfo);
        target.Pawn.health.AddHediff(HediffDefOf.XenogerminationComa, null, dInfo);
    }

    public IEnumerable<GeneDef> GetDependencies(IEnumerable<GeneDef> genes)
    {
        foreach (GeneDef geneDef in genes)
        {
            if (geneDef.prerequisite != null)
            {
                foreach (GeneDef dependency in GetDependencies([geneDef.prerequisite]))
                {
                    yield return dependency;
                }
            }
            yield return geneDef;
        }
    }
}
