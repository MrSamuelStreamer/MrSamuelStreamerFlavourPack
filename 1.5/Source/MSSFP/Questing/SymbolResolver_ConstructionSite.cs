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

    public static readonly IntRange NumberOfShelves = new IntRange(10, 30);

    public static ThingDef ShelfSmall => DefDatabase<ThingDef>.GetNamed("ShelfSmall");
    public static ThingDef Shelf => DefDatabase<ThingDef>.GetNamed("Shelf");

    public static List<Thing> GetLoot()
    {
        return MSSFPDefOf.MSSFP_Construction.root.Generate(new ThingSetMakerParams { maxThingMarketValue = null, allowNonStackableDuplicates = true });
    }

    public override void Resolve(ResolveParams rp)
    {
        Map map = BaseGen.globalSettings.map;
        Faction enemyFaction = rp.faction ?? Find.FactionManager.RandomEnemyFaction();
        int edgeDefenseWidth = 0;
        if (rp.edgeDefenseWidth.HasValue)
            edgeDefenseWidth = rp.edgeDefenseWidth.Value;
        else if (rp.rect is { Width: >= 20, Height: >= 20 } && (enemyFaction.def.techLevel >= TechLevel.Industrial || Rand.Bool))
            edgeDefenseWidth = Rand.Bool ? 2 : 4;

        int step = rp.rect.Width - 2*InsetPathFromEdgeBy - PathWidth;

        int startVert = rp.rect.minX + Rand.RangeInclusive(0, step);
        int startHoriz = rp.rect.minZ + Rand.RangeInclusive(0, step);

        List<CellRect> areas = [];

        areas.Add(CellRect.FromLimits(rp.rect.minX,                     rp.rect.minZ,                  startVert, startHoriz)); // Top Left
        areas.Add(CellRect.FromLimits(startVert + PathWidth,    startHoriz + PathWidth, rp.rect.maxX,                rp.rect.maxZ)); // Bottom Right
        areas.Add(CellRect.FromLimits(startVert + PathWidth,     rp.rect.minZ,                  rp.rect.maxX,                startHoriz)); // Top Right
        areas.Add(CellRect.FromLimits(rp.rect.minX,                     startHoriz + PathWidth, startVert,           rp.rect.maxZ)); // Bottom Left
        SpawnPawns(rp, enemyFaction, map);
        BaseGen.symbolStack.Push("thing", rp with
        {
            rect = areas.RandomElement(),
            singleThingToSpawn = rp.conditionCauser
        });

        foreach (CellRect cellRect in areas)
        {
            GenerateStockpile(rp with {rect = cellRect}, map, enemyFaction);
        }

        ResolveParams floorRP = rp with { floorDef = TerrainDefOf.BrokenAsphalt};
        floorRP.allowBridgeOnAnyImpassableTerrain =  true;
        floorRP.floorOnlyIfTerrainSupports =  false;
        BaseGen.symbolStack.Push("floor", floorRP);

        BaseGen.symbolStack.Push("clear", rp with {clearRoof=true});

        ResolveParams ensureCanReachMapEdgeRP = rp with { rect = rp.rect.ContractedBy(edgeDefenseWidth), faction = enemyFaction };
        BaseGen.symbolStack.Push("ensureCanReachMapEdge", ensureCanReachMapEdgeRP);
    }

    public static void GenerateStockpile(ResolveParams rp, Map map, Faction enemyFaction)
    {
        ResolveParams wallRP = rp with
        {
            wallThingDef = ThingDefOf.AncientFence,
            chanceToSkipWallBlock = 0.05f
        };

        ResolveParams pathRP = rp with
        {
            floorDef = TerrainDefOf.Concrete,
            allowBridgeOnAnyImpassableTerrain = true,
            floorOnlyIfTerrainSupports = true
        };

        List<Thing> loot = GetLoot();

        int numberOfShelves = NumberOfShelves.RandomInRange;

        for (int i = 0; i < numberOfShelves; i++)
        {
            if(loot.Count <= 0) break;
            Thing thing = loot.InRandomOrder().First();

            ThingDef shelfType = Rand.Bool ? Shelf : ShelfSmall;
            ThingDef stuff = GenStuff.RandomStuffFor(shelfType);

            Building_Storage shelf = (Building_Storage)ThingMaker.MakeThing(shelfType, stuff);

            loot.Remove(thing);

            BaseGen.symbolStack.Push("thing", rp with
            {
                singleThingToSpawn = shelf,
                singleThingInnerThings = [thing]
            });
        }

        foreach (Thing thing in loot)
        {
            BaseGen.symbolStack.Push("thing", rp with
            {
                singleThingToSpawn = thing
            });
        }

        BaseGen.symbolStack.Push("outdoorLighting", rp);
        BaseGen.symbolStack.Push("edgeWalls", wallRP);
        for (int index = 0; index < GenMath.RoundRandom(rp.rect.Area / 400f); ++index)
        {
            ResolveParams firefoamPopperRP = rp with { faction = enemyFaction, rect = rp.rect};
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

            LordJob_DefendBase lordJobDefendBase = new(enemyFaction, centerCell, true);
            lord = LordMaker.MakeNewLord(enemyFaction, lordJobDefendBase, map);
        }

        rp.settlementLord = lord;

        ResolveParams pawnGenerationParams = rp with
        {
            rect = rp.rect,
            faction = enemyFaction,
            singlePawnLord = lord,
            pawnGroupKindDef = rp.pawnGroupKindDef ?? PawnGroupKindDefOf.Settlement,
            singlePawnSpawnCellExtraPredicate = rp.singlePawnSpawnCellExtraPredicate ?? (x => map.reachability.CanReachMapEdge(x, TraverseParms.For(TraverseMode.PassDoors)))
        };

        if (pawnGenerationParams.pawnGroupMakerParams == null)
        {
            pawnGenerationParams.pawnGroupMakerParams = new PawnGroupMakerParms();
            pawnGenerationParams.pawnGroupMakerParams.tile = map.Tile;
            pawnGenerationParams.pawnGroupMakerParams.faction = enemyFaction;
            PawnGroupMakerParms groupMakerParams = pawnGenerationParams.pawnGroupMakerParams;
            double num = rp.settlementPawnGroupPoints ?? (double) SymbolResolver_Settlement.DefaultPawnsPoints.RandomInRange;
            groupMakerParams.points = (float) num;
            pawnGenerationParams.pawnGroupMakerParams.inhabitants = true;
            pawnGenerationParams.pawnGroupMakerParams.seed = rp.settlementPawnGroupSeed;
        }

        BaseGen.symbolStack.Push("pawnGroup", pawnGenerationParams);
    }
}
