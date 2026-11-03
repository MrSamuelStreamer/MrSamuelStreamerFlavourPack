using MSSFP.Utils;
using UnityEngine;
using Verse;

namespace MSSFP.SettingsTabs;

/// <summary>Black hole and tunnel settings.</summary>
public class WorldSettingsTab : SettingsTab
{
    private readonly BlackHoleSettingsSection blackHole = new();
    private readonly TunnelsSettingsSection tunnels = new();

    public WorldSettingsTab(ModSettings settings, Mod mod) : base(settings, mod) { }

    public override string TabName => "MSS_FP_Settings_Tab_World".Translate();
    public override int TabOrder => 90;

    public override void DoTabContents(
        Listing_Standard options,
        Rect scrollViewRect,
        ref float scrollViewHeight)
    {
        MSSFP.Settings s = settings as MSSFP.Settings;
        if (s == null) return;

        DrawSectionHeader(options, "MSS_FP_Settings_Section_BlackHole".Translate(), ref scrollViewHeight);
        blackHole.Draw(this, s, options, ref scrollViewHeight);

        DrawSectionHeader(options, "MSS_FP_Settings_Section_Tunnels".Translate(), ref scrollViewHeight);
        tunnels.Draw(this, s, options, ref scrollViewHeight);
    }
}
