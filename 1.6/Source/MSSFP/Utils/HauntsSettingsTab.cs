using UnityEngine;
using Verse;

namespace MSSFP.Utils;

public class HauntSettingsTab(ModSettings settings, Mod mod) : SettingsTab(settings, mod)
{
    public Settings Settings => settings as Settings;
    public override string TabName => "Haunts";
    public override int TabOrder => 1;

    public override void DoTabContents(
        Listing_Standard options,
        Rect scrollViewRect,
        ref float scrollViewHeight
    )
    {
        // --- Core ---
        DrawSectionHeader(options, "MSS_FP_Settings_Section_HauntCore".Translate(), ref scrollViewHeight);

        DrawCheckBox(
            options,
            "MSS_FP_Settings_ShowHaunts".Translate(),
            ref Settings.ShowHaunts,
            ref scrollViewHeight
        );
        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnableEcho".Translate(),
            ref Settings.EnableEcho,
            ref scrollViewHeight
        );
        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnableGraveHaunts".Translate(),
            ref Settings.EnableGraveHaunts,
            ref scrollViewHeight
        );
        DrawCheckBox(
            options,
            "MSS_FP_Settings_ShowHauntDevDashboard".Translate(),
            ref Settings.ShowHauntDevDashboard,
            ref scrollViewHeight
        );
        DrawCheckBox(
            options,
            "MSS_FP_Settings_AlwaysShowNamedHaunts".Translate(),
            ref Settings.AlwaysShowNamedHaunts,
            ref scrollViewHeight
        );

        // --- Progression ---
        DrawSectionHeader(options, "MSS_FP_Settings_Section_HauntProgression".Translate(), ref scrollViewHeight);

        Settings.HauntProgressionSpeedMultiplier = options.SliderLabeled(
            "MSS_FP_Settings_HauntProgressionSpeed".Translate(
                Settings.HauntProgressionSpeedMultiplier.ToStringPercent()
            ),
            Settings.HauntProgressionSpeedMultiplier,
            0.25f,
            4.0f,
            tooltip: "MSS_FP_Settings_HauntProgressionSpeed_Tooltip".Translate()
        );
        scrollViewHeight += 30f;

        Settings.HauntRegressionSpeedMultiplier = options.SliderLabeled(
            "MSS_FP_Settings_HauntRegressionSpeed".Translate(
                Settings.HauntRegressionSpeedMultiplier.ToStringPercent()
            ),
            Settings.HauntRegressionSpeedMultiplier,
            0.0f,
            4.0f,
            tooltip: "MSS_FP_Settings_HauntRegressionSpeed_Tooltip".Translate()
        );
        scrollViewHeight += 30f;

        // --- Kill Haunts ---
        DrawSectionHeader(options, "MSS_FP_Settings_Section_KillHaunts".Translate(), ref scrollViewHeight);

        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnableKillHaunts".Translate(),
            ref Settings.EnableKillHaunts,
            ref scrollViewHeight
        );

        Settings.KillHauntBaseChance = options.SliderLabeled(
            "MSS_FP_Settings_KillHauntBaseChance".Translate(
                Settings.KillHauntBaseChance.ToStringPercent()
            ),
            Settings.KillHauntBaseChance,
            0.01f,
            0.5f,
            tooltip: "MSS_FP_Settings_KillHauntBaseChance_Tooltip".Translate()
        );
        scrollViewHeight += 30f;

        DrawIntAdjuster(
            options,
            "MSS_FP_Settings_KillHauntCooldownDays".Translate(Settings.KillHauntCooldownTicks / 60000),
            ref Settings.KillHauntCooldownTicks,
            60000,
            0,
            ref scrollViewHeight
        );
        DrawIntAdjuster(
            options,
            "MSS_FP_Settings_MaxBadHauntsPerPawn".Translate(Settings.MaxBadHauntsPerPawn),
            ref Settings.MaxBadHauntsPerPawn,
            1,
            1,
            ref scrollViewHeight
        );

        // --- Poltergeist ---
        DrawSectionHeader(options, "MSS_FP_Settings_Section_Poltergeist".Translate(), ref scrollViewHeight);

        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnablePoltergeistEvents".Translate(),
            ref Settings.EnablePoltergeistEvents,
            ref scrollViewHeight
        );

        Settings.PoltergeistIntensityMultiplier = options.SliderLabeled(
            "MSS_FP_Settings_PoltergeistIntensityMultiplier".Translate(
                Settings.PoltergeistIntensityMultiplier.ToStringPercent()
            ),
            Settings.PoltergeistIntensityMultiplier,
            0.5f,
            2.0f,
            tooltip: "MSS_FP_Settings_PoltergeistIntensityMultiplier_Tooltip".Translate()
        );
        scrollViewHeight += 30f;

        Settings.PoltergeistEventThreshold = options.SliderLabeled(
            "MSS_FP_Settings_PoltergeistEventThreshold".Translate(
                Settings.PoltergeistEventThreshold.ToString("F2")
            ),
            Settings.PoltergeistEventThreshold,
            0.25f,
            2.0f,
            tooltip: "MSS_FP_Settings_PoltergeistEventThreshold_Tooltip".Translate()
        );
        scrollViewHeight += 30f;

        // --- Proximity & Cooldowns ---
        DrawSectionHeader(options, "MSS_FP_Settings_Section_HauntCooldowns".Translate(), ref scrollViewHeight);

        DrawIntAdjuster(
            options,
            "MSS_FP_Settings_HauntProximityRadius".Translate(Settings.HauntProximityRadius),
            ref Settings.HauntProximityRadius,
            1,
            1,
            ref scrollViewHeight
        );
        DrawIntAdjuster(
            options,
            "MSS_FP_Settings_HauntMinCooldownDays".Translate(Settings.HauntMinCooldownDays),
            ref Settings.HauntMinCooldownDays,
            1,
            1,
            ref scrollViewHeight
        );
        DrawIntAdjuster(
            options,
            "MSS_FP_Settings_HauntPostFireCooldownDays".Translate(Settings.HauntPostFireCooldownDays),
            ref Settings.HauntPostFireCooldownDays,
            1,
            1,
            ref scrollViewHeight
        );
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref Settings.ShowHaunts, "ShowHaunts", false);
        Scribe_Values.Look(ref Settings.EnableEcho, "EnableEcho", false);
        Scribe_Values.Look(ref Settings.EnableGraveHaunts, "EnableGraveHaunts", false);
        Scribe_Values.Look(ref Settings.ShowHauntDevDashboard, "ShowHauntDevDashboard", true);
        Scribe_Values.Look(ref Settings.HauntProgressionSpeedMultiplier, "HauntProgressionSpeedMultiplier", 1.0f);
        Scribe_Values.Look(ref Settings.HauntRegressionSpeedMultiplier, "HauntRegressionSpeedMultiplier", 1.0f);
        Scribe_Values.Look(ref Settings.AlwaysShowNamedHaunts, "AlwaysShowNamedHaunts", false);
        Scribe_Values.Look(ref Settings.EnablePoltergeistEvents, "EnablePoltergeistEvents", false);
        Scribe_Values.Look(ref Settings.PoltergeistIntensityMultiplier, "PoltergeistIntensityMultiplier", 1.0f);
        Scribe_Values.Look(ref Settings.PoltergeistEventThreshold, "PoltergeistEventThreshold", 0.5f);
        Scribe_Values.Look(ref Settings.HauntProximityRadius, "HauntProximityRadius", 50);
        Scribe_Values.Look(ref Settings.HauntMinCooldownDays, "HauntMinCooldownDays", 2);
        Scribe_Values.Look(ref Settings.HauntPostFireCooldownDays, "HauntPostFireCooldownDays", 4);
        Scribe_Values.Look(ref Settings.EnableKillHaunts, "EnableKillHaunts", true);
        Scribe_Values.Look(ref Settings.KillHauntBaseChance, "KillHauntBaseChance", 0.15f);
        Scribe_Values.Look(ref Settings.KillHauntCooldownTicks, "KillHauntCooldownTicks", 60000);
        Scribe_Values.Look(ref Settings.MaxBadHauntsPerPawn, "MaxBadHauntsPerPawn", 5);
    }
}
