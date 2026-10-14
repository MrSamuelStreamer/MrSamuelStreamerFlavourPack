using MSSFP.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.SettingsTabs;

/// <summary>
/// Settings UI for the Golden Cube Implant feature. Auto-discovered by
/// <see cref="MSSFP.Settings"/>'s reflection-based tab loader.
/// All persisted fields live on <see cref="MSSFP.Settings"/> directly so they
/// survive removal of this assembly — this tab is render-only.
/// </summary>
public class GoldenCubeSettingsTab : SettingsTab
{
    public GoldenCubeSettingsTab(ModSettings settings, Mod mod) : base(settings, mod) { }

    public override string TabName => "Golden Cube";
    public override int TabOrder => 90;

    public override void DoTabContents(
        Listing_Standard options,
        Rect scrollViewRect,
        ref float scrollViewHeight)
    {
        MSSFP.Settings s = settings as MSSFP.Settings;
        if (s == null) return;

        if (!ModsConfig.AnomalyActive)
        {
            options.Label("MSSFP_Settings_GoldenCube_RequiresAnomaly".Translate());
            scrollViewHeight += 30f;
            return;
        }

        DrawCheckBox(
            options,
            "MSSFP_Settings_EnableGoldenCubeImplant".Translate(),
            ref s.EnableGoldenCubeImplant,
            ref scrollViewHeight);

        if (!s.EnableGoldenCubeImplant) return;

        options.GapLine();
        scrollViewHeight += 12f;

        s.GoldenCubeImplantRaidChance = options.SliderLabeled(
            "MSSFP_Settings_GoldenCubeImplantRaidChance".Translate(
                s.GoldenCubeImplantRaidChance.ToStringPercent("F1")),
            s.GoldenCubeImplantRaidChance, 0f, 0.25f);
        scrollViewHeight += 30f;

        s.GoldenCubeTransferChance = options.SliderLabeled(
            "MSSFP_Settings_GoldenCubeTransferChance".Translate(
                s.GoldenCubeTransferChance.ToStringPercent("F0")),
            s.GoldenCubeTransferChance, 0f, 1f);
        scrollViewHeight += 30f;
    }
}
