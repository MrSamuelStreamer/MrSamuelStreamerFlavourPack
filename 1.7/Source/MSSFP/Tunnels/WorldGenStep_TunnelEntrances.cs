using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.Tunnels;

public class WorldGenStep_TunnelEntrances : WorldGenStep
{
    public FloatRange tunnelSitesPer100kTiles;

    public override int SeedPart => 235235252;

    public static HashSet<LandmarkDef> PossibleLandmarks
    {
        get
        {
            if (field.NullOrEmpty())
            {
                field =
                [
                    LandmarkDefOf.Chasm,
                    LandmarkDefOf.Cavern,
                    LandmarkDefOf.Hollow,
                    LandmarkDefOf.Valley,
                ];
            }
            return field;
        }
    }

    public override void GenerateFresh(string seed, PlanetLayer layer)
    {
        // Sticky per-save gate (see plan Cluster D): compute the effective enabled
        // state once here, snapshot it onto the world's TunnelGenData, and never
        // re-derive it from live settings again for the life of this save.
        TunnelGenData comp = Find.World.GetComponent<TunnelGenData>();
        if (comp == null)
            return;

        if (!comp.wasSet)
        {
            comp.tunnelsEnabledForSave =
                MSSFPMod.settings.TunnelsEnabledForNewWorlds
                || Find.Scenario?.AllParts?.OfType<MSSFP.ScenParts.ScenPart_EnableTunnels>().Any() == true;
            comp.wasSet = true;
            TunnelUtilities.RefreshCache();
        }

        if (!comp.tunnelsEnabledForSave)
            return;

        GenerateTunnelEntrances(layer);
    }

    public void GenerateTunnelEntrances(PlanetLayer layer)
    {
        Faction faction = Faction.OfAncients;

        float viewAngleFactor = layer.Def.viewAngleSettlementsFactorCurve.Evaluate(Mathf.Clamp01(layer.ViewAngle / 180f));
        float scaleFactor = Find.World.info.overallPopulation.GetScaleFactor();
        int settlementsToGenerateCount = GenMath.RoundRandom(layer.TilesCount / 100000f * tunnelSitesPer100kTiles.RandomInRange * scaleFactor * viewAngleFactor);

        for (int index = 0; index < settlementsToGenerateCount; ++index)
        {
            PlanetTile tile = TileFinder.RandomSettlementTileFor(layer, faction);
            SpawnTunnelEntrance(tile, layer, faction);
        }
    }

    public static TunnelEntrance SpawnTunnelEntrance(PlanetTile tile, PlanetLayer layer, Faction faction)
    {
        TunnelEntrance worldObject = (TunnelEntrance)WorldObjectMaker.MakeWorldObject(MSSFPDefOf.MSSFP_TunnelEntranceSite);
        worldObject.SetFaction(faction);
        worldObject.Tile = tile;
        List<SitePartDefWithParams> sitePartDefsWithParams;

        if (Faction.OfPlayerSilentFail != null)
        {
            SiteMakerHelper.GenerateDefaultParams(StorytellerUtility.DefaultSiteThreatPointsNow(), worldObject.Tile, faction, [MSSFPDefOf.MSSFP_TunnelEntranceSitePart], out sitePartDefsWithParams);
        }
        else
        {
            SiteMakerHelper.GenerateDefaultParams(100, worldObject.Tile, faction, [MSSFPDefOf.MSSFP_TunnelEntranceSitePart], out sitePartDefsWithParams);
        }
        worldObject.AddPart(new SitePart(worldObject, sitePartDefsWithParams[0].def, sitePartDefsWithParams[0].parms));

        worldObject.Name = SettlementNameGenerator.GenerateSettlementName(worldObject, MSSFPDefOf.MSSFP_TunnelEntranceSite.nameMaker);

        if (ModsConfig.OdysseyActive)
            Find.World.landmarks.AddLandmark(PossibleLandmarks.RandomElement(), worldObject.Tile, layer, true);
        Find.WorldObjects.Add(worldObject);

        return worldObject;
    }
}
