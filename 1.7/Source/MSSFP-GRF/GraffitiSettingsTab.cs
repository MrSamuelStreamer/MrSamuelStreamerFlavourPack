using MSSFP.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.GRF;

/// <summary>
/// Settings for the Graffiti Mod compatibility layer.
///
/// Discovered by reflection over loaded assemblies (see <c>MSSFP.Settings.LoadTabs</c>),
/// so it registers only when this assembly is loaded — which <c>loadFolders.xml</c> gates
/// on <c>Mlie.GraffitiMod</c> being active. Players without the graffiti mod never see it.
/// </summary>
public class GraffitiSettingsTab : SettingsTab
{
    public const bool DefaultPermanentGraffiti = false;

    /// <summary>
    /// When set, graffiti stops being treated as cleanable filth: colonists will not take
    /// cleaning jobs on it, rain will not wash it away, and pawns will not track it off
    /// the wall on their feet. Defaults to off, so the graffiti mod behaves as shipped
    /// unless the player opts in.
    /// </summary>
    public bool PermanentGraffiti = DefaultPermanentGraffiti;

    public GraffitiSettingsTab(ModSettings settings, Mod mod)
        : base(settings, mod) { }

    public override string TabName => "Graffiti";

    public override int TabOrder => 91;

    /// <summary>
    /// Live value, read from the registered tab instance. Falls back to the default when
    /// the tab has not been constructed yet — the patch can run before settings load.
    /// </summary>
    public static bool Permanent =>
        MSSFPMod.settings?.GetSettings<GraffitiSettingsTab>()?.PermanentGraffiti
        ?? DefaultPermanentGraffiti;

    public override void DoTabContents(
        Listing_Standard options,
        Rect scrollViewRect,
        ref float scrollViewHeight
    )
    {
        scrollViewHeight += options.Label("MSSFP_GRF_Settings_Blurb".Translate()).height;

        options.CheckboxLabeled(
            "MSSFP_GRF_Settings_Permanent".Translate(),
            ref PermanentGraffiti,
            "MSSFP_GRF_Settings_PermanentTip".Translate()
        );
        scrollViewHeight += 30f;
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(
            ref PermanentGraffiti,
            "MSSFP_GRF_PermanentGraffiti",
            DefaultPermanentGraffiti
        );
    }
}
