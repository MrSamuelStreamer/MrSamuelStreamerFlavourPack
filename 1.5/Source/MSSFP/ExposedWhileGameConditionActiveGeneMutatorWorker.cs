using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP;

public class ExposedWhileGameConditionActiveGeneMutatorWorker: GeneMutatorWorker
{
    public override void MapComponentTick(Map map)
    {
        base.MapComponentTick(map);
        if(!Find.World.GameConditionManager.ConditionIsActive(def.conditionActive) && !map.GameConditionManager.ConditionIsActive(def.conditionActive)) return;

        if(Find.TickManager.TicksGame % 250 != 0) return;

        IReadOnlyList<Pawn> allPawnsSpawned = map.mapPawns.AllPawnsSpawned;

        foreach (Pawn pawn in allPawnsSpawned)
        {
            if (def.chanceToApply != null && !pawn.Position.Roofed(map) && pawn.def.race.IsFlesh && pawn.genes != null && Rand.Chance(def.chanceToApply.Value))
            {
                LookTargets lookTargets = new LookTargets();
                lookTargets.targets.Add(pawn);

                if (!def.pinataMode)
                {
                    GeneClassification gene = def.RandomGene;
                    if (!pawn.genes.Xenogenes.Any(g => g.def.ConflictsWith(gene.gene)) && !pawn.genes.Endogenes.Any(g => g.def.ConflictsWith(gene.gene)))
                    {
                        foreach (GeneDef geneRequire in gene.requires)
                        {
                            pawn.genes.AddGene(geneRequire, true);
                        }
                        pawn.genes.AddGene(gene.gene, true);
                        Messages.Message("MSS_GainedGeneFromCondition".Translate(pawn.Name, def.ReasonString, gene.gene.LabelCap), lookTargets, MessageTypeDefOf.NeutralEvent, true);
                    }
                }
                else
                {
                    if (Rand.Chance(def.chanceToApply.Value))
                    {
                        // random genes
                        bool addAsXeno = Rand.Chance(0.95f);
                        foreach (GeneDef geneDef in RandomGeneSet(pawn))
                        {
                            pawn.genes.AddGene(geneDef,addAsXeno);
                            Messages.Message("MSS_GainedGeneFromCondition".Translate(pawn.Name, "Nuclear Fallout Exposure", geneDef.LabelCap), lookTargets, MessageTypeDefOf.NeutralEvent, true);

                        }
                    }

                    if (Rand.Chance(def.chanceToApply.Value/2))
                    {
                        foreach (Gene gene in pawn.genes.Xenogenes.Take(def.pinataModeChance.RandomInRange))
                        {
                            pawn.genes.RemoveGene(gene);
                            Messages.Message("MSS_LostGeneFromCondition".Translate(pawn.Name, "Nuclear Fallout Exposure", gene.LabelCap), lookTargets, MessageTypeDefOf.NeutralEvent, true);
                        }
                    }
                }
            }
        }
    }

    public virtual List<GeneDef> RandomGeneSet(Pawn pawn)
    {
        int tries = 10;
        while (tries-- > 0)
        {
            List<GeneDef> pawnGenes = pawn.genes.Endogenes.Select(g => g.def).Concat(pawn.genes.Xenogenes.Select(g => g.def)).ToList();

            IEnumerable<GeneDef> validGenePool = DefDatabase<GeneDef>.AllDefs.Except(pawnGenes);

            List<GeneDef> selected = validGenePool.Take(def.pinataModeChance.RandomInRange).ToList();

            if(selected.Count <= 0) continue;

            List<GeneDef> prereqs = selected.Where(g => g.prerequisite != null).Select(g => g.prerequisite).ToList();

            while (prereqs.Count > 0)
            {
                selected = selected.Concat(prereqs).ToList();
                prereqs = selected.Where(g => g.prerequisite != null).Select(g => g.prerequisite).Except(prereqs).ToList();
            }

            List<GeneDef> conflicts = new List<GeneDef>();

            foreach (GeneDef gene in selected)
            {
                if (!conflicts.Contains(gene))
                {
                    foreach (GeneDef otherGene in pawnGenes.Concat(conflicts).Except(gene).Except(conflicts))
                    {
                        if (gene.ConflictsWith(otherGene))
                        {
                            conflicts.Add(otherGene);
                        }
                    }
                }
            }

            selected = selected.Except(conflicts).ToList();
            if(selected.Count <= 0) continue;

            return selected;
        }

        ModLog.Warn("Tried 10 times to generate a list of random genes and failed");

        return [];
    }
}
