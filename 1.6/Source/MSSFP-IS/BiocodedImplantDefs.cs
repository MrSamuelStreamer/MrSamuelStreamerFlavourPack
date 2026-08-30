using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.IS;

/// <summary>
/// Generates one "biocoded" wrapper <see cref="ThingDef"/> per salvageable implant product.
///
/// WHY A WRAPPER DEF AND NOT <c>CompBiocodable</c>: vanilla's biocode comp is only ever
/// consulted for weapons and apparel (<c>EquipmentUtility</c>, <c>Pawn_ApparelTracker</c>,
/// the biocoded/non-biocoded <c>SpecialThingFilterWorker</c>s all gate on
/// <c>def.IsWeapon</c>/<c>def.IsApparel</c>). Nothing in surgery reads it — there are zero
/// references in <c>Recipe_InstallArtificialBodyPart</c>, <c>Recipe_Surgery</c> or
/// <c>MedicalRecipesUtility</c>. Its only effect on a body-part item would be
/// <c>StatPart_Biocoded</c> zeroing market value, i.e. the opposite of what we want.
///
/// A distinct def, by contrast, is excluded from every install recipe for free: recipes
/// name the SOURCE def in their ingredient filters, so the variant simply never matches.
/// No <c>Bill</c> patch, no comp injection onto vanilla bionics.
/// </summary>
public static class BiocodedImplantDefs
{
    /// <summary>
    /// defName prefix. STABLE FOREVER — saved items resolve by defName, so renaming this
    /// orphans every biocoded implant in every existing save.
    /// </summary>
    public const string DefNamePrefix = "MSSFP_Biocoded_";

    /// <summary>Fraction of the source implant's market value the variant is worth.</summary>
    public const float ValueFraction = 0.25f;

    private static readonly Dictionary<ThingDef, ThingDef> SourceToBiocoded = new();

    /// <summary>Source implant product -> its biocoded variant. Populated during def generation.</summary>
    public static IReadOnlyDictionary<ThingDef, ThingDef> Map => SourceToBiocoded;

    public static bool TryGetBiocoded(ThingDef source, out ThingDef biocoded) =>
        SourceToBiocoded.TryGetValue(source, out biocoded);

    /// <summary>
    /// Every distinct ThingDef that some HediffDef drops on removal — i.e. exactly the set
    /// Luke's mod can extract. Covers modded implants for free.
    /// </summary>
    private static IEnumerable<ThingDef> SalvageableProducts()
    {
        HashSet<ThingDef> seen = new();
        List<HediffDef> hediffs = DefDatabase<HediffDef>.AllDefsListForReading;
        for (int i = 0; i < hediffs.Count; i++)
        {
            ThingDef product = hediffs[i].spawnThingOnRemoved;
            if (product != null && seen.Add(product))
                yield return product;
        }
    }

    public static IEnumerable<ThingDef> ImpliedDefs(bool hotReload = false)
    {
        SourceToBiocoded.Clear();
        foreach (ThingDef source in SalvageableProducts())
        {
            ThingDef variant = MakeVariant(source, hotReload);
            SourceToBiocoded[source] = variant;
            yield return variant;
        }
    }

    private static ThingDef MakeVariant(ThingDef source, bool hotReload)
    {
        string defName = DefNamePrefix + source.defName;
        ThingDef d = hotReload
            ? DefDatabase<ThingDef>.GetNamed(defName, false) ?? new ThingDef()
            : new ThingDef();

        d.defName = defName;
        d.label = "MSSFP_IS_BiocodedLabel".Translate(source.label);
        // Blurb PREPENDED, not appended: inspect panes truncate, and the sentence that
        // changes the player's decision must survive the truncation.
        d.description = "MSSFP_IS_BiocodedDesc".Translate() + "\n\n" + source.description;

        d.thingClass = source.thingClass;
        d.category = source.category;
        // Shared instance deliberately: GraphicDatabase caches by texture path, so every
        // variant resolves to the same Graphic and costs no extra atlas entry.
        d.graphicData = source.graphicData;
        d.drawerType = source.drawerType;
        d.altitudeLayer = source.altitudeLayer;
        d.selectable = source.selectable;
        d.useHitPoints = source.useHitPoints;
        d.pathCost = source.pathCost;
        d.techLevel = source.techLevel;
        d.soundInteract = source.soundInteract;
        d.soundDrop = source.soundDrop;
        d.stackLimit = 1;
        d.modContentPack = source.modContentPack;

        d.thingCategories =
            source.thingCategories != null
                ? new List<ThingCategoryDef>(source.thingCategories)
                : null;

        // Deep-cloned so a later edit to ours cannot mutate the source's StatModifier.
        d.statBases =
            source.statBases?.Select(sm => new StatModifier { stat = sm.stat, value = sm.value })
                .ToList() ?? new List<StatModifier>();

        // No comps. Biocoded salvage is inert, and an empty list avoids sharing mutable
        // CompProperties instances with the source def.
        d.comps = new List<CompProperties>();

        // Player may sell it; traders never stock it.
        d.tradeability = Tradeability.Sellable;

        // Everything below is deliberately NOT copied, so the variant can never appear in
        // raider gear, trader stock, quest rewards, or a crafting bill:
        //   techHediffsTags, tradeTags, thingSetMakerTags, recipeMaker, costList,
        //   researchPrerequisites, minifiedDef

        return d;
    }
}
