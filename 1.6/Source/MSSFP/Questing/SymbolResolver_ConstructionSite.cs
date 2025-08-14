using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using Verse.AI.Group;

namespace MSSFP.Questing;

public class SymbolResolver_ConstructionSite : SymbolResolver
{
    public static readonly int InsetPathFromEdgeBy = 4;
    public static readonly int PathWidth = 2;
    public static readonly string AmbushTag = "mss_loversadvance_ambush";

    public static readonly IntRange NumberOfShelves = new IntRange(5, 25);

    public static ThingDef ShelfSmall => DefDatabase<ThingDef>.GetNamed("ShelfSmall");
    public static ThingDef Shelf => DefDatabase<ThingDef>.GetNamed("Shelf");

    public static List<Thing> GetLoot()
    {
        return MSSFPDefOf.MSSFP_Construction.root.Generate(
            new ThingSetMakerParams
            {
                maxThingMarketValue = null,
                allowNonStackableDuplicates = true,
            }
        );
    }

    public override void Resolve(ResolveParams rp)
    {
        Map map = BaseGen.globalSettings.map;
        Faction enemyFaction = rp.faction ?? Find.FactionManager.RandomEnemyFaction();
        int edgeDefenseWidth = 0;
        if (rp.edgeDefenseWidth.HasValue)
            edgeDefenseWidth = rp.edgeDefenseWidth.Value;
        else if (
            rp.rect is { Width: >= 20, Height: >= 20 }
            && (enemyFaction.def.techLevel >= TechLevel.Industrial || Rand.Bool)
        )
            edgeDefenseWidth = Rand.Bool ? 2 : 4;

        int step = rp.rect.Width - 2 * InsetPathFromEdgeBy - PathWidth;

        int startVert = rp.rect.minX + Rand.RangeInclusive(4, step - 4);
        int startHoriz = rp.rect.minZ + Rand.RangeInclusive(4, step - 4);

        List<CellRect> areas = [];

        areas.Add(CellRect.FromLimits(rp.rect.minX, rp.rect.minZ, startVert, startHoriz)); // Top Left
        areas.Add(
            CellRect.FromLimits(
                startVert + PathWidth,
                startHoriz + PathWidth,
                rp.rect.maxX,
                rp.rect.maxZ
            )
        ); // Bottom Right
        areas.Add(
            CellRect.FromLimits(startVert + PathWidth, rp.rect.minZ, rp.rect.maxX, startHoriz)
        ); // Top Right
        areas.Add(
            CellRect.FromLimits(rp.rect.minX, startHoriz + PathWidth, startVert, rp.rect.maxZ)
        ); // Bottom Left

        BaseGen.symbolStack.Push(
            "rectTrigger",
            rp with
            {
                rect = rp.rect,
                rectTriggerSignalTag = AmbushTag,
            }
        );

        BaseGen.symbolStack.Push(
            "ambush",
            rp with
            {
                ambushSignalTag = AmbushTag,
                ambushPoints = rp.threatPoints * 8,
                spawnNear = map.Center,
                ambushType = SignalActionAmbushType.Normal,
            }
        );

        SpawnPawns(rp, enemyFaction, map);
        BaseGen.symbolStack.Push(
            "thing",
            rp with
            {
                rect = areas.RandomElement(),
                singleThingToSpawn = rp.conditionCauser,
            }
        );

        foreach (CellRect cellRect in areas)
        {
            GenerateStockpile(rp with { rect = cellRect }, map, enemyFaction);
        }

        ResolveParams floorRP = rp with { floorDef = TerrainDefOf.BrokenAsphalt };
        floorRP.allowBridgeOnAnyImpassableTerrain = true;
        floorRP.floorOnlyIfTerrainSupports = false;
        BaseGen.symbolStack.Push("floor", floorRP);

        BaseGen.symbolStack.Push("clear", rp with { clearRoof = true });

        ResolveParams ensureCanReachMapEdgeRP = rp with
        {
            rect = rp.rect.ContractedBy(edgeDefenseWidth),
            faction = enemyFaction,
        };
        BaseGen.symbolStack.Push("ensureCanReachMapEdge", ensureCanReachMapEdgeRP);
    }

    public static Dictionary<ThingDef, float> _weightedConstructionThingDefs = [];

    public static void MaybeGetDef(
        ref Dictionary<ThingDef, float> defs,
        string defName,
        float chance = 0.05f
    )
    {
        ThingDef def = DefDatabase<ThingDef>.GetNamed(defName, false);
        if (def != null)
            defs.Add(def, chance);
    }

    public static Dictionary<ThingDef, float> WeightedConstructionThingDefs
    {
        get
        {
            if (_weightedConstructionThingDefs.NullOrEmpty())
            {
                _weightedConstructionThingDefs = new Dictionary<ThingDef, float>();
                _weightedConstructionThingDefs.Add(
                    DefDatabase<ThingDef>.GetNamed("TableStonecutter"),
                    0.05f
                );
                _weightedConstructionThingDefs.Add(
                    DefDatabase<ThingDef>.GetNamed("ElectricSmelter"),
                    0.05f
                );
                _weightedConstructionThingDefs.Add(
                    DefDatabase<ThingDef>.GetNamed("FueledSmithy"),
                    0.05f
                );
                _weightedConstructionThingDefs.Add(
                    DefDatabase<ThingDef>.GetNamed("ShelfSmall"),
                    0.5f
                );
                _weightedConstructionThingDefs.Add(DefDatabase<ThingDef>.GetNamed("Shelf"), 0.5f);

                MaybeGetDef(ref _weightedConstructionThingDefs, "FT_TableConcreteMixer");
                MaybeGetDef(ref _weightedConstructionThingDefs, "DankPyon_Bricks1x2c", 0.3f);
                MaybeGetDef(ref _weightedConstructionThingDefs, "DankPyon_Bricks2x2c", 0.3f);
                MaybeGetDef(ref _weightedConstructionThingDefs, "DankPyon_Kiln");
                MaybeGetDef(ref _weightedConstructionThingDefs, "VBY_PrimitiveKiln");
                MaybeGetDef(
                    ref _weightedConstructionThingDefs,
                    "BasicStorageIndustrialContainer",
                    0.3f
                );
                MaybeGetDef(ref _weightedConstructionThingDefs, "VBY_PrimitiveStoneCuttingTable");
                MaybeGetDef(ref _weightedConstructionThingDefs, "DankPyon_MiningTools");
                MaybeGetDef(ref _weightedConstructionThingDefs, "DankPyon_BarrelWater", 0.3f);
                MaybeGetDef(ref _weightedConstructionThingDefs, "BasicStoragePalletLarge", 0.3f);
                MaybeGetDef(ref _weightedConstructionThingDefs, "DankPyon_IronIngots1x1c", 0.3f);
                MaybeGetDef(ref _weightedConstructionThingDefs, "DankPyon_IronIngots1x2c", 0.3f);
                MaybeGetDef(ref _weightedConstructionThingDefs, "DankPyon_IronIngots2x2c", 0.3f);
                MaybeGetDef(ref _weightedConstructionThingDefs, "BasicStorageLargeCrate", 0.3f);
                MaybeGetDef(ref _weightedConstructionThingDefs, "BasicStorageWoodenCrate", 0.3f);
                MaybeGetDef(ref _weightedConstructionThingDefs, "BasicStorageMediumCrate", 0.3f);
                MaybeGetDef(ref _weightedConstructionThingDefs, "VFEC_ConcretePress");
                MaybeGetDef(ref _weightedConstructionThingDefs, "DankPyon_BundledSack2x2c", 0.3f);
                MaybeGetDef(ref _weightedConstructionThingDefs, "DankPyon_BundledSack1x1c", 0.3f);
                MaybeGetDef(ref _weightedConstructionThingDefs, "DankPyon_Timber1x1c", 0.3f);
                MaybeGetDef(ref _weightedConstructionThingDefs, "DankPyon_WoodLogs1x1c", 0.3f);
                MaybeGetDef(ref _weightedConstructionThingDefs, "DankPyon_Timber1x2c", 0.3f);
                MaybeGetDef(ref _weightedConstructionThingDefs, "DankPyon_WoodLogs1x2c", 0.3f);
                MaybeGetDef(ref _weightedConstructionThingDefs, "DankPyon_Timber2x2c", 0.3f);
                MaybeGetDef(ref _weightedConstructionThingDefs, "DankPyon_WoodLogs2x2c", 0.3f);
                MaybeGetDef(ref _weightedConstructionThingDefs, "ASF_StorageTent");
            }

            return _weightedConstructionThingDefs;
        }
    }

    public static Thing RandomConstructionThing()
    {
        ThingDef def = WeightedConstructionThingDefs.RandomElementByWeight(e => e.Value).Key;

        ThingDef stuff = GenStuff.RandomStuffFor(def);

        return ThingMaker.MakeThing(def, stuff);
    }

    public static void GenerateStockpile(ResolveParams rp, Map map, Faction enemyFaction)
    {
        ResolveParams wallRP = rp with
        {
            wallThingDef = ThingDefOf.AncientFence,
            chanceToSkipWallBlock = 0.05f,
        };

        ResolveParams pathRP = rp with
        {
            floorDef = TerrainDefOf.Concrete,
            allowBridgeOnAnyImpassableTerrain = true,
            floorOnlyIfTerrainSupports = true,
        };

        List<Thing> loot = GetLoot();

        int numberOfShelves = NumberOfShelves.RandomInRange;

        for (int i = 0; i < numberOfShelves; i++)
        {
            if (loot.Count <= 0)
                break;
            Thing thing = loot.InRandomOrder().First();

            Thing container = RandomConstructionThing();

            loot.Remove(thing);

            BaseGen.symbolStack.Push(
                "thing",
                rp with
                {
                    singleThingToSpawn = container,
                    singleThingInnerThings = [thing],
                }
            );
        }

        List<Thing> list = ThingSetMakerDefOf.MapGen_AncientTempleContents.root.Generate();
        list.AddRange(ThingSetMakerDefOf.MapGen_AncientTempleContents.root.Generate());
        list.AddRange(ThingSetMakerDefOf.MapGen_AncientTempleContents.root.Generate());
        list.AddRange(ThingSetMakerDefOf.MapGen_AncientTempleContents.root.Generate());
        list.AddRange(ThingSetMakerDefOf.MapGen_AncientTempleContents.root.Generate());
        list.AddRange(ThingSetMakerDefOf.MapGen_AncientTempleContents.root.Generate());
        list.AddRange(ThingSetMakerDefOf.MapGen_AncientTempleContents.root.Generate());
        list.AddRange(ThingSetMakerDefOf.MapGen_AncientTempleContents.root.Generate());
        list.AddRange(ThingSetMakerDefOf.MapGen_AncientTempleContents.root.Generate());

        list.SortByDescending(t => t.MarketValue * t.stackCount);
        for (int index = 0; index < list.Count; ++index)
        {
            ResolveParams resolveParams = rp;
            if (ModsConfig.IdeologyActive && index == 0)
            {
                resolveParams.singleThingDef = ThingDefOf.AncientHermeticCrate;
                resolveParams.singleThingInnerThings = [list[0]];
            }
            else
                resolveParams.singleThingToSpawn = list[index];
            BaseGen.symbolStack.Push("thing", resolveParams);
        }

        foreach (Thing thing in loot)
        {
            BaseGen.symbolStack.Push("thing", rp with { singleThingToSpawn = thing });
        }

        BaseGen.symbolStack.Push("outdoorLighting", rp);
        BaseGen.symbolStack.Push("edgeWalls", wallRP);
        for (int index = 0; index < GenMath.RoundRandom(rp.rect.Area / 400f); ++index)
        {
            ResolveParams firefoamPopperRP = rp with { faction = enemyFaction, rect = rp.rect };
            BaseGen.symbolStack.Push("firefoamPopper", firefoamPopperRP);
        }
        BaseGen.symbolStack.Push("floor", pathRP);
    }

    public static void SpawnPawns(ResolveParams rp, Faction enemyFaction, Map map)
    {
        Lord lord = rp.singlePawnLord;
        if (lord == null)
        {
            IntVec3 centerCell = rp.rect.CenterCell;

            LordJob_DefendBase lordJobDefendBase = new(enemyFaction, centerCell, 0);
            lord = LordMaker.MakeNewLord(enemyFaction, lordJobDefendBase, map);
        }

        rp.settlementLord = lord;

        ResolveParams pawnGenerationParams = rp with
        {
            rect = rp.rect,
            faction = enemyFaction,
            singlePawnLord = lord,
            pawnGroupKindDef = PawnGroupKindDefOf.Loggers,
            singlePawnSpawnCellExtraPredicate = x =>
                map.reachability.CanReachMapEdge(x, TraverseParms.For(TraverseMode.PassDoors)),
        };

        if (pawnGenerationParams.pawnGroupMakerParams == null)
        {
            pawnGenerationParams.pawnGroupMakerParams = new PawnGroupMakerParms();
            pawnGenerationParams.pawnGroupMakerParams.generateFightersOnly = false;
            pawnGenerationParams.pawnGroupMakerParams.dontUseSingleUseRocketLaunchers = true;
            pawnGenerationParams.pawnGroupMakerParams.tile = map.Tile;
            pawnGenerationParams.pawnGroupMakerParams.faction = enemyFaction;
            PawnGroupMakerParms groupMakerParams = pawnGenerationParams.pawnGroupMakerParams;
            groupMakerParams.points = (rp.threatPoints ?? 200) / 4;
            pawnGenerationParams.pawnGroupMakerParams.inhabitants = true;
            pawnGenerationParams.pawnGroupMakerParams.seed = rp.settlementPawnGroupSeed;
        }

        BaseGen.symbolStack.Push("pawnGroup", pawnGenerationParams);
    }
}
