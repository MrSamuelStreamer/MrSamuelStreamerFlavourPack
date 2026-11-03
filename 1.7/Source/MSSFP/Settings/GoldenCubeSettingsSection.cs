using MSSFP.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.SettingsTabs;

/// <summary>
/// Golden Cube controls drawn inside <see cref="MSSFP.Utils.MemesSettingsTab"/>.
/// Persisted fields live on <see cref="MSSFP.Settings"/>.
/// </summary>
public class GoldenCubeSettingsSection
{
    public void Draw(SettingsTab host, MSSFP.Settings settings, Listing_Standard options, ref float scrollViewHeight)
    {
        if (!ModsConfig.AnomalyActive)
        {
            options.Label("MSSFP_Settings_GoldenCube_RequiresAnomaly".Translate());
            scrollViewHeight += 30f;
            return;
        }

        host.DrawCheckBox(
            options,
            "MSSFP_Settings_EnableGoldenCubeImplant".Translate(),
            ref settings.EnableGoldenCubeImplant,
            ref scrollViewHeight);

        if (!settings.EnableGoldenCubeImplant) return;

        options.GapLine();
        scrollViewHeight += 12f;

        settings.GoldenCubeImplantRaidChance = options.SliderLabeled(
            "MSSFP_Settings_GoldenCubeImplantRaidChance".Translate(
                settings.GoldenCubeImplantRaidChance.ToStringPercent("F1")),
            settings.GoldenCubeImplantRaidChance, 0f, 0.25f);
        scrollViewHeight += 30f;

        settings.GoldenCubeTransferChance = options.SliderLabeled(
            "MSSFP_Settings_GoldenCubeTransferChance".Translate(
                settings.GoldenCubeTransferChance.ToStringPercent("F0")),
            settings.GoldenCubeTransferChance, 0f, 1f);
        scrollViewHeight += 30f;
    }
}
