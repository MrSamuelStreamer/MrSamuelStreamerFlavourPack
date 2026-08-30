using MSSFP.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.IS;

/// <summary>
/// Settings for the Implant Salvage compatibility layer.
///
/// This tab is discovered by reflection over loaded assemblies (see
/// <c>MSSFP.Settings.LoadTabs</c>), so it registers ONLY when this assembly is loaded —
/// which <c>loadFolders.xml</c> gates on <c>luke.implantsalvage</c> being active. Players
/// without Luke's mod never see the tab.
///
/// State lives here rather than on core <see cref="MSSFP.Settings"/> for the same reason:
/// nothing about this feature should exist when the target mod is absent. Scribing is done
/// in <see cref="ExposeData"/>, mirroring <c>PawnPortabilitySettingsTab</c>.
/// </summary>
public class ImplantSalvageSettingsTab : SettingsTab
{
    /// <summary>Default chance a surviving implant comes out biocoded.</summary>
    public const float DefaultBiocodeChance = 0.75f;

    /// <summary>
    /// Chance that an implant which survived Luke's destroy roll is nonetheless
    /// rendered non-installable. Sale value is unaffected by this slider; it is a
    /// flat 25% of source, baked into the generated def.
    /// </summary>
    public float BiocodeChance = DefaultBiocodeChance;

    public ImplantSalvageSettingsTab(ModSettings settings, Mod mod)
        : base(settings, mod) { }

    public override string TabName => "Implant Salvage";

    public override int TabOrder => 90;

    /// <summary>
    /// Live chance, read from the registered tab instance. Falls back to the default if
    /// the tab has not been constructed yet (def generation can run before settings load).
    /// </summary>
    public static float Chance =>
        MSSFPMod.settings?.GetSettings<ImplantSalvageSettingsTab>()?.BiocodeChance
        ?? DefaultBiocodeChance;

    public override void DoTabContents(
        Listing_Standard options,
        Rect scrollViewRect,
        ref float scrollViewHeight
    )
    {
        scrollViewHeight += options
            .Label("MSSFP_IS_Settings_Blurb".Translate())
            .height;

        BiocodeChance = options.SliderLabeled(
            "MSSFP_IS_Settings_BiocodeChance".Translate(BiocodeChance.ToStringPercent()),
            BiocodeChance,
            0f,
            1f
        );
        scrollViewHeight += 30f;
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref BiocodeChance, "MSSFP_IS_BiocodeChance", DefaultBiocodeChance);
    }
}
