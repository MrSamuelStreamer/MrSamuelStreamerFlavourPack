using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MSSFP.Comps;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(Hediff_Pregnant))]
public static class Hediff_Pregnant_Patch
{
    private static List<GeneDef> tmpGenes = new();
    private static List<GeneDef> tmpGenesShuffled = new();
    private static Dictionary<GeneDef, float> tmpGeneChances = new();

    public static float InbredChanceFromParents(List<Pawn> parents, out PawnRelationDef relation)
    {
        relation = null;
        if (parents.Count < 2)
            return 0.0f;
        float inbredChanceOnChild = 0.0f;

        foreach (Pawn parent1 in parents)
        {
            foreach (Pawn parent2 in parents.Except([parent1]))
            {
                foreach (PawnRelationDef relation1 in parent1.GetRelations(parent2))
                {
                    if (relation1.inbredChanceOnChild > (double)inbredChanceOnChild)
                    {
                        inbredChanceOnChild = relation1.inbredChanceOnChild;
                        relation = relation1;
                    }
                    inbredChanceOnChild = Mathf.Max(inbredChanceOnChild, relation1.inbredChanceOnChild);
                }
            }
        }
        return inbredChanceOnChild;
    }

    public static List<GeneDef> GetInheritedGenes(List<Pawn> parents, out bool success)
    {
        tmpGenes.Clear();
        tmpGenesShuffled.Clear();
        foreach (Pawn parent in parents)
        {
            if (parent?.genes != null)
            {
                foreach (Gene endogene in parent.genes.Endogenes)
                {
                    if (endogene.def.endogeneCategory != EndogeneCategory.Melanin && endogene.def.biostatArc <= 0)
                    {
                        if (!tmpGenesShuffled.Contains(endogene.def))
                            tmpGenesShuffled.Add(endogene.def);
                        if (tmpGeneChances.ContainsKey(endogene.def))
                            tmpGeneChances[endogene.def] = 1f;
                        else
                            tmpGeneChances.Add(endogene.def, 0.5f);
                    }
                }
            }
        }

        int generationTries = 0;
        do
        {
            tmpGenes.Clear();
            tmpGenesShuffled.Shuffle();
            foreach (GeneDef key in tmpGenesShuffled)
            {
                if (!tmpGenes.Contains(key) && Rand.Chance(tmpGeneChances[key]))
                    tmpGenes.Add(key);
            }
            tmpGenes.RemoveAll(x => x.prerequisite != null && !tmpGenes.Contains(x.prerequisite));
            int val = tmpGenes.NonOverriddenGenes(false).Sum(x => x.biostatMet);
            if (!(val >= GeneTuning.BiostatRange.min && val <= GeneTuning.BiostatRange.max))
                ++generationTries;
            else
                break;
        } while (generationTries < 50);
        success = generationTries < 50;

        if (PawnSkinColors.SkinColorsFromParents(parents.RandomElement(), parents.RandomElement()).TryRandomElement(out GeneDef result))
            tmpGenes.Add(result);

        if (!tmpGenes.Any(x => x.endogeneCategory == EndogeneCategory.HairColor))
        {
            List<GeneDef> endogeneByCategory = [];
            foreach (Pawn parent in parents)
            {
                endogeneByCategory.Add(parent?.genes?.GetFirstEndogeneByCategory(EndogeneCategory.HairColor));
            }

            if (endogeneByCategory.Count > 0)
            {
                tmpGenes.Add(endogeneByCategory.RandomElement());
            }
            else
            {
                GeneDef result2;
                if (DefDatabase<GeneDef>.AllDefs.Where(x => x.endogeneCategory == EndogeneCategory.HairColor).TryRandomElementByWeight(x => x.selectionWeight, out result2))
                    tmpGenes.Add(result2);
            }
        }

        if (!tmpGenes.Contains(GeneDefOf.Inbred) && Rand.Value < (double)InbredChanceFromParents(parents, out PawnRelationDef _))
            tmpGenes.Add(GeneDefOf.Inbred);
        tmpGeneChances.Clear();
        tmpGenesShuffled.Clear();
        return tmpGenes;
    }

    [HarmonyPatch(nameof(Hediff_Pregnant.PostAdd))]
    [HarmonyPostfix]
    public static void PostAdd_Patch(Hediff_Pregnant __instance)
    {
        Building_Bed bed = CompUpgradableBed.AllBeds.Select(comp => comp.Bed).FirstOrDefault(b => b.CurOccupants.Contains(__instance.pawn));

        CompUpgradableBed comp = bed?.TryGetComp<CompUpgradableBed>();
        if (comp == null)
            return;

        comp.AddPregnancy(__instance.pawn, bed.CurOccupants.ToList());

        List<GeneDef> genes = GetInheritedGenes(bed.CurOccupants.ToList(), out bool success);
        if (success)
        {
            GeneSet geneSet = new();
            foreach (GeneDef geneDef in genes)
            {
                geneSet.AddGene(geneDef);
            }
            __instance.geneSet = geneSet;
        }
    }

    [HarmonyPatch(nameof(Hediff_Pregnant.DoBirthSpawn))]
    [HarmonyPrefix]
    public static bool DoBirthSpawn_Patch(Hediff_Pregnant __instance, Pawn mother)
    {
        CompUpgradableBed comp = CompUpgradableBed.CompForPregnancy(__instance);
        if (comp == null)
            return true;

        if (mother.RaceProps.Humanlike && !ModsConfig.BiotechActive)
            return false;

        int litterSize = mother.RaceProps.litterSizeCurve != null ? Mathf.RoundToInt(Rand.ByCurve(mother.RaceProps.litterSizeCurve)) : 1;
        if (litterSize < 1)
            litterSize = 1;

        if (litterSize == 1)
        {
            litterSize = Rand.RangeInclusive(1, Math.Max(1, comp.ParentsForPregnancy(__instance).Count));
        }

        PawnGenerationRequest request = new(mother.kindDef, mother.Faction, allowDowned: true, developmentalStages: DevelopmentalStage.Newborn);
        Pawn pawn = null;
        for (int index = 0; index < litterSize; ++index)
        {
            pawn = PawnGenerator.GeneratePawn(request);
            if (PawnUtility.TrySpawnHatchedOrBornPawn(pawn, mother))
            {
                if (pawn.playerSettings != null && mother.playerSettings != null)
                    pawn.playerSettings.AreaRestrictionInPawnCurrentMap = mother.playerSettings.AreaRestrictionInPawnCurrentMap;
                if (pawn.RaceProps.IsFlesh)
                {
                    foreach (Pawn parent in comp.ParentsForPregnancy(__instance))
                    {
                        pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, parent);
                        foreach (Gene gene in parent.genes.GenesListForReading)
                        {
                            if (Rand.Chance(0.1f) && !pawn.genes.HasActiveGene(gene.def))
                            {
                                pawn.genes.AddGene(gene.def, false);
                            }
                        }
                    }
                }
                if (mother.Spawned)
                    mother.GetLord()?.AddPawn(pawn);
            }
            else
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);

            comp.Notify_PawnBorn(pawn);
            TaleRecorder.RecordTale(TaleDefOf.GaveBirth, mother, pawn);
        }
        if (!mother.Spawned)
            return false;
        FilthMaker.TryMakeFilth(mother.Position, mother.Map, ThingDefOf.Filth_AmnioticFluid, mother.LabelIndefinite(), 5);
        mother.caller?.DoCall();
        pawn.caller?.DoCall();

        // comp.RemovePregnancy(__instance);

        return false;
    }
}
