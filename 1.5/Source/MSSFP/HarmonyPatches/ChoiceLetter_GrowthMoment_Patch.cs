using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(ChoiceLetter_GrowthMoment))]
public static class ChoiceLetter_GrowthMoment_Patch
{
    [HarmonyPatch(nameof(ChoiceLetter_GrowthMoment.MakeChoices))]
    [HarmonyPostfix]
    public static void MakeChoices_Patch(ChoiceLetter_GrowthMoment __instance)
    {
        if(__instance.def != LetterDefOf.ChildToAdult) return;

        List<GeneDef> genePool = DefDatabase<GeneDef>.AllDefs.Where(g => g.HasModExtension<AgeUpGeneModDefExtension>()).ToList();
        if(genePool.Count <= 0) return;

        GeneDef selectedGene = genePool.RandomElementByWeight(g => g.GetModExtension<AgeUpGeneModDefExtension>().WeightingForRandomSelection);

        __instance.pawn.genes.AddGene(selectedGene, true);

        Messages.Message("MSS_Gen_RandomGene".Translate(__instance.pawn.NameFullColored, selectedGene.LabelCap), MessageTypeDefOf.NeutralEvent);
    }
}
