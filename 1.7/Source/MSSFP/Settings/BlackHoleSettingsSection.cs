using MSSFP.Comps.World;
using MSSFP.Utils;
using UnityEngine;
using Verse;

namespace MSSFP.SettingsTabs;

/// <summary>
/// Black Hole controls drawn inside <see cref="WorldSettingsTab"/>.
/// Persisted fields live on <see cref="MSSFP.Settings"/>.
/// </summary>
public class BlackHoleSettingsSection
{
    public void Draw(SettingsTab host, MSSFP.Settings settings, Listing_Standard options, ref float scrollViewHeight)
    {
        bool prevEnabled = settings.BlackHoleEnabled;
        host.DrawCheckBox(options, "Enable black hole (replaces the home star with a black hole)", ref settings.BlackHoleEnabled, ref scrollViewHeight);

        if (settings.BlackHoleEnabled != prevEnabled)
        {
            // Sync the world condition and the sun-hide patch to match the new state.
            // Deactivation must end the GameCondition BEFORE unpatching so the sun
            // isn't drawn on top of a still-attached render helper for one frame.
            if (settings.BlackHoleEnabled)
            {
                MSSFPMod.ToggleBlackHoleSunPatch(true);
                WorldComponent_MSSFPBlackHole.SyncActiveStateWithSetting();
            }
            else
            {
                WorldComponent_MSSFPBlackHole.SyncActiveStateWithSetting();
                MSSFPMod.ToggleBlackHoleSunPatch(false);
            }
            SkyBackgroundFix.Apply(settings.BlackHoleEnabled);
        }

        if (settings.BlackHoleEnabled)
        {
            options.GapLine();
            scrollViewHeight += 12f;

            settings.BlackHoleLightCap = options.SliderLabeled(
                $"Sunlight cap: {settings.BlackHoleLightCap.ToStringPercent()}",
                settings.BlackHoleLightCap, 0.05f, 1f);
            scrollViewHeight += 30f;

            host.DrawCheckBox(options, "Enable black hole growth over time", ref settings.BlackHoleGrowthEnabled, ref scrollViewHeight);

            if (settings.BlackHoleGrowthEnabled)
            {
                settings.BlackHoleGrowthRate = options.SliderLabeled(
                    $"Growth rate (area doublings per year): {settings.BlackHoleGrowthRate:0.##}",
                    settings.BlackHoleGrowthRate, 0f, 5f);
                scrollViewHeight += 30f;

                settings.BlackHoleGrowthMax = options.SliderLabeled(
                    $"Growth max (radius multiplier cap): {settings.BlackHoleGrowthMax:0.#}",
                    settings.BlackHoleGrowthMax, 1f, 32f);
                scrollViewHeight += 30f;
            }
        }

        options.GapLine();
        scrollViewHeight += 12f;

        bool prevSunScale = settings.SunSizeScaleEnabled;
        host.DrawCheckBox(options, "Enable sun size growth over time (independent of black hole)", ref settings.SunSizeScaleEnabled, ref scrollViewHeight);

        if (settings.SunSizeScaleEnabled != prevSunScale)
            MSSFPMod.ToggleSunSizeScalePatch(settings.SunSizeScaleEnabled);

        if (settings.SunSizeScaleEnabled)
        {
            settings.SunSizeScaleRate = options.SliderLabeled(
                $"Sun growth rate (area doublings per year): {settings.SunSizeScaleRate:0.##}",
                settings.SunSizeScaleRate, 0f, 5f);
            scrollViewHeight += 30f;

            settings.SunSizeScaleMax = options.SliderLabeled(
                $"Sun growth max (radius multiplier cap): {settings.SunSizeScaleMax:0.#}",
                settings.SunSizeScaleMax, 1f, 32f);
            scrollViewHeight += 30f;
        }
    }
}
