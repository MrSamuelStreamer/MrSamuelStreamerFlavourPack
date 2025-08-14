using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MSSFP.ModExtensions;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Patch the choice letter for growth moments to add random genes when becoming an adult
/// </summary>
[HarmonyPatch(typeof(ChoiceLetter_GrowthMoment))]
public static class ChoiceLetter_GrowthMoment_Patch
{
    public static IntRange GenesToSelect = new IntRange(1, 3);

    [HarmonyPatch(nameof(ChoiceLetter_GrowthMoment.MakeChoices))]
    [HarmonyPostfix]
    public static void MakeChoices_Patch(ChoiceLetter_GrowthMoment __instance)
    {
        if (__instance.def != LetterDefOf.ChildToAdult)
            return;

        List<GeneDef> genePool = DefDatabase<GeneDef>
            .AllDefs.Where(g => g.HasModExtension<AgeUpGeneModDefExtension>())
            .ToList();
        if (genePool.Count <= 0)
            return;

        int geneCount = GenesToSelect.RandomInRange;
        List<GeneDef> genesToExclude = [];

        for (int i = 0; i < geneCount; i++)
        {
            if (
                !genePool
                    .Except(genesToExclude)
                    .TryRandomElementByWeight(
                        g =>
                            g.GetModExtension<AgeUpGeneModDefExtension>().WeightingForRandomSelection,
                        out GeneDef selectedGene
                    )
            )
            {
                continue;
            }

            genesToExclude.Add(selectedGene);
            genesToExclude.AddRange(
                selectedGene.GetModExtension<AgeUpGeneModDefExtension>().ConflictsWith
            );

            __instance.pawn.genes.AddGene(selectedGene, true);

            Messages.Message(
                "MSS_Gen_RandomGene".Translate(
                    __instance.pawn.NameFullColored,
                    selectedGene.LabelCap
                ),
                MessageTypeDefOf.NeutralEvent
            );
        }
    }
}
