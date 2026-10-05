using System.Collections.Generic;
using MSSFP.Utils;
using UnityEngine;
using Verse;

namespace MSSFP.LabelIcons;

public class LabelIconsSettingsTab : SettingsTab
{
    // Icon id -> enabled. A missing id means enabled, so new icons default on.
    public Dictionary<string, bool> Enabled = new();

    public LabelIconsSettingsTab(ModSettings settings, Mod mod)
        : base(settings, mod)
    {
    }

    public override string TabName => "Label Icons";
    public override int TabOrder => 81;

    public bool IsEnabled(string id) => !Enabled.TryGetValue(id, out bool on) || on;

    public override void DoTabContents(
        Listing_Standard options,
        Rect scrollViewRect,
        ref float scrollViewHeight)
    {
        options.Label("MSS_FP_Settings_LabelIcons_Description".Translate());
        scrollViewHeight += 24f;

        foreach (LabelIcon icon in LabelIcons.All)
        {
            bool on = IsEnabled(icon.Id);
            DrawCheckBox(options, icon.LabelKey.Translate(), ref on, ref scrollViewHeight);
            Enabled[icon.Id] = on;
        }
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref Enabled, "LabelIconsEnabled", LookMode.Value, LookMode.Value);
        Enabled ??= new Dictionary<string, bool>();
    }
}

/// <summary>Static accessor; caches the tab because it is read per icon per label per frame.</summary>
public static class LabelIconsSettings
{
    private static LabelIconsSettingsTab tab;

    public static bool IsEnabled(string id)
    {
        tab ??= MSSFPMod.settings?.GetSettings<LabelIconsSettingsTab>();
        return tab?.IsEnabled(id) ?? true;
    }
}
