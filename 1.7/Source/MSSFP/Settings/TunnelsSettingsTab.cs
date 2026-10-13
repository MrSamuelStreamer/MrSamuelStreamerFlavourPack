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

        // ── Travel speed ─────────────────────────────────────────────────
        DrawSectionHeader(options, "MSSFP_Tunnels_Settings_Section_TravelSpeed".Translate(), ref scrollViewHeight);

        s.TunnelDefaultTilesPerHour = RoundToStep(options.SliderLabeled(
            "MSSFP_Tunnels_Settings_DefaultTilesPerHour".Translate(s.TunnelDefaultTilesPerHour.ToString("0.#")),
            s.TunnelDefaultTilesPerHour, 1f, 20f), 0.5f);
        scrollViewHeight += 30f;

        s.TunnelResearchedTilesPerHour = RoundToStep(options.SliderLabeled(
            "MSSFP_Tunnels_Settings_ResearchedTilesPerHour".Translate(s.TunnelResearchedTilesPerHour.ToString("0.#")),
            s.TunnelResearchedTilesPerHour, 3f, 30f), 0.5f);
        scrollViewHeight += 30f;

        // ── Map generation ───────────────────────────────────────────────
        DrawSectionHeader(options, "MSSFP_Tunnels_Settings_Section_MapGeneration".Translate(), ref scrollViewHeight);

        s.RubyVeinSpawnChance = RoundToStep(options.SliderLabeled(
            "MSSFP_Tunnels_Settings_RubyVeinSpawnChance".Translate(s.RubyVeinSpawnChance.ToStringPercent("F1")),
            s.RubyVeinSpawnChance, 0f, 0.05f), 0.001f);
        scrollViewHeight += 30f;

        // ── Incidents ────────────────────────────────────────────────────
        DrawSectionHeader(options, "MSSFP_Tunnels_Settings_Section_Incidents".Translate(), ref scrollViewHeight);

        DrawCheckBox(
            options,
            "MSSFP_Tunnels_Settings_DisableTunnelIncidents".Translate(),
            ref s.DisableTunnelIncidents,
            ref scrollViewHeight);

        if (!s.DisableTunnelIncidents)
        {
            // Floored at 0.05 rather than 0 to avoid Storyteller zero-weight ambiguity
            // (a zero-weight incident category can behave inconsistently with some
            // storyteller implementations rather than cleanly "never firing").
            s.TunnelIncidentWeightMultiplier = RoundToStep(options.SliderLabeled(
                "MSSFP_Tunnels_Settings_IncidentWeightMultiplier".Translate(s.TunnelIncidentWeightMultiplier.ToString("0.00")),
                s.TunnelIncidentWeightMultiplier, 0.05f, 2f), 0.05f);
            scrollViewHeight += 30f;

            DrawCheckBox(
                options,
                "MSSFP_Tunnels_Settings_AllowCombatTunnelIncidents".Translate(),
                ref s.AllowCombatTunnelIncidents,
                ref scrollViewHeight);
        }

        // ── Quest sites ──────────────────────────────────────────────────
        // Orthogonal to the tunnel system (see QuestSiteRadius_Patch) — always shown,
        // even when tunnels are disabled.
        DrawSectionHeader(options, "MSSFP_Tunnels_Settings_Section_QuestSites".Translate(), ref scrollViewHeight);

        // Valid step range is bounded by QuestSiteRadiusHelper.Multipliers (5 entries:
        // Vanilla/4x/8x/16x/Unlimited) and MinMultipliers (4 entries: Vanilla/4x/8x/16x).
        // Sliding past these bounds would index out of range in the Harmony patch.
        s.QuestSiteRadiusStep = Mathf.RoundToInt(options.SliderLabeled(
            "MSSFP_Tunnels_Settings_QuestSiteRadiusStep".Translate(s.QuestSiteRadiusStep),
            s.QuestSiteRadiusStep, 0f, 4f));
        scrollViewHeight += 30f;

        s.QuestSiteMinRadiusStep = Mathf.RoundToInt(options.SliderLabeled(
            "MSSFP_Tunnels_Settings_QuestSiteMinRadiusStep".Translate(s.QuestSiteMinRadiusStep),
            s.QuestSiteMinRadiusStep, 0f, 3f));
        scrollViewHeight += 30f;
    }

    private static float RoundToStep(float value, float step)
    {
        return Mathf.Round(value / step) * step;
    }
}
