using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MSSFP;

public class Settings : ModSettings
{
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

    public void DoWindowContents(Rect wrect)
    {
        Listing_Standard options = new();
        options.Begin(wrect);

        options.CheckboxLabeled("MSS_Mabel_Settings_DestroyFloors".Translate(), ref destroyFloors);
        options.Gap();

        options.CheckboxLabeled("MSS_FP_Settings_OverrideRelicPool".Translate(), ref overrideRelicPool);
        options.Gap();

        options.CheckboxLabeled("MSS_FP_Settings_disableFroggeNom".Translate(), ref disableFroggeNom);
        options.Gap();

        options.CheckboxLabeled("MSS_FP_Settings_ShowHaunts".Translate(), ref ShowHaunts);
        options.Gap();

        options.CheckboxLabeled("MSS_FP_Settings_NoSkylanternRaids".Translate(), ref NoSkylanternRaids);
        options.Gap();

        options.CheckboxLabeled("MSS_FP_Settings_DrawByMrStreamer".Translate(), ref DrawByMrStreamer);
        options.Gap();

        options.CheckboxLabeled("MSS_FP_Settings_EnableOutpostFission".Translate(), ref EnableOutpostFission);
        options.Gap();

        options.Label("MSS_FP_Settings_DaysForOutpostFission".Translate(DaysForOutpostFission));
        options.IntAdjuster(ref DaysForOutpostFission, 1, 1);

        options.Label("MSS_FP_Settings_DaysForFission".Translate(DaysForFission));
        options.IntAdjuster(ref DaysForFission, 1, 1);

        GeneEventChance = options.SliderLabeled("MSS_FP_GeneEventChance".Translate(GeneEventChance * 100), GeneEventChance, 0f, 1f, tooltip:"MSS_FP_GeneEventChance_Tooltip");

        float GoodGeneChanceUpd = options.SliderLabeled("MSS_FP_GoodGeneChance".Translate(GoodGeneChance * 100), GoodGeneChance, 0f, 1f, tooltip:"MSS_FP_GoodGeneChance_Tooltip");
        float BadGeneChanceUpd = options.SliderLabeled("MSS_FP_BadGeneChance".Translate(BadGeneChance * 100), BadGeneChance, 0f, 1f, tooltip:"MSS_FP_BadGeneChance_Tooltip");
        float NeutralGeneChanceUpd = options.SliderLabeled("MSS_FP_NeutralGeneChance".Translate(NeutralGeneChance * 100), NeutralGeneChance, 0f, 1f, tooltip:"MSS_FP_NeutralGeneChance_Tooltip");
        float RandomGeneChanceUpd = options.SliderLabeled("MSS_FP_RandomGeneChance".Translate(RandomGeneChance * 100), RandomGeneChance, 0f, 1f, tooltip:"MSS_FP_RandomGeneChance_Tooltip");

        if (!Mathf.Approximately(GoodGeneChance, GoodGeneChanceUpd))
        {
            AdjustChanceRatios(GoodGeneChance - GoodGeneChanceUpd, ref BadGeneChance, ref NeutralGeneChance, ref RandomGeneChance);
            GoodGeneChance = GoodGeneChanceUpd;
        }else if (!Mathf.Approximately(BadGeneChance, BadGeneChanceUpd))
        {
            AdjustChanceRatios(BadGeneChance - BadGeneChanceUpd, ref GoodGeneChance, ref NeutralGeneChance, ref RandomGeneChance);
            BadGeneChance = BadGeneChanceUpd;
        }else if (!Mathf.Approximately(NeutralGeneChance, NeutralGeneChanceUpd))
        {
            AdjustChanceRatios(NeutralGeneChance - NeutralGeneChanceUpd, ref GoodGeneChance, ref BadGeneChance, ref RandomGeneChance);
            NeutralGeneChance = NeutralGeneChanceUpd;
        }else if (!Mathf.Approximately(RandomGeneChance, RandomGeneChanceUpd))
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
        Scribe_Values.Look(ref DrawByMrStreamer, "EnableOutpostFission", true);
        Scribe_Values.Look(ref GeneEventChance, "GeneEventChance", 1f );
        Scribe_Values.Look(ref GoodGeneChance, "GoodGeneChance", 1f/4f);
        Scribe_Values.Look(ref BadGeneChance, "BadGeneChance", 1f/4f);
        Scribe_Values.Look(ref NeutralGeneChance, "NeutralGeneChance", 1f/4f);
        Scribe_Values.Look(ref RandomGeneChance, "RandomGeneChance", 1f/4f);
        Scribe_Values.Look(ref DaysForOutpostFission, "DaysForOutpostFission", 7);
        Scribe_Values.Look(ref DaysForFission, "DaysForFission", 7);
    }
}
