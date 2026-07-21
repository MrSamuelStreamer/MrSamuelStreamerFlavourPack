using System.Collections.Generic;
using System.Linq;
using KCSG;
using MSSFP;
using MSSFP.ModExtensions;
using RimWorld;
using Verse;

// RimWorld 1.6 (Odyssey) introduced its own RimWorld.StructureLayoutDef; alias to KCSG's so this
// file keeps compiling if a using RimWorld; is ever added.
using StructureLayoutDef = KCSG.StructureLayoutDef;

namespace MSSFP.VFE.Structures;

/// <summary>
/// Picks a viewer-submitted structure layout that is legal for a given map.
///
/// KCSG already gates its own layout picking on StructureLayoutDef.RequiredModLoaded, but that
/// property is internal to KCSG.dll, so the same check is reimplemented here against the public
/// modRequirements list.
/// </summary>
public static class ViewerStructureSelector
{
    /// <summary>
    /// Odds a given encounter uses a viewer structure. Single source of truth: the settlement
    /// Harmony patch and the quest-site GenStep both read this, so the two paths cannot drift.
    /// </summary>
    public const float UseChance = 0.08f;

    /// <summary>Mixed into world-object IDs so settlement and site rolls are independent.</summary>
    public const int SettlementSeedSalt = 0x4D5353;
    public const int SiteSeedSalt = 0x534954;

    /// <summary>Cells of slack required between the structure and the map edge.</summary>
    private const int EdgeMargin = 12;

    /// <summary>
    /// True when every mod the layout declares is actually loaded. Mirrors
    /// KCSG.StructureLayoutDef.ResolveModRequirements, including its PackageIdNonUnique comparison.
    /// </summary>
    private static bool RequirementsMet(StructureLayoutDef layout)
    {
        if (layout.modRequirements.NullOrEmpty())
            return true;

        return layout.modRequirements.All(req =>
            ModsConfig.ActiveModsInLoadOrder.Any(m => m.PackageIdNonUnique == req.ToLower())
        );
    }

    private static bool Fits(StructureLayoutDef layout, Map map)
    {
        return layout.Sizes.x + EdgeMargin <= map.Size.x && layout.Sizes.z + EdgeMargin <= map.Size.z;
    }

    /// <summary>
    /// All layouts legal for this map. <paramref name="standaloneOnly"/> restricts to layouts that
    /// are meant to fill a map on their own, which is what settlement replacement wants; quest-site
    /// scatter accepts fragments too.
    /// </summary>
    public static IEnumerable<StructureLayoutDef> Eligible(Map map, bool standaloneOnly)
    {
        foreach (StructureLayoutDef layout in DefDatabase<StructureLayoutDef>.AllDefsListForReading)
        {
            StructureDefModExtension ext = layout.GetModExtension<StructureDefModExtension>();
            if (ext == null || ext.excludeFromRandomGen)
                continue;
            // standalone = the layout IS the entire map, not a building within one. Paused for
            // now — see Settlement_MapGeneratorDef_Patch for why. Restoring this later is a
            // one-line revert; the standaloneOnly check below is kept for that reason.
            if (ext.standalone)
                continue;
            if (standaloneOnly && !ext.standalone)
                continue;
            if (ext.biome != null && ext.biome != map.Biome)
                continue;
            if (!RequirementsMet(layout) || !Fits(layout, map))
                continue;

            yield return layout;
        }
    }

    public static bool TryPick(Map map, bool standaloneOnly, out StructureLayoutDef layout)
    {
        return Eligible(map, standaloneOnly).TryRandomElement(out layout);
    }

    /// <summary>Standalone (whole-map) layouts, for the Point of Interest world object only. Settlement
    /// replacement (Eligible) still excludes these — see the pause note there.</summary>
    public static IEnumerable<StructureLayoutDef> EligibleStandalone(Map map)
    {
        foreach (StructureLayoutDef layout in DefDatabase<StructureLayoutDef>.AllDefsListForReading)
        {
            StructureDefModExtension ext = layout.GetModExtension<StructureDefModExtension>();
            if (ext == null || ext.excludeFromRandomGen || !ext.standalone)
                continue;
            if (ext.biome != null && ext.biome != map.Biome)
                continue;
            if (!RequirementsMet(layout) || !Fits(layout, map))
                continue;

            yield return layout;
        }
    }

    /// <summary>
    /// True if at least one standalone layout could generate in this biome. No Map exists yet at
    /// incident time, so this checks only what's knowable pre-generation (biome, mod requirements) —
    /// size is checked again by EligibleStandalone once the map's actual dimensions exist, but every
    /// point-of-interest map uses the same fixed size, so a biome pass here is what actually prevents
    /// an empty point of interest.
    /// </summary>
    public static bool AnyStandaloneViableFor(BiomeDef biome)
    {
        foreach (StructureLayoutDef layout in DefDatabase<StructureLayoutDef>.AllDefsListForReading)
        {
            StructureDefModExtension ext = layout.GetModExtension<StructureDefModExtension>();
            if (ext == null || ext.excludeFromRandomGen || !ext.standalone)
                continue;
            if (ext.biome != null && ext.biome != biome)
                continue;
            if (!RequirementsMet(layout))
                continue;

            return true;
        }
        return false;
    }

    /// <summary>
    /// Seeded so the answer is stable across map regeneration and reload for a given world object.
    /// A plain Rand.Chance would let a player reroll the outcome by leaving and re-entering.
    /// </summary>
    public static bool Roll(int worldObjectId, int salt)
    {
        if (MSSFPMod.settings?.EnableViewerStructures != true)
            return false;

        return Rand.ChanceSeeded(UseChance, Gen.HashCombineInt(worldObjectId, salt));
    }
}
