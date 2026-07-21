using System.Collections.Generic;
using System.Linq;
using KCSG;
using MSSFP.Comps.Map;
using MSSFP.ModExtensions;
using RimWorld;
using Verse;
using Verse.AI.Group;

// RimWorld 1.6 (Odyssey) introduced its own RimWorld.StructureLayoutDef, which collides with
// KCSG's. Alias so there is no ambiguity about which layout system this GenStep drives.
using StructureLayoutDef = KCSG.StructureLayoutDef;

namespace MSSFP.VFE.Structures;

/// <summary>
/// Drops a standalone (whole-map) viewer structure onto a Point of Interest.
///
/// Separate from GenStep_MSSFPViewerStructure: that class serves settlement replacement and
/// quest-site scatter, neither of which needs loot generation or a pawnless fallback. Folding
/// those concerns into its existing flag surface would make it harder to read, not easier.
/// </summary>
public class GenStep_MSSPointOfInterest : GenStep_CustomStructureGen
{
    /// <summary>Odds a layout with no authored pawns gets hostile occupants instead of friendly ones.</summary>
    private const float HostileChance = 0.5f;

    private const int MinFallbackPawns = 2;
    private const int MaxFallbackPawns = 5;

    /// <summary>Matches the value RimWorld's SymbolResolver_Settlement uses for base defenders.</summary>
    private const int DefendAssaultDelayTicks = 25000;

    public override int SeedPart => 1465920177;

    public override void Generate(Map map, GenStepParams parms)
    {
        if (!ViewerStructureSelector.EligibleStandalone(map).TryRandomElement(out StructureLayoutDef layout))
        {
            ModLog.Warn($"No eligible point-of-interest structure for map {map}");
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

        bool hasAuthoredPawns = ext?.spawnedPawns.NullOrEmpty() == false;
        if (hasAuthoredPawns)
            SpawnAuthorPawns(ext, map, rect);
        else
            ScatterFallbackPawns(map, rect);

        if (ext == null || ext.doLoot)
            ScatterLoot(map, rect);

        LayoutUtils.ReconnectAllPowerBuildings(map);
    }

    private static void SpawnAuthorPawns(StructureDefModExtension ext, Map map, CellRect rect)
    {
        if (ext.spawnedPawns.NullOrEmpty())
            return;

        Faction faction = ext.anyHostile ? Faction.OfAncientsHostile : Faction.OfAncients;
        IntVec3 lordCenter = ext.lordCenter.IsValid ? ext.lordCenter : rect.CenterCell;
        Lord lord = LordMaker.MakeNewLord(faction, new LordJob_DefendBase(faction, lordCenter, DefendAssaultDelayTicks, false), map);

        foreach (PawnRepr repr in ext.spawnedPawns)
        {
            repr.SpawnPawn(map, faction, lord);
        }
    }

    /// <summary>
    /// Only runs when the layout defines no authored pawns. Rolls friendly vs. hostile and generates
    /// pawns directly via PawnGenerationRequest rather than PawnGroupMaker/PawnGroupKindDefOf.Settlement
    /// — the exact API that had no usable maker for the faction that broke settlement replacement.
    /// </summary>
    private static void ScatterFallbackPawns(Map map, CellRect rect)
    {
        bool hostile = Rand.Chance(HostileChance);
        Faction faction = hostile ? Faction.OfAncientsHostile : Faction.OfAncients;
        PawnKindDef kindDef = hostile ? PawnKindDefOf.AncientSoldier : PawnKindDefOf.Drifter;

        Lord lord = LordMaker.MakeNewLord(faction, new LordJob_DefendBase(faction, rect.CenterCell, DefendAssaultDelayTicks, false), map);

        int count = Rand.RangeInclusive(MinFallbackPawns, MaxFallbackPawns);
        for (int i = 0; i < count; i++)
        {
            PawnGenerationRequest request = new(
                kindDef,
                faction,
                PawnGenerationContext.NonPlayer,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                inhabitant: false
            );

            Pawn pawn = PawnGenerator.GeneratePawn(request);
            lord.AddPawn(pawn);

            if (TryFindSpawnCell(map, rect, out IntVec3 cell))
            {
                GenSpawn.Spawn(pawn, cell, map);
            }
            else
            {
                ModLog.Warn("ScatterFallbackPawns: no valid spawn cell, discarding a generated pawn");
                pawn.Destroy();
            }
        }
    }

    private static void ScatterLoot(Map map, CellRect rect)
    {
        List<Thing> loot = MSSFPStructureDefOf.MSS_PointOfInterestLootCommon.root.Generate(new ThingSetMakerParams
        {
            totalMarketValueRange = new FloatRange(300f, 800f),
        });

        if (Rand.Chance(0.25f))
        {
            loot.AddRange(MSSFPStructureDefOf.MSS_PointOfInterestLootRare.root.Generate(new ThingSetMakerParams
            {
                totalMarketValueRange = new FloatRange(800f, 1500f),
                countRange = new IntRange(1, 2),
            }));
        }

        foreach (Thing thing in loot)
        {
            if (TryFindSpawnCell(map, rect, out IntVec3 cell))
            {
                GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
            }
            else
            {
                thing.Destroy();
            }
        }
    }

    private static bool TryFindSpawnCell(Map map, CellRect rect, out IntVec3 cell)
    {
        return rect.Cells.Where(c => c.InBounds(map) && c.Standable(map)).TryRandomElement(out cell);
    }
}
