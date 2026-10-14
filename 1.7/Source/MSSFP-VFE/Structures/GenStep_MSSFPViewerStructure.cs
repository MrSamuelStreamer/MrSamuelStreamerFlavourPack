using System.Collections.Generic;
using KCSG;
using MSSFP.Comps.Map;
using MSSFP.ModExtensions;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

// RimWorld 1.6 (Odyssey) introduced its own RimWorld.StructureLayoutDef, which collides with
// KCSG's. Alias so there is no ambiguity about which layout system this GenStep drives.
using StructureLayoutDef = KCSG.StructureLayoutDef;

namespace MSSFP.VFE.Structures;

/// <summary>
/// Drops a viewer-submitted structure onto a map.
///
/// Placement, cleaning and fog handling are all inherited: the base class already runs
/// GetAllMineableIn -> CleanRect -> Generate in the right order and uses RimWorld 1.6's
/// water-aware, UsedRects-aware MapGenUtility rect finders. All this subclass adds is choosing
/// the layout at runtime, deciding whether to fire at all, and post-generation dressing.
/// </summary>
public class GenStep_MSSFPViewerStructure : GenStep_CustomStructureGen
{
    /// <summary>Only run on quest sites. Set on the scatter GenStepDef added to Encounter maps.</summary>
    public bool sitesOnly = false;

    /// <summary>
    /// Roll ViewerStructureSelector.UseChance before generating. The settlement path leaves this
    /// false because the roll already happened in the map-generator swap.
    /// </summary>
    public bool rollChance = false;

    public bool standaloneOnly = true;

    /// <summary>Spawn a defending faction group, as vanilla settlement generation would.</summary>
    public bool spawnDefenders = false;

    /// <summary>Matches the value RimWorld's SymbolResolver_Settlement uses for base defenders.</summary>
    private const int DefendAssaultDelayTicks = 25000;

    public override int SeedPart => 1465920163;

    public override void Generate(Map map, GenStepParams parms)
    {
        if (sitesOnly && map.Parent is not Site)
            return;

        if (rollChance && (map.Parent == null || !ViewerStructureSelector.Roll(map.Parent.ID, ViewerStructureSelector.SiteSeedSalt)))
            return;

        if (!ViewerStructureSelector.TryPick(map, standaloneOnly, out StructureLayoutDef layout))
        {
            ModLog.Warn($"No eligible viewer structure for map {map} (standaloneOnly={standaloneOnly})");
            return;
        }

        // The base class picks at random from this list; giving it exactly one pins the choice.
        structureLayoutDefs = new List<StructureLayoutDef> { layout };
        base.Generate(map, parms);
    }

    public override void PostGenerate(CellRect rect, Map map, GenStepParams parms)
    {
        if (structureLayoutDefs.NullOrEmpty())
            return;

        StructureLayoutDef layout = structureLayoutDefs[0];
        StructureDefModExtension ext = layout.GetModExtension<StructureDefModExtension>();

        map.GetComponent<GeneratedStructureMapComponent>()?.RecordStructure(layout.defName, ext?.author);

        // A layout may not carry the extension, so null-check before use.
        if (ext != null)
        {
            SpawnAuthorPawns(ext, map, rect);
        }

        if (spawnDefenders)
        {
            SpawnDefenders(map, rect, ext);
        }

        LayoutUtils.ReconnectAllPowerBuildings(map);
        StructureMapPawnGuard.EvictPlayerFactionPawns(map);
    }

    private static void SpawnAuthorPawns(StructureDefModExtension ext, Map map, CellRect rect)
    {
        if (ext.spawnedPawns.NullOrEmpty())
            return;

        Faction faction = ext.anyHostile ? Faction.OfAncientsHostile : map.ParentFaction ?? Faction.OfAncients;
        IntVec3 lordCenter = ext.lordCenter.IsValid ? ext.lordCenter : rect.CenterCell;
        Lord lord = LordMaker.MakeNewLord(faction, new LordJob_DefendBase(faction, lordCenter, DefendAssaultDelayTicks, false), map);

        foreach (PawnRepr repr in ext.spawnedPawns)
        {
            repr.SpawnPawn(map, faction, lord);
        }
    }

    /// <summary>
    /// Mirrors the pawnGroup push in RimWorld's SymbolResolver_Settlement so a replaced settlement
    /// still has inhabitants defending it, rather than being an empty shell.
    /// </summary>
    private static void SpawnDefenders(Map map, CellRect rect, StructureDefModExtension ext)
    {
        Faction faction = map.ParentFaction;
        if (faction == null || faction == Faction.OfPlayer)
            return;

        IntVec3 center = ext is { lordCenter.IsValid: true } ? ext.lordCenter : rect.CenterCell;

        BaseGen.globalSettings.map = map;
        Lord lord = LordMaker.MakeNewLord(faction, new LordJob_DefendBase(faction, center, DefendAssaultDelayTicks, false), map);

        ResolveParams rp = new()
        {
            rect = rect,
            faction = faction,
            singlePawnLord = lord,
            pawnGroupKindDef = PawnGroupKindDefOf.Settlement,
            pawnGroupMakerParams = new PawnGroupMakerParms
            {
                tile = map.Tile,
                faction = faction,
                points = StorytellerUtility.DefaultThreatPointsNow(map),
                inhabitants = true,
            },
        };

        BaseGen.symbolStack.Push("pawnGroup", rp, null);
        BaseGen.Generate();
    }
}
