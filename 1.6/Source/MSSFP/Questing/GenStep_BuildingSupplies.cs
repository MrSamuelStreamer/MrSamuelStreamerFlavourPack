using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Questing;

public class GenStep_BuildingSupplies : GenStep
{
    private const int Size = 40;
    public override int SeedPart => 272971578;

    public override void Generate(Map map, GenStepParams parms)
    {
        Faction faction =
            map.ParentFaction == null || map.ParentFaction == Faction.OfPlayer
                ? Find.FactionManager.RandomEnemyFaction()
                : map.ParentFaction;
        if (!MapGenerator.TryGetVar("UsedRects", out List<CellRect> usedRects))
        {
            usedRects = [];
            MapGenerator.SetVar("UsedRects", usedRects);
        }
        CellRect rectOfInterest = CellRect.CenteredOn(map.Center, Size, Size).ClipInsideMap(map);
        SitePart sitePart = parms.sitePart;
        sitePart.conditionCauserWasSpawned = true;
        ResolveParams resolveParams = new()
        {
            rect = rectOfInterest,
            faction = faction,
            conditionCauser = sitePart.conditionCauser,
        };
        BaseGen.globalSettings.map = map;
        BaseGen.symbolStack.Push("mss_basePart_constructionSite", resolveParams);
        BaseGen.Generate();
        MapGenerator.SetVar("RectOfInterest", rectOfInterest);
        usedRects.Add(rectOfInterest);
    }
}
