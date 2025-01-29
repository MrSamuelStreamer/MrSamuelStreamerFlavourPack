using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;


[HarmonyPatch(typeof(Dialog_GrowthMomentChoices))]
public static class Dialog_GrowthMomentChoices_Patch
{
    public struct Choices
    {
        public Choices(){
            IsGood = Rand.Chance(0.2f);
        }
        public bool IsGood;
        public GeneClassification selectedGene;
        public List<GeneClassification> geneChoices;
        public GeneType type;
    }

    public static Dictionary<Dialog_GrowthMomentChoices, Choices> DialogLookup = new Dictionary<Dialog_GrowthMomentChoices, Choices>();

    public static Lazy<MethodInfo> DrawTraitChoices = new Lazy<MethodInfo>(() => AccessTools.Method(typeof(Dialog_GrowthMomentChoices), "DrawTraitChoices"));
    public static Lazy<MethodInfo> MakeChoices = new Lazy<MethodInfo>(() => AccessTools.Method(typeof(ChoiceLetter_GrowthMoment), "MakeChoices"));
    public static Lazy<MethodInfo> Width = new Lazy<MethodInfo>(() => AccessTools.PropertyGetter(typeof(UnityEngine.Rect), "width"));
    public static Lazy<MethodInfo> DrawGeneSelectorInfo = new Lazy<MethodInfo>(() => AccessTools.Method(typeof(Dialog_GrowthMomentChoices_Patch), "DrawGeneSelector"));
    public static Lazy<MethodInfo> MakeChoicesHookInfo = new Lazy<MethodInfo>(() => AccessTools.Method(typeof(Dialog_GrowthMomentChoices_Patch), "MakeChoicesHook"));

    [HarmonyPatch(nameof(Dialog_GrowthMomentChoices.DoWindowContents))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> DoWindowContentsTranspiler(IEnumerable<CodeInstruction> instructionsEnumerable)
    {
        foreach (CodeInstruction instruction in instructionsEnumerable)
        {
            yield return instruction;
            if (instruction.opcode == OpCodes.Call && instruction.operand as MethodInfo == DrawTraitChoices.Value)
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldloca_S, 2);
                yield return new CodeInstruction(OpCodes.Call, Width.Value);
                yield return new CodeInstruction(OpCodes.Ldloca_S, 6);
                yield return new CodeInstruction(OpCodes.Call, DrawGeneSelectorInfo.Value);
            }

            if (instruction.opcode == OpCodes.Callvirt && instruction.operand as MethodInfo == MakeChoices.Value)
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                // yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                yield return new CodeInstruction(OpCodes.Call, MakeChoicesHookInfo.Value);

            }
        }
    }

    [HarmonyPatch("SelectionsMade")]
    [HarmonyPostfix]
    public static void SelectionsMadePostfix(Dialog_GrowthMomentChoices __instance, ref bool __result)
    {
        if (__result && DialogLookup.TryGetValue(__instance, out Choices value))
        {
            __result = value.selectedGene != null;
        }
    }

    public static Lazy<FieldInfo> Letter = new(() => AccessTools.Field(typeof(Dialog_GrowthMomentChoices), "letter"));

    public static IntRange GeneRange = new IntRange(2, 12);
    public static List<GeneClassification> GeneChoicesForPawn(Pawn pawn, bool good, out GeneType geneType)
    {
        GeneClassificationDef classification = DefDatabase<GeneClassificationDef>.AllDefs.RandomElementByWeight(g => g.selectionWeight);

        geneType = classification.type;

        List<GeneClassification> validGenes = classification.genes.Where(g => g.gene != null &&
            !pawn.genes.GenesListForReading.Any(pg => pg.def.ConflictsWith(g.gene)) &&
            (g.gene.prerequisite == null || pawn.genes.GenesListForReading.Any(pg => pg.def == g.gene.prerequisite))).ToList();

        List<GeneClassification> output = new List<GeneClassification>();
        for (int i = 0; i < GeneRange.RandomInRange; i++)
        {
            output.Add(validGenes.Except(output).RandomElementByWeight(g=>g.weight));
        }

        return output.ToList();
    }

    public static void DrawGeneSelector(Dialog_GrowthMomentChoices instance, float width, ref float curY)
    {
        if (!DialogLookup.ContainsKey(instance))
        {
            DialogLookup[instance] = new Choices();
        }

        Choices currentChoices = DialogLookup[instance];

        ChoiceLetter_GrowthMoment letter = (ChoiceLetter_GrowthMoment)Letter.Value.GetValue(instance);

        if (letter.ArchiveView)
        {
            return;
        }

        if (currentChoices.geneChoices.NullOrEmpty())
        {
            currentChoices.geneChoices = GeneChoicesForPawn(letter.pawn, true, out currentChoices.type);
        }

        Widgets.Label(0.0f, ref curY, width, "MSS_BirthdayPickGene".Translate((NamedArgument) (Thing) letter.pawn, currentChoices.type.ToString()).Resolve() + ":");


        Listing_Standard listingStandard = new Listing_Standard();
        Rect rect = new Rect(0.0f, curY, 230f, 99999f);
        listingStandard.Begin(rect);
        foreach (GeneClassification gene in currentChoices.geneChoices)
        {
            if (listingStandard.RadioButton(gene.gene.LabelCap, currentChoices.selectedGene == gene, 30f, gene.gene.DescriptionFull))
                currentChoices.selectedGene = gene;
        }
        listingStandard.End();
        curY += (float) (listingStandard.CurHeight + 10.0 + 4.0);
        DialogLookup[instance] = currentChoices;
    }

    public static void MakeChoicesHook(Dialog_GrowthMomentChoices instance)
    {
        if(!DialogLookup.TryGetValue(instance, out Choices currentChoices)) return;

        ChoiceLetter_GrowthMoment letter = (ChoiceLetter_GrowthMoment)Letter.Value.GetValue(instance);

        if (!currentChoices.selectedGene.requires.NullOrEmpty())
        {
            foreach (GeneDef geneDef in currentChoices.selectedGene.requires)
            {
                letter.pawn.genes.AddGene(geneDef, Rand.Chance(0.95f));
            }
        }

        letter.pawn.genes.AddGene(currentChoices.selectedGene.gene, false);

        DialogLookup.Remove(instance);
    }
}
