using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP;

public class Settings : ModSettings
{
    public static SimpleCurve MetabolismToFoodConsumptionFactorCurveNew = new SimpleCurve()
    {
        { new CurvePoint(-1000f, 5000f), true },
        { new CurvePoint(-100f, 200f), true },
        { new CurvePoint(-50f, 50f), true },
        { new CurvePoint(-40f, 40f), true },
        { new CurvePoint(-30f, 30f), true },
        { new CurvePoint(-20f, 20f), true },
        { new CurvePoint(-10f, 5f), true },
        { new CurvePoint(-5f, 2.25f), true },
        { new CurvePoint(0.0f, 1f), true },
        { new CurvePoint(5f, 0.5f), true },
        { new CurvePoint(10f, 0.05f), true },
        { new CurvePoint(100f, 0.005f), true },
        { new CurvePoint(1000f, 0f), true },
    };

    public static SimpleCurve MetabolismToFoodConsumptionFactorCurveOrig = new SimpleCurve()
    {
        { new CurvePoint(-5f, 2.25f), true },
        { new CurvePoint(0.0f, 1f), true },
        { new CurvePoint(5f, 0.5f), true },
    };

    private float ScrollViewHeight = 0;
    public Vector2 scrollPosition = Vector2.zero;

    public bool destroyFloors = true;
    public bool overrideRelicPool = false;
    public bool disableFroggeNom = false;
    public bool ShowHaunts = true;
    public bool NoSkylanternRaids = false;
    public bool DrawByMrStreamer = false;

    public float GeneEventChance = 1f;

    public float GoodGeneChance = 1f / 4f;
    public float BadGeneChance = 1f / 4f;
    public float NeutralGeneChance = 1f / 4f;
    public float RandomGeneChance = 1f / 4f;

    public bool EnableOutpostFission = true;
    public int DaysForOutpostFission = 15;
    public int DaysForFission = 7;

    public bool EnableLoversRetreat = true;
    public bool EnableFroggeIncidents = true;

    public bool SingleUseMentalFuses = true;

    public bool DisablePossession = false;

    public bool DisableBSIncorporateGeneLimit = false;
    public bool EnableExtendedMetabolismMultipliers = true;

    public bool DisableNonsenseIncidents = false;

    public void DrawCheckBox(Listing_Standard options, string label, ref bool value, ref float svh)
    {
        svh += Text.CalcHeight(label, options.ColumnWidth) + 12f;
        options.CheckboxLabeled(label, ref value);
        options.Gap();
    }

    public void DrawIntAdjuster(Listing_Standard options, string label, ref int value, int countChange, int min, ref float svh)
    {
        svh += options.Label(label).height;
        options.IntAdjuster(ref value, countChange, min);
        svh += 24f + options.verticalSpacing;
    }

    public void DoWindowContents(Rect wrect)
    {
        Rect viewRect = new Rect(0, 0, wrect.width - 20, ScrollViewHeight);
        ScrollViewHeight = 0;
        scrollPosition = GUI.BeginScrollView(new Rect(0, 50, wrect.width, wrect.height - 50), scrollPosition, viewRect);

        Listing_Standard options = new();
        options.Begin(viewRect);

        DrawCheckBox(options, "MSS_Mabel_Settings_DestroyFloors".Translate(), ref destroyFloors, ref ScrollViewHeight);
        DrawCheckBox(options, "MSS_FP_Settings_OverrideRelicPool".Translate(), ref overrideRelicPool, ref ScrollViewHeight);
        DrawCheckBox(options, "MSS_FP_Settings_disableFroggeNom".Translate(), ref disableFroggeNom, ref ScrollViewHeight);
        DrawCheckBox(options, "MSS_FP_Settings_ShowHaunts".Translate(), ref ShowHaunts, ref ScrollViewHeight);
        DrawCheckBox(options, "MSS_FP_Settings_NoSkylanternRaids".Translate(), ref NoSkylanternRaids, ref ScrollViewHeight);
        DrawCheckBox(options, "MSS_FP_Settings_DrawByMrStreamer".Translate(), ref DrawByMrStreamer, ref ScrollViewHeight);
        DrawCheckBox(options, "MSS_FP_Settings_EnableOutpostFission".Translate(), ref EnableOutpostFission, ref ScrollViewHeight);
        DrawCheckBox(options, "MSS_FP_Settings_EnableLoversRetreat".Translate(), ref EnableLoversRetreat, ref ScrollViewHeight);
        DrawCheckBox(options, "MSS_FP_Settings_EnableFroggeIncidents".Translate(), ref EnableFroggeIncidents, ref ScrollViewHeight);
        DrawCheckBox(options, "MSS_FP_Settings_SingleUseMentalFuses".Translate(), ref SingleUseMentalFuses, ref ScrollViewHeight);
        DrawCheckBox(options, "MSS_FP_Settings_DisablePossession".Translate(), ref DisablePossession, ref ScrollViewHeight);
        DrawCheckBox(options, "MSS_FP_Settings_DisableBSIncorporateGeneLimit".Translate(), ref DisableBSIncorporateGeneLimit, ref ScrollViewHeight);
        DrawCheckBox(options, "MSS_FP_Settings_EnableExtendedMetabolismMultipliers".Translate(), ref EnableExtendedMetabolismMultipliers, ref ScrollViewHeight);
        DrawCheckBox(options, "MSS_FP_Settings_DisableNonsenseIncidents".Translate(), ref DisableNonsenseIncidents, ref ScrollViewHeight);

        DrawIntAdjuster(options, "MSS_FP_Settings_DaysForOutpostFission".Translate(DaysForOutpostFission), ref DaysForOutpostFission, 1, 1, ref ScrollViewHeight);
        DrawIntAdjuster(options, "MSS_FP_Settings_DaysForFission".Translate(DaysForFission), ref DaysForFission, 1, 1, ref ScrollViewHeight);
        DrawIntAdjuster(options, "MSS_FP_Settings_DaysForFission".Translate(DaysForFission), ref DaysForFission, 1, 1, ref ScrollViewHeight);

        GeneEventChance = options.SliderLabeled("MSS_FP_GeneEventChance".Translate(GeneEventChance * 100), GeneEventChance, 0f, 1f, tooltip: "MSS_FP_GeneEventChance_Tooltip");
        ScrollViewHeight += 30f;

        float GoodGeneChanceUpd = options.SliderLabeled("MSS_FP_GoodGeneChance".Translate(GoodGeneChance * 100), GoodGeneChance, 0f, 1f, tooltip: "MSS_FP_GoodGeneChance_Tooltip");
        ScrollViewHeight += 30f;
        float BadGeneChanceUpd = options.SliderLabeled("MSS_FP_BadGeneChance".Translate(BadGeneChance * 100), BadGeneChance, 0f, 1f, tooltip: "MSS_FP_BadGeneChance_Tooltip");
        ScrollViewHeight += 30f;
        float NeutralGeneChanceUpd = options.SliderLabeled(
            "MSS_FP_NeutralGeneChance".Translate(NeutralGeneChance * 100),
            NeutralGeneChance,
            0f,
            1f,
            tooltip: "MSS_FP_NeutralGeneChance_Tooltip"
        );
        ScrollViewHeight += 30f;
        float RandomGeneChanceUpd = options.SliderLabeled(
            "MSS_FP_RandomGeneChance".Translate(RandomGeneChance * 100),
            RandomGeneChance,
            0f,
            1f,
            tooltip: "MSS_FP_RandomGeneChance_Tooltip"
        );
        ScrollViewHeight += 30f;

        if (!Mathf.Approximately(GoodGeneChance, GoodGeneChanceUpd))
        {
            AdjustChanceRatios(GoodGeneChance - GoodGeneChanceUpd, ref BadGeneChance, ref NeutralGeneChance, ref RandomGeneChance);
            GoodGeneChance = GoodGeneChanceUpd;
        }
        else if (!Mathf.Approximately(BadGeneChance, BadGeneChanceUpd))
        {
            AdjustChanceRatios(BadGeneChance - BadGeneChanceUpd, ref GoodGeneChance, ref NeutralGeneChance, ref RandomGeneChance);
            BadGeneChance = BadGeneChanceUpd;
        }
        else if (!Mathf.Approximately(NeutralGeneChance, NeutralGeneChanceUpd))
        {
            AdjustChanceRatios(NeutralGeneChance - NeutralGeneChanceUpd, ref GoodGeneChance, ref BadGeneChance, ref RandomGeneChance);
            NeutralGeneChance = NeutralGeneChanceUpd;
        }
        else if (!Mathf.Approximately(RandomGeneChance, RandomGeneChanceUpd))
        {
            AdjustChanceRatios(RandomGeneChance - RandomGeneChanceUpd, ref GoodGeneChance, ref BadGeneChance, ref NeutralGeneChance);
            RandomGeneChance = RandomGeneChanceUpd;
        }

        options.End();
    }

    public void AdjustChanceRatios(float change, ref float chanceA, ref float chanceB, ref float chanceC)
    {
        float total = chanceA + chanceB + chanceC;
        float aRatio = chanceA / total;
        float bRatio = chanceB / total;
        float cRatio = chanceC / total;

        chanceA += change * aRatio;
        chanceB += change * bRatio;
        chanceC += change * cRatio;
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref destroyFloors, "destroyFloors", true);
        Scribe_Values.Look(ref overrideRelicPool, "overrideRelicPool", false);
        Scribe_Values.Look(ref disableFroggeNom, "disableFrogge", false);
        Scribe_Values.Look(ref ShowHaunts, "ShowHaunts", true);
        Scribe_Values.Look(ref NoSkylanternRaids, "NoSkylanternRaids", false);
        Scribe_Values.Look(ref DrawByMrStreamer, "DrawByMrStreamer", false);
        Scribe_Values.Look(ref EnableOutpostFission, "EnableOutpostFission", true);
        Scribe_Values.Look(ref EnableLoversRetreat, "EnableLoversRetreat", true);
        Scribe_Values.Look(ref EnableFroggeIncidents, "EnableFroggeIncidents", true);
        Scribe_Values.Look(ref SingleUseMentalFuses, "SingleUseMentalFuses", true);
        Scribe_Values.Look(ref DisablePossession, "DisablePossession", false);
        Scribe_Values.Look(ref DisablePossession, "DisableNonsenseIncidents", false);
        Scribe_Values.Look(ref DisableBSIncorporateGeneLimit, "DisableBSIncorporateGeneLimit", true);
        Scribe_Values.Look(ref EnableExtendedMetabolismMultipliers, "EnableExtendedMetabolismMultipliers", true);
        Scribe_Values.Look(ref GeneEventChance, "GeneEventChance", 1f);
        Scribe_Values.Look(ref GoodGeneChance, "GoodGeneChance", 1f / 4f);
        Scribe_Values.Look(ref BadGeneChance, "BadGeneChance", 1f / 4f);
        Scribe_Values.Look(ref NeutralGeneChance, "NeutralGeneChance", 1f / 4f);
        Scribe_Values.Look(ref RandomGeneChance, "RandomGeneChance", 1f / 4f);
        Scribe_Values.Look(ref DaysForOutpostFission, "DaysForOutpostFission", 7);
        Scribe_Values.Look(ref DaysForFission, "DaysForFission", 7);

        GeneDef inco = DefDatabase<GeneDef>.GetNamed("BS_Incorporate");
        if (inco != null)
        {
            inco.description = DisableBSIncorporateGeneLimit ? "MSSFP_BS_Incorporate_Desc2".Translate() : "MSSFP_BS_Incorporate_Desc1".Translate();
        }
        AbilityDef incoab = DefDatabase<AbilityDef>.GetNamed("BS_Incorporate_Abillity");
        if (incoab != null)
        {
            incoab.description = DisableBSIncorporateGeneLimit ? "MSSFP_BS_Incorporate_Desc2".Translate() : "MSSFP_BS_Incorporate_Desc1".Translate();
        }

        FieldInfo MetabolismToFoodConsumptionFactorCurveField = AccessTools.Field(typeof(GeneTuning), nameof(GeneTuning.MetabolismToFoodConsumptionFactorCurve));

        MetabolismToFoodConsumptionFactorCurveField.SetValue(
            null,
            EnableExtendedMetabolismMultipliers ? MetabolismToFoodConsumptionFactorCurveNew : MetabolismToFoodConsumptionFactorCurveOrig
        );
    }
}
