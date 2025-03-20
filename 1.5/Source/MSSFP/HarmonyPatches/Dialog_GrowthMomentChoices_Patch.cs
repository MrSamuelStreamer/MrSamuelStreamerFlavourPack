using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MSSFP.Genes;
using RimWorld;
using UnityEngine;
using Verse;
using GeneType = MSSFP.Genes.GeneType;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Patches to add gene selection to growth moments
/// </summary>
[HarmonyPatch(typeof(Dialog_GrowthMomentChoices))]
public static class Dialog_GrowthMomentChoices_Patch
{
    public struct Choices
    {
        public Choices()
        {
            ShouldDoChoice = Rand.Chance(MSSFPMod.settings.GeneEventChance);
        }

        public GeneClassification selectedGene;
        public List<GeneClassification> geneChoices;
        public GeneType? type;
        public bool ShouldDoChoice;
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
        if (__result && DialogLookup.TryGetValue(__instance, out Choices value) && value.ShouldDoChoice)
        {
            __result = value.selectedGene != null;
        }
    }

    public static Lazy<FieldInfo> Letter = new(() => AccessTools.Field(typeof(Dialog_GrowthMomentChoices), "letter"));

    public static IntRange GeneRange = new IntRange(2, 12);

    public static GeneType GetRandomGeneType()
    {
        Dictionary<GeneType, float> weights = new Dictionary<GeneType, float>();
        foreach (GeneType type in Enum.GetValues(typeof(GeneType)))
        {
            switch (type)
            {
                case GeneType.good:
                    weights[type] = MSSFPMod.settings.GoodGeneChance;
                    break;
                case GeneType.bad:
                    weights[type] = MSSFPMod.settings.BadGeneChance;
                    break;
                case GeneType.neutral:
                    weights[type] = MSSFPMod.settings.NeutralGeneChance;
                    break;
                case GeneType.random:
                    weights[type] = MSSFPMod.settings.NeutralGeneChance;
                    break;
                default:
                    weights[type] = 0;
                    break;
            }
        }

        return weights.RandomElementByWeight(weight => weight.Value).Key;
    }

    public static List<GeneClassification> GeneChoicesForPawn(Pawn pawn, out GeneType? geneType)
    {
        GeneType randType = GetRandomGeneType();
        GeneClassificationDef classification =
            randType == GeneType.random
                ? DefDatabase<GeneClassificationDef>.AllDefs.RandomElement()
                : DefDatabase<GeneClassificationDef>.AllDefs.Where(g => g.type == randType).RandomElement();

        List<GeneClassification> validGenes = classification
            .genes.Where(g =>
                g.gene != null
                && !pawn.genes.GenesListForReading.Any(pg => pg.def.ConflictsWith(g.gene))
                && (g.gene.prerequisite == null || pawn.genes.GenesListForReading.Any(pg => pg.def == g.gene.prerequisite))
            )
            .ToList();

        List<GeneClassification> output = new();
        for (int i = 0; i < GeneRange.RandomInRange; i++)
        {
            output.Add(validGenes.Except(output).RandomElementByWeight(g => g.weight));
        }

        geneType = randType;
        return output.ToList();
    }

    public static void DrawGeneSelector(Dialog_GrowthMomentChoices instance, float width, ref float curY)
    {
        if (!DialogLookup.ContainsKey(instance))
        {
            DialogLookup[instance] = new Choices();
        }

        Choices currentChoices = DialogLookup[instance];

        if (!currentChoices.ShouldDoChoice)
            return;

        ChoiceLetter_GrowthMoment letter = (ChoiceLetter_GrowthMoment)Letter.Value.GetValue(instance);

        if (letter.ArchiveView)
        {
            return;
        }

        if (currentChoices.geneChoices.NullOrEmpty())
        {
            currentChoices.geneChoices = GeneChoicesForPawn(letter.pawn, out currentChoices.type);
        }

        string type = "Random";
        if (currentChoices.type != null)
            type = currentChoices.type.ToString();

        Widgets.Label(0.0f, ref curY, width, "MSS_BirthdayPickGene".Translate((NamedArgument)(Thing)letter.pawn, type).Resolve() + ":");

        Listing_Standard listingStandard = new Listing_Standard();
        Rect rect = new Rect(0.0f, curY, 230f, 99999f);
        listingStandard.Begin(rect);
        foreach (GeneClassification gene in currentChoices.geneChoices)
        {
            if (listingStandard.RadioButton(gene.gene.LabelCap, currentChoices.selectedGene == gene, 30f, gene.gene.DescriptionFull))
                currentChoices.selectedGene = gene;
        }
        listingStandard.End();
        curY += (float)(listingStandard.CurHeight + 10.0 + 4.0);
        DialogLookup[instance] = currentChoices;
    }

    public static void MakeChoicesHook(Dialog_GrowthMomentChoices instance)
    {
        if (!DialogLookup.TryGetValue(instance, out Choices currentChoices))
            return;
        if (!currentChoices.ShouldDoChoice)
        {
            DialogLookup.Remove(instance);
            return;
        }

        ChoiceLetter_GrowthMoment letter = (ChoiceLetter_GrowthMoment)Letter.Value.GetValue(instance);

        bool isXenoGene = Rand.Chance(0.95f);
        if (!currentChoices.selectedGene.requires.NullOrEmpty())
        {
            foreach (GeneDef geneDef in currentChoices.selectedGene.requires)
            {
                letter.pawn.genes.AddGene(geneDef, isXenoGene);
            }
        }

        letter.pawn.genes.AddGene(currentChoices.selectedGene.gene, isXenoGene);

        DialogLookup.Remove(instance);
    }
}
