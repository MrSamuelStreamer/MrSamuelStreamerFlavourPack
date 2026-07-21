using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.VFE.Structures;

/// <summary>
/// Sparsely places a Point of Interest on the world map — a home for standalone (whole-map)
/// structures, kept off the Settlement path entirely. See GenStep_MSSPointOfInterest and
/// Settlement_MapGeneratorDef_Patch for why.
/// </summary>
public class IncidentWorker_MSSPointOfInterest : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (MSSFPMod.settings?.EnableViewerStructures != true)
            return false;

        return TryFindViableTile(out _);
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (!TryFindViableTile(out PlanetTile tile))
            return false;

        Site site = SiteMaker.MakeSite(
            MSSFPStructureDefOf.MSS_PointOfInterest,
            tile,
            faction: null,
            worldObjectDef: MSSFPStructureDefOf.MSS_PointOfInterestWorldObject
        );
        Find.WorldObjects.Add(site);
        SendStandardLetter(parms, new LookTargets(site));
        return true;
    }

    // Rejects any tile whose biome can't fit at least one standalone layout, so a Point of Interest
    // can never spawn with nothing eligible to place on it — see AnyStandaloneViableFor.
    private static bool TryFindViableTile(out PlanetTile tile)
    {
        return TileFinder.TryFindNewSiteTile(
            out tile,
            minDist: 10,
            maxDist: 30,
            allowCaravans: false,
            validator: t => ViewerStructureSelector.AnyStandaloneViableFor(Find.WorldGrid[t].PrimaryBiome)
        );
    }
}
