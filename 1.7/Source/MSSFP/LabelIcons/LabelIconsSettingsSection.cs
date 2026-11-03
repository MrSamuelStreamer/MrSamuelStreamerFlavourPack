using System.Collections.Generic;
using MSSFP.Utils;
using UnityEngine;
using Verse;

namespace MSSFP.LabelIcons;

/// <summary>
/// Label-icon toggles drawn inside <see cref="MSSFP.SettingsTabs.UiSettingsTab"/>.
/// This is not a settings tab. <see cref="UiSettingsTab"/> owns the instance and saves it.
/// </summary>
public class LabelIconsSettingsSection
{
    // Icon id -> enabled. A missing id means enabled, so new icons default on.
    public Dictionary<string, bool> Enabled = new();

    public bool IsEnabled(string id) => !Enabled.TryGetValue(id, out bool on) || on;

    public void Draw(SettingsTab host, Listing_Standard options, ref float scrollViewHeight)
    {
        options.Label("MSS_FP_Settings_LabelIcons_Description".Translate());
        scrollViewHeight += 24f;

        foreach (LabelIcon icon in LabelIcons.All)
        {
            bool on = IsEnabled(icon.Id);
            host.DrawCheckBox(options, icon.LabelKey.Translate(), ref on, ref scrollViewHeight);
            Enabled[icon.Id] = on;
        }
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref Enabled, "LabelIconsEnabled", LookMode.Value, LookMode.Value);
        Enabled ??= new Dictionary<string, bool>();
    }
}

/// <summary>Static accessor; caches the section because it is read per icon per label per frame.</summary>
public static class LabelIconsSettings
{
    private static LabelIconsSettingsSection section;

    public static bool IsEnabled(string id)
    {
        section ??= MSSFPMod.settings?.GetSettings<MSSFP.SettingsTabs.UiSettingsTab>()?.LabelIcons;
        return section?.IsEnabled(id) ?? true;
    }
}
