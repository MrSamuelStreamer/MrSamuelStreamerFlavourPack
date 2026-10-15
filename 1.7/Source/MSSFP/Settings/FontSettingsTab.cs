using MSSFP.Fonts;
using MSSFP.Utils;
using UnityEngine;
using Verse;

namespace MSSFP.SettingsTabs;

/// <summary>
/// Settings UI for the Cadman UI font swap (default disabled).
/// Auto-discovered by <see cref="MSSFP.Settings"/>'s reflection tab loader.
/// The persisted field lives on <see cref="MSSFP.Settings.UseCadmanFont"/>.
/// </summary>
public class FontSettingsTab : SettingsTab
{
    public FontSettingsTab(ModSettings settings, Mod mod) : base(settings, mod) { }

    public override string TabName => "MSS_FP_Settings_Tab_Font".Translate();
    public override int TabOrder => 85;

    public override void DoTabContents(
        Listing_Standard options,
        Rect scrollViewRect,
        ref float scrollViewHeight)
    {
        MSSFP.Settings s = settings as MSSFP.Settings;
        if (s == null) return;

        if (!CadmanFont.Available)
        {
            scrollViewHeight += options.Label("MSS_FP_Settings_CadmanFont_Unavailable".Translate()).height;
            return;
        }

        bool prev = s.UseCadmanFont;
        DrawCheckBox(options, "MSS_FP_Settings_UseCadmanFont".Translate(), ref s.UseCadmanFont, ref scrollViewHeight);
        if (s.UseCadmanFont != prev)
            CadmanFont.SetEnabled(s.UseCadmanFont);

        scrollViewHeight += options.Label("MSS_FP_Settings_UseCadmanFont_Note".Translate()).height;
    }
}
