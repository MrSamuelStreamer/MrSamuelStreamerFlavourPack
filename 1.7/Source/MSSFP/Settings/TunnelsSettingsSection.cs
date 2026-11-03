using MSSFP.Tunnels;
using MSSFP.Utils;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.SettingsTabs;

/// <summary>
/// Tunnel controls drawn inside <see cref="WorldSettingsTab"/>.
/// Persisted fields live on <see cref="MSSFP.Settings"/>.
/// </summary>
public class TunnelsSettingsSection
{
    public void Draw(SettingsTab host, MSSFP.Settings settings, Listing_Standard options, ref float scrollViewHeight)
    {
        options.CheckboxLabeled(
            "MSSFP_Tunnels_Settings_EnableCheckbox".Translate(),
            ref settings.TunnelsEnabledForNewWorlds,
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
        host.DrawSectionHeader(options, "MSSFP_Tunnels_Settings_Section_TravelSpeed".Translate(), ref scrollViewHeight);

        settings.TunnelDefaultTilesPerHour = RoundToStep(options.SliderLabeled(
            "MSSFP_Tunnels_Settings_DefaultTilesPerHour".Translate(settings.TunnelDefaultTilesPerHour.ToString("0.#")),
            settings.TunnelDefaultTilesPerHour, 1f, 20f), 0.5f);
        scrollViewHeight += 30f;

        settings.TunnelResearchedTilesPerHour = RoundToStep(options.SliderLabeled(
            "MSSFP_Tunnels_Settings_ResearchedTilesPerHour".Translate(settings.TunnelResearchedTilesPerHour.ToString("0.#")),
            settings.TunnelResearchedTilesPerHour, 3f, 30f), 0.5f);
        scrollViewHeight += 30f;

        // ── Map generation ───────────────────────────────────────────────
        host.DrawSectionHeader(options, "MSSFP_Tunnels_Settings_Section_MapGeneration".Translate(), ref scrollViewHeight);

        settings.RubyVeinSpawnChance = RoundToStep(options.SliderLabeled(
            "MSSFP_Tunnels_Settings_RubyVeinSpawnChance".Translate(settings.RubyVeinSpawnChance.ToStringPercent("F1")),
            settings.RubyVeinSpawnChance, 0f, 0.05f), 0.001f);
        scrollViewHeight += 30f;

        // ── Incidents ────────────────────────────────────────────────────
        host.DrawSectionHeader(options, "MSSFP_Tunnels_Settings_Section_Incidents".Translate(), ref scrollViewHeight);

        host.DrawCheckBox(
            options,
            "MSSFP_Tunnels_Settings_DisableTunnelIncidents".Translate(),
            ref settings.DisableTunnelIncidents,
            ref scrollViewHeight);

        if (!settings.DisableTunnelIncidents)
        {
            // Floored at 0.05 rather than 0 to avoid Storyteller zero-weight ambiguity
            // (a zero-weight incident category can behave inconsistently with some
            // storyteller implementations rather than cleanly "never firing").
            settings.TunnelIncidentWeightMultiplier = RoundToStep(options.SliderLabeled(
                "MSSFP_Tunnels_Settings_IncidentWeightMultiplier".Translate(settings.TunnelIncidentWeightMultiplier.ToString("0.00")),
                settings.TunnelIncidentWeightMultiplier, 0.05f, 2f), 0.05f);
            scrollViewHeight += 30f;

            host.DrawCheckBox(
                options,
                "MSSFP_Tunnels_Settings_AllowCombatTunnelIncidents".Translate(),
                ref settings.AllowCombatTunnelIncidents,
                ref scrollViewHeight);
        }

        // ── Quest sites ──────────────────────────────────────────────────
        // Orthogonal to the tunnel system (see QuestSiteRadius_Patch) — always shown,
        // even when tunnels are disabled.
        host.DrawSectionHeader(options, "MSSFP_Tunnels_Settings_Section_QuestSites".Translate(), ref scrollViewHeight);

        // Valid step range is bounded by QuestSiteRadiusHelper.Multipliers (5 entries:
        // Vanilla/4x/8x/16x/Unlimited) and MinMultipliers (4 entries: Vanilla/4x/8x/16x).
        // Sliding past these bounds would index out of range in the Harmony patch.
        settings.QuestSiteRadiusStep = Mathf.RoundToInt(options.SliderLabeled(
            "MSSFP_Tunnels_Settings_QuestSiteRadiusStep".Translate(settings.QuestSiteRadiusStep),
            settings.QuestSiteRadiusStep, 0f, 4f));
        scrollViewHeight += 30f;

        settings.QuestSiteMinRadiusStep = Mathf.RoundToInt(options.SliderLabeled(
            "MSSFP_Tunnels_Settings_QuestSiteMinRadiusStep".Translate(settings.QuestSiteMinRadiusStep),
            settings.QuestSiteMinRadiusStep, 0f, 3f));
        scrollViewHeight += 30f;
    }

    private static float RoundToStep(float value, float step)
    {
        return Mathf.Round(value / step) * step;
    }
}
