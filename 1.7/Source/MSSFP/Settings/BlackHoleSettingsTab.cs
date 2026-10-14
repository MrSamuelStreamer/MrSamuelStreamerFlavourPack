using MSSFP.Comps.World;
using MSSFP.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.SettingsTabs;

/// <summary>
/// Settings UI for the Black Hole cosmetic feature (default disabled).
/// Auto-discovered by <see cref="MSSFP.Settings"/>'s reflection tab loader.
/// All persisted fields live on <see cref="MSSFP.Settings"/> directly.
/// </summary>
public class BlackHoleSettingsTab : SettingsTab
{
    public BlackHoleSettingsTab(ModSettings settings, Mod mod) : base(settings, mod) { }

    public override string TabName => "Black Hole";
    public override int TabOrder => 95;

    public override void DoTabContents(
        Listing_Standard options,
        Rect scrollViewRect,
        ref float scrollViewHeight)
    {
        MSSFP.Settings s = settings as MSSFP.Settings;
        if (s == null) return;

        bool prevEnabled = s.BlackHoleEnabled;
        DrawCheckBox(options, "Enable black hole (replaces the home star with a black hole)", ref s.BlackHoleEnabled, ref scrollViewHeight);

        if (s.BlackHoleEnabled != prevEnabled)
        {
            // Sync the world condition and the sun-hide patch to match the new state.
            // Deactivation must end the GameCondition BEFORE unpatching so the sun
            // isn't drawn on top of a still-attached render helper for one frame.
            if (s.BlackHoleEnabled)
            {
                MSSFPMod.ToggleBlackHoleSunPatch(true);
                WorldComponent_MSSFPBlackHole.SyncActiveStateWithSetting();
            }
            else
            {
                WorldComponent_MSSFPBlackHole.SyncActiveStateWithSetting();
                MSSFPMod.ToggleBlackHoleSunPatch(false);
            }
            SkyBackgroundFix.Apply(s.BlackHoleEnabled);
        }

        if (s.BlackHoleEnabled)
        {
            options.GapLine();
            scrollViewHeight += 12f;

            s.BlackHoleLightCap = options.SliderLabeled(
                $"Sunlight cap: {s.BlackHoleLightCap.ToStringPercent()}",
                s.BlackHoleLightCap, 0.05f, 1f);
            scrollViewHeight += 30f;

            DrawCheckBox(options, "Enable black hole growth over time", ref s.BlackHoleGrowthEnabled, ref scrollViewHeight);

            if (s.BlackHoleGrowthEnabled)
            {
                s.BlackHoleGrowthRate = options.SliderLabeled(
                    $"Growth rate (area doublings per year): {s.BlackHoleGrowthRate:0.##}",
                    s.BlackHoleGrowthRate, 0f, 5f);
                scrollViewHeight += 30f;

                s.BlackHoleGrowthMax = options.SliderLabeled(
                    $"Growth max (radius multiplier cap): {s.BlackHoleGrowthMax:0.#}",
                    s.BlackHoleGrowthMax, 1f, 32f);
                scrollViewHeight += 30f;
            }
        }

        options.GapLine();
        scrollViewHeight += 12f;

        bool prevSunScale = s.SunSizeScaleEnabled;
        DrawCheckBox(options, "Enable sun size growth over time (independent of black hole)", ref s.SunSizeScaleEnabled, ref scrollViewHeight);

        if (s.SunSizeScaleEnabled != prevSunScale)
            MSSFPMod.ToggleSunSizeScalePatch(s.SunSizeScaleEnabled);

        if (s.SunSizeScaleEnabled)
        {
            s.SunSizeScaleRate = options.SliderLabeled(
                $"Sun growth rate (area doublings per year): {s.SunSizeScaleRate:0.##}",
                s.SunSizeScaleRate, 0f, 5f);
            scrollViewHeight += 30f;

            s.SunSizeScaleMax = options.SliderLabeled(
                $"Sun growth max (radius multiplier cap): {s.SunSizeScaleMax:0.#}",
                s.SunSizeScaleMax, 1f, 32f);
            scrollViewHeight += 30f;
        }
    }
}
