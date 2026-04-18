using UnityEngine;
using Verse;

namespace MSSFP.Utils;

public class MemesSettingsTab(ModSettings settings, Mod mod) : SettingsTab(settings, mod)
{
    public Settings Settings => settings as Settings;
    public override string TabName => "Memes";
    public override int TabOrder => 3;

    public override void DoTabContents(
        Listing_Standard options,
        Rect scrollViewRect,
        ref float scrollViewHeight
    )
    {
        // --- Social ---
        DrawSectionHeader(options, "MSS_FP_Settings_Section_Social".Translate(), ref scrollViewHeight);

        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnableLoversRetreat".Translate(),
            ref Settings.EnableLoversRetreat,
            ref scrollViewHeight
        );

        if (Settings.EnableLoversRetreat)
        {
            DrawCheckBox(
                options,
                "MSS_FP_Settings_AllowAnyPregnant".Translate(),
                ref Settings.allowAnyPregnant,
                ref scrollViewHeight
            );
        }

        DrawCheckBox(
            options,
            "MSS_FP_Settings_SingleUseMentalFuses".Translate(),
            ref Settings.SingleUseMentalFuses,
            ref scrollViewHeight
        );

        // --- Events ---
        DrawSectionHeader(options, "MSS_FP_Settings_Section_Events".Translate(), ref scrollViewHeight);

        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnableTrekBeamers".Translate(),
            ref Settings.EnableTrekBeamers,
            ref scrollViewHeight
        );
        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnableMercenaryHiring".Translate(),
            ref Settings.EnableMercenaryHiring,
            ref scrollViewHeight
        );
        if (Settings.EnableMercenaryHiring)
        {
            DrawCheckBox(
                options,
                "MSS_FP_Settings_UseMrStreamerMercenaries".Translate(),
                ref Settings.useMrStreamerMercenaries,
                ref scrollViewHeight
            );
        }

        bool disableSkylanternRaids = !Settings.EnableSkylanternRaids;
        DrawCheckBox(
            options,
            "MSS_FP_Settings_NoSkylanternRaids".Translate(),
            ref disableSkylanternRaids,
            ref scrollViewHeight
        );
        Settings.EnableSkylanternRaids = !disableSkylanternRaids;

        DrawIntAdjuster(
            options,
            "MSS_FP_Settings_DaysForFission".Translate(Settings.DaysForFission),
            ref Settings.DaysForFission,
            1,
            1,
            ref scrollViewHeight
        );

        // --- Combat ---
        DrawSectionHeader(options, "MSS_FP_Settings_Section_Combat".Translate(), ref scrollViewHeight);

        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnableRecoilDamage".Translate(),
            ref Settings.EnableRecoilDamage,
            ref scrollViewHeight
        );

        Settings.RecoilDamageMultiplier = options.SliderLabeled(
            "MSS_FP_Settings_RecoilDamageMultiplier".Translate(Settings.RecoilDamageMultiplier),
            Settings.RecoilDamageMultiplier, 0f, 2f);
        scrollViewHeight += 30f;

        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnableRecoilKnockback".Translate(),
            ref Settings.EnableRecoilKnockback,
            ref scrollViewHeight
        );
        Settings.RecoilKnockbackMultiplier = options.SliderLabeled(
            "MSS_FP_Settings_RecoilKnockbackMultiplier".Translate(Settings.RecoilKnockbackMultiplier),
            Settings.RecoilKnockbackMultiplier, 0f, 2f);
        scrollViewHeight += 30f;

        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnableCodexPunch".Translate(),
            ref Settings.EnableCodexPunch,
            ref scrollViewHeight
        );
        if (Settings.EnableCodexPunch)
        {
            Settings.CodexPunchChanceMultiplier = options.SliderLabeled(
                "MSS_FP_Settings_CodexPunchChanceMultiplier".Translate(Settings.CodexPunchChanceMultiplier.ToString("F1")),
                Settings.CodexPunchChanceMultiplier, 0.5f, 5.0f);
            scrollViewHeight += 30f;
        }

    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref Settings.EnableLoversRetreat, "EnableLoversRetreat", false);
        Scribe_Values.Look(ref Settings.allowAnyPregnant, "allowAnyPregnant", false);
        Scribe_Values.Look(ref Settings.SingleUseMentalFuses, "SingleUseMentalFuses", false);
        Scribe_Values.Look(ref Settings.EnableTrekBeamers, "EnableTrekBeamers", true);
        Scribe_Values.Look(ref Settings.EnableSkylanternRaids, "EnableSkylanternRaids", false);
        Scribe_Values.Look(ref Settings.EnableMercenaryHiring, "EnableMercenaryHiring", true);
        Scribe_Values.Look(ref Settings.DaysForFission, "DaysForFission", 7);
        Scribe_Values.Look(ref Settings.EnableCodexPunch, "EnableCodexPunch", true);
        Scribe_Values.Look(ref Settings.CodexPunchChanceMultiplier, "CodexPunchChanceMultiplier", 1.0f);
    }
}
