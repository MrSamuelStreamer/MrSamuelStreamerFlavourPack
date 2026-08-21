using MSSFP.Tunnels;
using MSSFP.Utils;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.SettingsTabs;

/// <summary>
/// Settings UI for the tunnel system (default disabled). Auto-discovered by
/// <see cref="MSSFP.Settings"/>'s reflection-based tab loader.
/// All persisted fields live on <see cref="MSSFP.Settings"/> directly.
/// </summary>
public class TunnelsSettingsTab : SettingsTab
{
    public TunnelsSettingsTab(ModSettings settings, Mod mod) : base(settings, mod) { }

    public override string TabName => "Tunnels";
    public override int TabOrder => 90;

    public override void DoTabContents(
        Listing_Standard options,
        Rect scrollViewRect,
        ref float scrollViewHeight)
    {
        MSSFP.Settings s = settings as MSSFP.Settings;
        if (s == null) return;

        options.CheckboxLabeled(
            "MSSFP_Tunnels_Settings_EnableCheckbox".Translate(),
            ref s.TunnelsEnabledForNewWorlds,
            "MSSFP_Tunnels_Settings_EnableTooltip".Translate());
        scrollViewHeight += 24f + options.verticalSpacing;

        options.GapLine();
        scrollViewHeight += 12f;

        World world = Find.World;
        TunnelGenData genData = world?.GetComponent<TunnelGenData>();
        string statusText = genData == null
            ? "MSSFP_Tunnels_Settings_StatusNoWorld".Translate()
            : genData.tunnelsEnabledForSave
                ? "MSSFP_Tunnels_Settings_StatusEnabled".Translate()
                : "MSSFP_Tunnels_Settings_StatusDisabled".Translate();
        options.Label(statusText);
        scrollViewHeight += Text.CalcHeight(statusText, options.ColumnWidth) + options.verticalSpacing;

        // TODO(commit 5): travel speed sliders (TunnelDefaultTilesPerHour, TunnelResearchedTilesPerHour)
        // TODO(commit 5): RubyVeinSpawnChance slider
        // TODO(commit 5): TunnelIncidentWeightMultiplier slider
        // TODO(commit 5): AllowCombatTunnelIncidents checkbox
        // TODO(commit 5): DisableTunnelIncidents checkbox
        // TODO(commit 5): QuestSiteRadiusStep / QuestSiteMinRadiusStep sliders
    }
}
