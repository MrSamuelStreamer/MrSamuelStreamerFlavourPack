using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP;

public class Settings : ModSettings
{
    private float ScrollViewHeight = 0;
    public Vector2 scrollPosition = Vector2.zero;

    public bool MabelDestroyFloors = false;
    public bool OverrideRelicPool = false;
    public bool DisableFroggeNom = false;
    public bool EnableMogus = false;
    public bool ShowHaunts = false;
    public bool EnableSkylanternRaids = false;
    public bool DrawByMrStreamer = false;
    public bool EnableGenesOnGrowthMoment = false;
    public float GeneEventChance = 1f;
    public float GoodGeneChance = 1f / 4f;
    public float BadGeneChance = 1f / 4f;
    public float NeutralGeneChance = 1f / 4f;
    public float RandomGeneChance = 1f / 4f;
    public bool EnableOutpostFission = false;
    public int DaysForOutpostFission = 15;
    public int DaysForFission = 7;
    public bool EnableLoversRetreat = false;
    public bool EnableFroggeIncidents = false;
    public bool SingleUseMentalFuses = false;
    public bool EnablePossession = false;
    public bool DisableBSIncorporateGeneLimit = false;
    public bool EnableNonsenseIncidents = false;
    public bool EnableGeneStealerNeed = false;
    public bool EnableDirtJobs = false;
    public bool EnableOskarianTech = false;
    public bool EnableGeneMutators = false;
    public bool EnableTrekBeamers = true;
    public bool EnableTaffRaids = true;
    public bool EnableWanderDelayModification = false;
    public int WanderDelayTicks = 0;

    public void DrawCheckBox(Listing_Standard options, string label, ref bool value, ref float svh)
    {
        float height = Text.CalcHeight(label, options.ColumnWidth) + 12f;
        options.CheckboxLabeled(label, ref value, height: height);
        svh += height;
    }

    public void DrawIntAdjuster(Listing_Standard options, string label, ref int value, int countChange, int min, ref float svh)
    {
        svh += options.Label(label).height;
        options.IntAdjuster(ref value, countChange, min);
        svh += 24f + options.verticalSpacing;
    }

    public void DoWindowContents(Rect wrect)
    {
        Rect viewRect = new(0, 0, wrect.width - 16, ScrollViewHeight);
        ScrollViewHeight = 0;
        scrollPosition = GUI.BeginScrollView(new Rect(0, 40, wrect.width, wrect.height), scrollPosition, viewRect);

        Listing_Standard options = new();
        options.Begin(viewRect);

        try
        {
            DrawCheckBox(options, "MSS_Mabel_Settings_DestroyFloors".Translate(), ref MabelDestroyFloors, ref ScrollViewHeight);
            DrawCheckBox(options, "MSS_FP_Settings_OverrideRelicPool".Translate(), ref OverrideRelicPool, ref ScrollViewHeight);
            DrawCheckBox(options, "MSS_FP_Settings_disableFroggeNom".Translate(), ref DisableFroggeNom, ref ScrollViewHeight);
            DrawCheckBox(options, "MSS_FP_Settings_enableMogus".Translate(), ref EnableMogus, ref ScrollViewHeight);
            DrawCheckBox(options, "MSS_FP_Settings_ShowHaunts".Translate(), ref ShowHaunts, ref ScrollViewHeight);
            DrawCheckBox(options, "MSS_FP_Settings_NoSkylanternRaids".Translate(), ref EnableSkylanternRaids, ref ScrollViewHeight);
            DrawCheckBox(options, "MSS_FP_Settings_DrawByMrStreamer".Translate(), ref DrawByMrStreamer, ref ScrollViewHeight);
            DrawCheckBox(options, "MSS_FP_Settings_EnableOutpostFission".Translate(), ref EnableOutpostFission, ref ScrollViewHeight);
            DrawCheckBox(options, "MSS_FP_Settings_EnableLoversRetreat".Translate(), ref EnableLoversRetreat, ref ScrollViewHeight);
            DrawCheckBox(options, "MSS_FP_Settings_EnableFroggeIncidents".Translate(), ref EnableFroggeIncidents, ref ScrollViewHeight);
            DrawCheckBox(options, "MSS_FP_Settings_SingleUseMentalFuses".Translate(), ref SingleUseMentalFuses, ref ScrollViewHeight);
            DrawCheckBox(options, "MSS_FP_Settings_EnablePossession".Translate(), ref EnablePossession, ref ScrollViewHeight);
            DrawCheckBox(options, "MSS_FP_Settings_DisableBSIncorporateGeneLimit".Translate(), ref DisableBSIncorporateGeneLimit, ref ScrollViewHeight);
            DrawCheckBox(options, "MSS_FP_Settings_EnableNonsenseIncidents".Translate(), ref EnableNonsenseIncidents, ref ScrollViewHeight);
            DrawCheckBox(options, "MSS_FP_Settings_EnableGeneStealerNeed".Translate(), ref EnableGeneStealerNeed, ref ScrollViewHeight);
            DrawCheckBox(options, "MSS_FP_Settings_EnableDirtJobs".Translate(), ref EnableDirtJobs, ref ScrollViewHeight);
            DrawCheckBox(options, "MSS_FP_Settings_EnableTrekBeamers".Translate(), ref EnableTrekBeamers, ref ScrollViewHeight);
            DrawCheckBox(options, "MSS_FP_Settings_EnableTaffRaids".Translate(), ref EnableTaffRaids, ref ScrollViewHeight);
            DrawCheckBox(options, "MSS_FP_Settings_EnableOskarianTech".Translate(), ref EnableOskarianTech, ref ScrollViewHeight);
            DrawCheckBox(options, "MSS_FP_Settings_EnableGeneMutators".Translate(), ref EnableGeneMutators, ref ScrollViewHeight);

            DrawCheckBox(options, "MSS_FP_Settings_EnableWanderDelayModification".Translate(), ref EnableWanderDelayModification, ref ScrollViewHeight);

            if (EnableWanderDelayModification)
            {
                float wanderDelaySeconds = WanderDelayTicks / 60f;
                wanderDelaySeconds = options.SliderLabeled(
                    "MSS_FP_Settings_WanderDelaySeconds".Translate((wanderDelaySeconds).ToString("F1")),
                    wanderDelaySeconds,
                    -2f,
                    200f,
                    tooltip: "MSS_FP_Settings_WanderDelaySeconds_Tooltip"
                );
                WanderDelayTicks = Mathf.RoundToInt(wanderDelaySeconds * 60f);
                ScrollViewHeight += 30f;
            }

            DrawCheckBox(options, "MSS_FP_Settings_EnableGenesOnGrowthMoment".Translate(), ref EnableGenesOnGrowthMoment, ref ScrollViewHeight);

            if (EnableGenesOnGrowthMoment)
            {
                GeneEventChance = options.SliderLabeled(
                    "MSS_FP_GeneEventChance".Translate(GeneEventChance * 100),
                    GeneEventChance,
                    0f,
                    1f,
                    tooltip: "MSS_FP_GeneEventChance_Tooltip"
                );
                ScrollViewHeight += 30f;

                float GoodGeneChanceUpd = options.SliderLabeled(
                    "MSS_FP_GoodGeneChance".Translate(GoodGeneChance * 100),
                    GoodGeneChance,
                    0f,
                    1f,
                    tooltip: "MSS_FP_GoodGeneChance_Tooltip"
                );
                ScrollViewHeight += 30f;
                float BadGeneChanceUpd = options.SliderLabeled(
                    "MSS_FP_BadGeneChance".Translate(BadGeneChance * 100),
                    BadGeneChance,
                    0f,
                    1f,
                    tooltip: "MSS_FP_BadGeneChance_Tooltip"
                );
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
            }

            DrawIntAdjuster(options, "MSS_FP_Settings_DaysForOutpostFission".Translate(DaysForOutpostFission), ref DaysForOutpostFission, 1, 1, ref ScrollViewHeight);
            DrawIntAdjuster(options, "MSS_FP_Settings_DaysForFission".Translate(DaysForFission), ref DaysForFission, 1, 1, ref ScrollViewHeight);

            ScrollViewHeight += 50;
        }
        finally
        {
            GUI.EndScrollView();
            options.End();
        }
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
        Scribe_Values.Look(ref MabelDestroyFloors, "destroyFloors", false);
        Scribe_Values.Look(ref OverrideRelicPool, "overrideRelicPool", false);
        Scribe_Values.Look(ref DisableFroggeNom, "disableFrogge", false);
        Scribe_Values.Look(ref EnableMogus, "enableMogus", false);
        Scribe_Values.Look(ref ShowHaunts, "ShowHaunts", false);
        Scribe_Values.Look(ref EnableSkylanternRaids, "NoSkylanternRaids", false);
        Scribe_Values.Look(ref DrawByMrStreamer, "DrawByMrStreamer", false);
        Scribe_Values.Look(ref EnableOutpostFission, "EnableOutpostFission", false);
        Scribe_Values.Look(ref EnableLoversRetreat, "EnableLoversRetreat", false);
        Scribe_Values.Look(ref EnableFroggeIncidents, "EnableFroggeIncidents", false);
        Scribe_Values.Look(ref SingleUseMentalFuses, "SingleUseMentalFuses", false);
        Scribe_Values.Look(ref EnablePossession, "DisablePossession", false);
        Scribe_Values.Look(ref EnableNonsenseIncidents, "DisableNonsenseIncidents", true);
        Scribe_Values.Look(ref DisableBSIncorporateGeneLimit, "DisableBSIncorporateGeneLimit", false);
        Scribe_Values.Look(ref EnableGeneStealerNeed, "EnableGeneStealerNeed", false);
        Scribe_Values.Look(ref EnableGenesOnGrowthMoment, "EnableGenesOnGrowthMoment", false);
        Scribe_Values.Look(ref EnableOskarianTech, "EnableOskarianTech", false);
        Scribe_Values.Look(ref EnableOskarianTech, "EnableGeneMutators", false);
        Scribe_Values.Look(ref EnableDirtJobs, "EnableDirtJobs", false);
        Scribe_Values.Look(ref EnableTrekBeamers, "EnableTrekBeamers", true);
        Scribe_Values.Look(ref EnableTaffRaids, "EnableTaffRaids", true);
        Scribe_Values.Look(ref EnableWanderDelayModification, "EnableWanderDelayModification", false);
        Scribe_Values.Look(ref WanderDelayTicks, "WanderDelayTicks", 0);
        Scribe_Values.Look(ref GeneEventChance, "GeneEventChance", 1f);
        Scribe_Values.Look(ref GoodGeneChance, "GoodGeneChance", 1f / 4f);
        Scribe_Values.Look(ref BadGeneChance, "BadGeneChance", 1f / 4f);
        Scribe_Values.Look(ref NeutralGeneChance, "NeutralGeneChance", 1f / 4f);
        Scribe_Values.Look(ref RandomGeneChance, "RandomGeneChance", 1f / 4f);
        Scribe_Values.Look(ref DaysForOutpostFission, "DaysForOutpostFission", 7);
        Scribe_Values.Look(ref DaysForFission, "DaysForFission", 7);

        if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs || Scribe.mode == LoadSaveMode.Saving)
        {
            GeneDef incorporateGeneDef = DefDatabase<GeneDef>.GetNamed("BS_Incorporate", false);
            if (incorporateGeneDef != null)
            {
                incorporateGeneDef.description = DisableBSIncorporateGeneLimit ? "MSSFP_BS_Incorporate_Desc2".Translate() : "MSSFP_BS_Incorporate_Desc1".Translate();
            }

            AbilityDef incorporateAbilityDef = DefDatabase<AbilityDef>.GetNamed("BS_Incorporate_Abillity", false);
            if (incorporateAbilityDef != null)
            {
                incorporateAbilityDef.description = DisableBSIncorporateGeneLimit ? "MSSFP_BS_Incorporate_Desc2".Translate() : "MSSFP_BS_Incorporate_Desc1".Translate();
            }
        }
    }
}
