using MSSFP.Dialogs;
using MSSFP.Fonts;
using MSSFP.Utils;
using UnityEngine;
using Verse;

namespace MSSFP.SettingsTabs;

/// <summary>
/// Settings UI for the UI font swap: shows the chosen font and opens the font picker.
/// Auto-discovered by <see cref="MSSFP.Settings"/>'s reflection tab loader.
/// The persisted choice lives on <see cref="MSSFP.Settings.UiFontId"/>.
/// </summary>
public class FontSettingsTab : SettingsTab
{
    private const float ButtonHeight = 30f;

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

        if (!UiFont.Available)
        {
            scrollViewHeight += options.Label("MSS_FP_Settings_UiFont_Unavailable".Translate()).height;
            return;
        }

        string chosen = s.UiFontId;
        string name = UiFont.DisplayName(chosen);
        string current = chosen == UiFont.Current
            ? "MSS_FP_Settings_UiFont_Current".Translate(name)
            : "MSS_FP_Settings_UiFont_CurrentMissing".Translate(name);
        scrollViewHeight += options.Label(current).height;

        if (options.ButtonText("MSS_FP_Settings_UiFont_Choose".Translate()))
            Find.WindowStack.Add(new Dialog_PickUiFont());
        scrollViewHeight += ButtonHeight + options.verticalSpacing;

        if (options.ButtonText("MSS_FP_Settings_UiFont_Reset".Translate()))
        {
            UiFont.Apply(UiFont.VanillaId);
            s.UiFontId = UiFont.VanillaId;
            s.Write();
        }
        scrollViewHeight += ButtonHeight + options.verticalSpacing;

        scrollViewHeight += options.Label("MSS_FP_Settings_UiFont_Note".Translate()).height;
    }
}
