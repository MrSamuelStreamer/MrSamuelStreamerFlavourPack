using System;
using HarmonyLib;
using MSSFP.Genes;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Add random genes on birth
/// </summary>
[HarmonyPatch(typeof(QuestManager))]
public static class QuestManager_Patch
{
    [HarmonyPatch(nameof(QuestManager.Notify_PawnBorn))]
    [HarmonyPostfix]
    public static void Notify_PawnBorn_Patch(Thing baby)
    {
        // couldn't get this patch to work in PregnancyUtility.ApplyBirthOutcome, so applying here instead
        if (baby is not Pawn pawn || pawn.genes == null || pawn.Map == null)
            return;

        foreach (GeneMutatorDef birthGeneDef in DefDatabase<GeneMutatorDef>.AllDefs)
        {
            if (
                ConditionActive(Find.World, pawn.Map, birthGeneDef.conditionActive)
                && (
                    !birthGeneDef.chanceToApply.HasValue
                    || Rand.Chance(birthGeneDef.chanceToApply.Value)
                )
            )
            {
                int tries = 10;
                GeneClassification gene = birthGeneDef.RandomGene;
                while (gene.gene == null && tries > 0)
                {
                    tries--;
                    gene = birthGeneDef.RandomGene;
                }

                if (gene.gene == null)
                {
                    ModLog.Warn($"Couldn't find a gene to give {baby}");
                    return;
                }
                if (
                    !pawn.genes.Xenogenes.Any(g => g.def.ConflictsWith(gene.gene))
                    && !pawn.genes.Endogenes.Any(g => g.def.ConflictsWith(gene.gene))
                )
                {
                    if (!gene.requires.NullOrEmpty())
                    {
                        foreach (GeneDef geneRequire in gene.requires)
                        {
                            pawn.genes.AddGene(geneRequire, true);
                        }
                    }

                    pawn.genes.AddGene(gene.gene, true);
                    LookTargets lookTargets = new LookTargets();
                    lookTargets.targets.Add(pawn);
                    Messages.Message(
                        "MSS_GainedGeneFromConditionNew".Translate(
                            pawn.Named("PAWN"),
                            birthGeneDef.ReasonString,
                            gene.gene.LabelCap
                        ),
                        lookTargets,
                        MessageTypeDefOf.NeutralEvent,
                        true
                    );
                }
                else
                {
                    ModLog.Log($"Couldn't add {gene.gene.label} to {pawn.Name} due to conflicts");
                }
            }
        }
    }

    public static bool ConditionActive(World world, Map map, GameConditionDef conditionDef) =>
        world.GameConditionManager.ConditionIsActive(conditionDef)
        || map.GameConditionManager.ConditionIsActive(conditionDef);
}
