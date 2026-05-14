using MSSFP.Holo;
using MSSFP.Utils;
using UnityEngine;
using Verse;

namespace MSSFP.SettingsTabs;

/// <summary>
/// Settings UI for the HoloMono shader FX. Auto-discovered by
/// <see cref="MSSFP.Settings"/>'s reflection-based tab loader.
///
/// Toggles map onto global shader keywords (_OUTLINE_ON / _GLOW_ON) via
/// <see cref="HoloShaders.ApplyKeywordsFromSettings"/>, queued as a PostSaveAction
/// so closing the settings window applies them without a restart.
/// </summary>
public class HoloSettingsTab : SettingsTab
{
    public HoloSettingsTab(ModSettings settings, Mod mod) : base(settings, mod) { }

    public override string TabName => "MSSFP_Settings_Section_Holo".Translate();
    public override int TabOrder => 80;

    public override void DoTabContents(
        Listing_Standard options,
        Rect scrollViewRect,
        ref float scrollViewHeight)
    {
        MSSFP.Settings s = settings as MSSFP.Settings;
        if (s == null) return;

        bool prevOutline = s.EnableHoloOutline;
        bool prevGlow = s.EnableHoloGlow;

        DrawCheckBox(
            options,
            "MSSFP_Settings_EnableHoloOutline".Translate(),
            ref s.EnableHoloOutline,
            ref scrollViewHeight);

        DrawCheckBox(
            options,
            "MSSFP_Settings_EnableHoloGlow".Translate(),
            ref s.EnableHoloGlow,
            ref scrollViewHeight);

        // Loaded state indicator — surfaces bundle load failures so users see
        // why toggles aren't taking effect.
        options.GapLine();
        scrollViewHeight += 12f;
        string statusKey = HoloShaders.IsLoaded
            ? "MSSFP_Settings_HoloShader_Loaded"
            : "MSSFP_Settings_HoloShader_Fallback";
        options.Label(statusKey.Translate());
        scrollViewHeight += 24f + options.verticalSpacing;

        // Queue keyword apply for after save. Same instance is idempotent so a
        // duplicate enqueue would be safe, but only enqueue on change to keep
        // PostSaveActions short.
        if (prevOutline != s.EnableHoloOutline || prevGlow != s.EnableHoloGlow)
        {
            PostSaveActions.Add(HoloShaders.ApplyKeywordsFromSettings);
        }
    }
}
