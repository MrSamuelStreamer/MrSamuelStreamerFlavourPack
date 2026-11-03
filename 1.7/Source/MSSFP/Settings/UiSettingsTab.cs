using MSSFP.LabelIcons;
using MSSFP.Utils;
using UnityEngine;
using Verse;

namespace MSSFP.SettingsTabs;

/// <summary>Font and label-icon settings.</summary>
public class UiSettingsTab : SettingsTab
{
    public readonly LabelIconsSettingsSection LabelIcons = new();
    private readonly FontSettingsSection font = new();

    public UiSettingsTab(ModSettings settings, Mod mod) : base(settings, mod) { }

    public override string TabName => "MSS_FP_Settings_Tab_UI".Translate();
    public override int TabOrder => 81;

    public override void DoTabContents(
        Listing_Standard options,
        Rect scrollViewRect,
        ref float scrollViewHeight)
    {
        MSSFP.Settings s = settings as MSSFP.Settings;
        if (s == null) return;

        DrawSectionHeader(options, "MSS_FP_Settings_Tab_Font".Translate(), ref scrollViewHeight);
        font.Draw(s, options, ref scrollViewHeight);

        DrawSectionHeader(options, "MSS_FP_Settings_Section_LabelIcons".Translate(), ref scrollViewHeight);
        LabelIcons.Draw(this, options, ref scrollViewHeight);
    }

    public override void ExposeData()
    {
        LabelIcons.ExposeData();
    }
}
