using MSSFP.Dialogs;
using MSSFP.Fonts;
using UnityEngine;
using Verse;

namespace MSSFP.SettingsTabs;

/// <summary>
/// Font picker drawn inside <see cref="UiSettingsTab"/>.
/// The persisted choice lives on <see cref="MSSFP.Settings.UiFontId"/>.
/// </summary>
public class FontSettingsSection
{
    private const float ButtonHeight = 30f;

    public void Draw(MSSFP.Settings settings, Listing_Standard options, ref float scrollViewHeight)
    {
        if (!UiFont.Available)
        {
            scrollViewHeight += options.Label("MSS_FP_Settings_UiFont_Unavailable".Translate()).height;
            return;
        }

        string chosen = settings.UiFontId;
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
            settings.UiFontId = UiFont.VanillaId;
            settings.Write();
        }
        scrollViewHeight += ButtonHeight + options.verticalSpacing;

        scrollViewHeight += options.Label("MSS_FP_Settings_UiFont_Note".Translate()).height;
    }
}
