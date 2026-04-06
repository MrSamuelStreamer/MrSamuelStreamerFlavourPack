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
        DrawCheckBox(
            options,
            "MSS_FP_Settings_ShowHaunts".Translate(),
            ref Settings.ShowHaunts,
            ref scrollViewHeight
        );
        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnablePossession".Translate(),
            ref Settings.EnablePossession,
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

        options.Label(
            "MSS_FP_Settings_HauntProgressionSpeed".Translate(
                Settings.HauntProgressionSpeedMultiplier.ToStringPercent()
            ),
            tooltip: "MSS_FP_Settings_HauntProgressionSpeed_Tooltip".Translate()
        );
        Settings.HauntProgressionSpeedMultiplier = options.SliderLabeled(
            Settings.HauntProgressionSpeedMultiplier.ToStringPercent(),
            Settings.HauntProgressionSpeedMultiplier,
            0.25f,
            4.0f
        );
        scrollViewHeight += 48f;

        options.Label(
            "MSS_FP_Settings_HauntRegressionSpeed".Translate(
                Settings.HauntRegressionSpeedMultiplier.ToStringPercent()
            ),
            tooltip: "MSS_FP_Settings_HauntRegressionSpeed_Tooltip".Translate()
        );
        Settings.HauntRegressionSpeedMultiplier = options.SliderLabeled(
            Settings.HauntRegressionSpeedMultiplier.ToStringPercent(),
            Settings.HauntRegressionSpeedMultiplier,
            0.0f,
            4.0f
        );
        scrollViewHeight += 48f;

        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnablePoltergeistEvents".Translate(),
            ref Settings.EnablePoltergeistEvents,
            ref scrollViewHeight
        );

        options.Label(
            "MSS_FP_Settings_PoltergeistIntensityMultiplier".Translate(
                Settings.PoltergeistIntensityMultiplier.ToStringPercent()
            ),
            tooltip: "MSS_FP_Settings_PoltergeistIntensityMultiplier_Tooltip".Translate()
        );
        Settings.PoltergeistIntensityMultiplier = options.SliderLabeled(
            Settings.PoltergeistIntensityMultiplier.ToStringPercent(),
            Settings.PoltergeistIntensityMultiplier,
            0.5f,
            2.0f
        );
        scrollViewHeight += 48f;

        options.Label(
            "MSS_FP_Settings_PoltergeistEventThreshold".Translate(
                Settings.PoltergeistEventThreshold.ToString("F2")
            ),
            tooltip: "MSS_FP_Settings_PoltergeistEventThreshold_Tooltip".Translate()
        );
        Settings.PoltergeistEventThreshold = options.SliderLabeled(
            Settings.PoltergeistEventThreshold.ToString("F2"),
            Settings.PoltergeistEventThreshold,
            0.25f,
            2.0f
        );
        scrollViewHeight += 48f;

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
        Scribe_Values.Look(ref Settings.EnablePossession, "EnablePossession", false);
        Scribe_Values.Look(ref Settings.ShowHauntDevDashboard, "ShowHauntDevDashboard", true);
        Scribe_Values.Look(ref Settings.HauntProgressionSpeedMultiplier, "HauntProgressionSpeedMultiplier", 1.0f);
        Scribe_Values.Look(ref Settings.HauntRegressionSpeedMultiplier, "HauntRegressionSpeedMultiplier", 1.0f);
        Scribe_Values.Look(ref Settings.AlwaysShowNamedHaunts, "AlwaysShowNamedHaunts", false);
        Scribe_Values.Look(ref Settings.EnablePoltergeistEvents, "EnablePoltergeistEvents", false);
        Scribe_Values.Look(ref Settings.PoltergeistIntensityMultiplier, "PoltergeistIntensityMultiplier", 1.0f);
        Scribe_Values.Look(ref Settings.PoltergeistEventThreshold, "PoltergeistEventThreshold", 0.5f);
        Scribe_Values.Look(ref Settings.HauntProximityRadius, "HauntProximityRadius", 10);
        Scribe_Values.Look(ref Settings.HauntMinCooldownDays, "HauntMinCooldownDays", 2);
        Scribe_Values.Look(ref Settings.HauntPostFireCooldownDays, "HauntPostFireCooldownDays", 4);
    }
}
