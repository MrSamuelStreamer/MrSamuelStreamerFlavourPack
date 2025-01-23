using System;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(QuestManager))]
public static class QuestManager_Patch
{
    [HarmonyPatch(nameof(QuestManager.Notify_PawnBorn))]
    [HarmonyPostfix]
    public static void Notify_PawnBorn_Patch(Thing baby)
    {
        // couldn't get this patch to work in PregnancyUtility.ApplyBirthOutcome, so applying here instead
        if(baby is not Pawn pawn || pawn.genes == null) return;

        foreach (GeneMutatorDef birthGeneDef in DefDatabase<GeneMutatorDef>.AllDefs)
        {
            if (ConditionActive(Find.World, pawn.Map, birthGeneDef.conditionActive) && (!birthGeneDef.chanceToApply.HasValue || Rand.Chance(birthGeneDef.chanceToApply.Value)))
            {
                GeneDef gene =  birthGeneDef.RandomGene;
                if (!pawn.genes.Xenogenes.Any(g => g.def.ConflictsWith(gene)) && !pawn.genes.Endogenes.Any(g => g.def.ConflictsWith(gene)))
                {
                    pawn.genes.AddGene(gene, true);
                    LookTargets lookTargets = new LookTargets();
                    lookTargets.targets.Add(pawn);
                    Messages.Message("MSS_GainedGeneFromCondition".Translate(pawn.Name, birthGeneDef.ReasonString, gene.LabelCap), lookTargets, MessageTypeDefOf.NeutralEvent, true);
                }
                else
                {
                    ModLog.Log($"Couldn't add {gene.label} to {pawn.Name} due to conflicts");
                }
            }
        }
    }

    public static bool ConditionActive(World world, Map map, GameConditionDef conditionDef) => world.GameConditionManager.ConditionIsActive(conditionDef) ||
                                                                                               map.GameConditionManager.ConditionIsActive(conditionDef);
}
