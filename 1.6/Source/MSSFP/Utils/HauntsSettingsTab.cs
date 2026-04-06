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
        Scribe_Values.Look(ref Settings.HauntProximityRadius, "HauntProximityRadius", 10);
        Scribe_Values.Look(ref Settings.HauntMinCooldownDays, "HauntMinCooldownDays", 2);
        Scribe_Values.Look(ref Settings.HauntPostFireCooldownDays, "HauntPostFireCooldownDays", 4);
    }
}
