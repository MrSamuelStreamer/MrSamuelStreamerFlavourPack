using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Utils;

public class GeneSettingsTab(ModSettings settings, Mod mod) : SettingsTab(settings, mod)
{
    public Settings Settings => settings as Settings;
    public override string TabName => "Genes";
    public override int TabOrder => 2;

    public override void DoTabContents(
        Listing_Standard options,
        Rect scrollViewRect,
        ref float scrollViewHeight
    )
    {
        DrawCheckBox(
            options,
            "MSS_FP_Settings_DisableBSIncorporateGeneLimit".Translate(),
            ref Settings.DisableBSIncorporateGeneLimit,
            ref scrollViewHeight
        );
        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnableGeneStealerNeed".Translate(),
            ref Settings.EnableGeneStealerNeed,
            ref scrollViewHeight
        );
        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnableGeneMutators".Translate(),
            ref Settings.EnableGeneMutators,
            ref scrollViewHeight
        );

        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnableGenesOnGrowthMoment".Translate(),
            ref Settings.EnableGenesOnGrowthMoment,
            ref scrollViewHeight
        );

        if (Settings.EnableGenesOnGrowthMoment)
        {
            Settings.GeneEventChance = options.SliderLabeled(
                "MSS_FP_GeneEventChance".Translate(Settings.GeneEventChance * 100),
                Settings.GeneEventChance,
                0f,
                1f,
                tooltip: "MSS_FP_GeneEventChance_Tooltip"
            );
            scrollViewHeight += 30f;

            float GoodGeneChanceUpd = options.SliderLabeled(
                "MSS_FP_GoodGeneChance".Translate(Settings.GoodGeneChance * 100),
                Settings.GoodGeneChance,
                0f,
                1f,
                tooltip: "MSS_FP_GoodGeneChance_Tooltip"
            );
            scrollViewHeight += 30f;
            float BadGeneChanceUpd = options.SliderLabeled(
                "MSS_FP_BadGeneChance".Translate(Settings.BadGeneChance * 100),
                Settings.BadGeneChance,
                0f,
                1f,
                tooltip: "MSS_FP_BadGeneChance_Tooltip"
            );
            scrollViewHeight += 30f;
            float NeutralGeneChanceUpd = options.SliderLabeled(
                "MSS_FP_NeutralGeneChance".Translate(Settings.NeutralGeneChance * 100),
                Settings.NeutralGeneChance,
                0f,
                1f,
                tooltip: "MSS_FP_NeutralGeneChance_Tooltip"
            );
            scrollViewHeight += 30f;
            float RandomGeneChanceUpd = options.SliderLabeled(
                "MSS_FP_RandomGeneChance".Translate(Settings.RandomGeneChance * 100),
                Settings.RandomGeneChance,
                0f,
                1f,
                tooltip: "MSS_FP_RandomGeneChance_Tooltip"
            );
            scrollViewHeight += 30f;

            if (!Mathf.Approximately(Settings.GoodGeneChance, GoodGeneChanceUpd))
            {
                AdjustChanceRatios(
                    Settings.GoodGeneChance - GoodGeneChanceUpd,
                    ref Settings.BadGeneChance,
                    ref Settings.NeutralGeneChance,
                    ref Settings.RandomGeneChance
                );
                Settings.GoodGeneChance = GoodGeneChanceUpd;
            }
            else if (!Mathf.Approximately(Settings.BadGeneChance, BadGeneChanceUpd))
            {
                AdjustChanceRatios(
                    Settings.BadGeneChance - BadGeneChanceUpd,
                    ref Settings.GoodGeneChance,
                    ref Settings.NeutralGeneChance,
                    ref Settings.RandomGeneChance
                );
                Settings.BadGeneChance = BadGeneChanceUpd;
            }
            else if (!Mathf.Approximately(Settings.NeutralGeneChance, NeutralGeneChanceUpd))
            {
                AdjustChanceRatios(
                    Settings.NeutralGeneChance - NeutralGeneChanceUpd,
                    ref Settings.GoodGeneChance,
                    ref Settings.BadGeneChance,
                    ref Settings.RandomGeneChance
                );
                Settings.NeutralGeneChance = NeutralGeneChanceUpd;
            }
            else if (!Mathf.Approximately(Settings.RandomGeneChance, RandomGeneChanceUpd))
            {
                AdjustChanceRatios(
                    Settings.RandomGeneChance - RandomGeneChanceUpd,
                    ref Settings.GoodGeneChance,
                    ref Settings.BadGeneChance,
                    ref Settings.NeutralGeneChance
                );
                Settings.RandomGeneChance = RandomGeneChanceUpd;
            }
        }
    }

    public void AdjustChanceRatios(
        float change,
        ref float chanceA,
        ref float chanceB,
        ref float chanceC
    )
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
        Scribe_Values.Look(
            ref Settings.DisableBSIncorporateGeneLimit,
            "DisableBSIncorporateGeneLimit",
            false
        );
        Scribe_Values.Look(ref Settings.EnableGeneStealerNeed, "EnableGeneStealerNeed", false);
        Scribe_Values.Look(ref Settings.EnableGeneMutators, "EnableGeneMutators", false);
        Scribe_Values.Look(ref Settings.GeneEventChance, "GeneEventChance", 1f);
        Scribe_Values.Look(ref Settings.GoodGeneChance, "GoodGeneChance", 1f / 4f);
        Scribe_Values.Look(ref Settings.BadGeneChance, "BadGeneChance", 1f / 4f);
        Scribe_Values.Look(ref Settings.NeutralGeneChance, "NeutralGeneChance", 1f / 4f);
        Scribe_Values.Look(ref Settings.RandomGeneChance, "RandomGeneChance", 1f / 4f);

        if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs || Scribe.mode == LoadSaveMode.Saving)
        {
            GeneDef incorporateGeneDef = DefDatabase<GeneDef>.GetNamed("BS_Incorporate", false);
            if (incorporateGeneDef != null)
            {
                incorporateGeneDef.description = Settings.DisableBSIncorporateGeneLimit
                    ? "MSSFP_BS_Incorporate_Desc2".Translate()
                    : "MSSFP_BS_Incorporate_Desc1".Translate();
            }

            AbilityDef incorporateAbilityDef = DefDatabase<AbilityDef>.GetNamed(
                "BS_Incorporate_Abillity",
                false
            );
            if (incorporateAbilityDef != null)
            {
                incorporateAbilityDef.description = Settings.DisableBSIncorporateGeneLimit
                    ? "MSSFP_BS_Incorporate_Desc2".Translate()
                    : "MSSFP_BS_Incorporate_Desc1".Translate();
            }
        }
    }
}
