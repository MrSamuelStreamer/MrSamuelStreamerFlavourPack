using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Tunnels;

public class WorldGenStep_Tunnels : WorldGenStep
{
    // Controls how many extra endpoints get sprinkled in, scaled by world size (tiles / 100k).
    private static readonly FloatRange ExtraTunnelNodesPer100KTiles = new FloatRange(5f, 25f);

    // Probability of allowing an extra connection even if it would create a cycle (i.e., not needed for spanning tree).
    private const float ChanceExtraNonSpanningTreeLink = 0.15f;

    // Probability of *not* adding a prospective link even if we decided it's eligible.
    // (This creates some missing connections so the network isn't too dense/regular.)
    private const float ChanceHideSpanningTreeLink = 0.01f;

    // For each endpoint, we look for up to this many nearby endpoints to propose links to.
    private const int PotentialSpanningTreeLinksPerSettlement = 8;

    public override int SeedPart => 1538472345;

    public bool RegenerateNeeded(PlanetTile tile)
    {
        // Membership in tunnelNodes (not potentialTunnels, which is now a pure lookup
        // and never gains entries just from being queried) tracks whether this tile
        // is already a known network node.
        return !TunnelGenData.Instance.tunnelNodes.TryGetValue(tile.Layer, out List<PlanetTile> nodes)
               || !nodes.Contains(tile);
    }

    public void Regenerate(PlanetLayer layer)
    {
        if (!TunnelUtilities.IsEnabled())
            return;

        GenerateTunnelEndpoints(layer, true);
        Rand.PushState();
        int seedFromSeedString = GenText.StableStringHash(Find.World.info.seedString);
        Rand.Seed = Gen.HashCombineInt(seedFromSeedString, SeedPart);
        GenerateTunnelNetwork(layer);
        Rand.PopState();
    }

    public override void GenerateFresh(string seed, PlanetLayer layer)
    {
        TunnelGenData comp = Find.World.GetComponent<TunnelGenData>();
        if (comp is not { tunnelsEnabledForSave: true })
            return;

        GenerateTunnelEndpoints(layer);
        Rand.PushState();
        int seedFromSeedString = GenText.StableStringHash(seed);
        Rand.Seed = Gen.HashCombineInt(seedFromSeedString, SeedPart);
        GenerateTunnelNetwork(layer);
        Rand.PopState();
    }

    public override void GenerateWithoutWorldData(string seed, PlanetLayer layer)
    {
        if (!TunnelUtilities.IsEnabled())
            return;

        Rand.PushState();
        int seedFromSeedString = GenText.StableStringHash(seed);
        Rand.Seed = Gen.HashCombineInt(seedFromSeedString, SeedPart);
        GenerateTunnelNetwork(layer);
        Rand.PopState();
    }

    private void GenerateTunnelEndpoints(PlanetLayer layer, bool withPlayerHomes = false)
    {
        TunnelGenData.Instance.Clear();

        List<PlanetTile> candidateNodes = Find.WorldObjects.AllWorldObjects
            .Where(wo => wo.def == MSSFPDefOf.MSSFP_TunnelEntranceSite)
            .Select(wo => wo.Tile)
            .ToList();

        if (withPlayerHomes)
        {
            // Add player home tiles.
            candidateNodes.AddRange(Current.Game.Maps.Where(map => map.IsPlayerHome).Select(map => map.Tile));
        }

        // Add additional random "settlement-like" tiles to increase network complexity with world size.
        int randomCandidates = GenMath.RoundRandom(Find.WorldGrid.TilesCount / 100000f * ExtraTunnelNodesPer100KTiles.RandomInRange);
        for (int index = 0; index < randomCandidates; ++index)
            candidateNodes.Add(TileFinder.RandomSettlementTileFor(layer, null));

        // Remove duplicates so we don't build redundant nodes/links.
        // Store nodes into the world generation data; later steps read from here.
        TunnelGenData.Instance.tunnelNodes[layer] = candidateNodes.Distinct().ToList();
    }

    private void GenerateTunnelNetwork(PlanetLayer layer)
    {
        // Ensure the path grid has up-to-date perceived costs for this layer before pathing/flooding.
        Find.WorldPathGrid.RecalculateLayerPerceivedPathCosts(layer, 0);

        // 1) build candidate links between nearby endpoints...
        // 2) reduce them to a mostly-connected final link set...
        // 3) draw them onto the world as "roads" (your tunnels).
        List<Link> finalLinks = GenerateFinalLinks(
            GenerateProspectiveLinks(TunnelGenData.Instance.tunnelNodes[layer], layer),
            TunnelGenData.Instance.tunnelNodes[layer].Count);

        DrawLinksOnWorld(finalLinks, TunnelGenData.Instance.tunnelNodes[layer]);
    }

    private List<Link> GenerateProspectiveLinks(
        List<PlanetTile> indexToTile,
        PlanetLayer layer)
    {
        // Map from the actual tile to its endpoint index (stored as a PlanetTile whose tileId is the index).
        // This lets us quickly detect "we reached another endpoint" during flood fill.
        Dictionary<PlanetTile, PlanetTile> tileToIndexLookup = new Dictionary<PlanetTile, PlanetTile>();
        for (int index = 0; index < indexToTile.Count; ++index)
            tileToIndexLookup[indexToTile[index]] = new PlanetTile(index, layer);

        // All candidate (A,B) links, each with a path distance/cost between the endpoints.
        List<Link> linkProspective = new List<Link>();

        // Reused list for FloodPathsWithCost start set.
        List<PlanetTile> startTiles = new List<PlanetTile>();

        // For each endpoint, flood outward and record the first few other endpoints we encounter.
        for (int index = 0; index < indexToTile.Count; ++index)
        {
            int srcLocal = index;
            PlanetTile srcTile = indexToTile[index];

            startTiles.Clear();
            startTiles.Add(srcTile);

            // Counts how many endpoints we've found from this source so we can early-stop.
            int found = 0;

            TunnelGenData.Instance.Pather.FloodPathsWithCost(
                startTiles,
                (src, dst) => src == dst ? 0 : 1,
                _ => false,
                terminator: (tile, distance) =>
                {
                    // If the flood reaches another endpoint tile, record a prospective link.
                    if (tile != srcTile && tileToIndexLookup.TryGetValue(tile, out PlanetTile planetTile))
                    {
                        ++found;

                        linkProspective.Add(new Link
                        {
                            // Store the flood cost as the link weight.
                            distance = distance,

                            // Save endpoints as indices into indexToTile.
                            indexA = srcLocal,
                            indexB = planetTile.tileId,
                        });
                    }

                    // Stop searching once we've found enough nearby endpoints from this source.
                    return found >= PotentialSpanningTreeLinksPerSettlement;
                });
        }

        // Sort by ascending cost so we consider cheaper/shorter links first when building the network.
        linkProspective.Sort((lhs, rhs) => lhs.distance.CompareTo(rhs.distance));
        return linkProspective;
    }

    private List<Link> GenerateFinalLinks(
        List<Link> linkProspective,
        int endpointCount)
    {
        // Disjoint-set / union-find-like structure to keep track of which endpoints are already connected.
        // (This is a very small, simple version: Group() climbs parent pointers recursively.)
        List<Connectedness> connectednessList = new List<Connectedness>();
        for (int index = 0; index < endpointCount; ++index)
            connectednessList.Add(new Connectedness());

        // The final set of links we will actually draw.
        List<Link> list = new List<Link>();

        // Iterate links in increasing distance so we mostly build a minimal spanning structure.
        foreach (Link prospective in linkProspective)
        {
            bool differentGroups =
                connectednessList[prospective.indexA].Group() != connectednessList[prospective.indexB].Group();

            // Allow the link if it connects two currently-disconnected groups (spanning tree behavior),
            // OR rarely allow an extra cycle link (adds alternate routes / loops).
            Link prospective1 = prospective;
            bool allowExtraNonTreeLink =
                Rand.Value <= ChanceExtraNonSpanningTreeLink
                && !list.Any(link => link.indexB == prospective1.indexA && link.indexA == prospective1.indexB);

            if (differentGroups || allowExtraNonTreeLink)
            {
                // Even if eligible, sometimes "hide" the link to avoid overly dense networks.
                if (Rand.Value > ChanceHideSpanningTreeLink)
                    list.Add(prospective);

                // If this link would connect two components, union them (regardless of whether we hid the visual link).
                if (differentGroups)
                {
                    Connectedness connectedness = new Connectedness();

                    // Attach both roots to a new parent node (effectively merging the two sets).
                    connectednessList[prospective.indexA].Group().parent = connectedness;
                    connectednessList[prospective.indexB].Group().parent = connectedness;
                }
            }
        }

        return list;
    }

    private void DrawLinksOnWorld(
        List<Link> linkFinal,
        List<PlanetTile> indexToTile)
    {
        // For each final link, compute an actual path and lay tunnel overlays along each step.
        foreach (Link link in linkFinal)
        {
            // Find the best path between the endpoints using the layer's pather.
            WorldPath path = TunnelGenData.Instance.Pather.FindPath(indexToTile[link.indexA], indexToTile[link.indexB], null);

            List<PlanetTile> nodesReversed = path.NodesReversed;
            TunnelDef tunnelDef = DefDatabase<TunnelDef>.AllDefsListForReading
                .RandomElementWithFallback();

            // Lay the overlay for each edge along the path (tile i -> tile i+1).
            for (int index = 0; index < nodesReversed.Count - 1; ++index)
                TunnelGenData.Instance.OverlayTunnel(nodesReversed[index], nodesReversed[index + 1], tunnelDef);

            path.ReleaseToPool();
        }
    }

    private struct Link
    {
        // Path cost between endpoints (used as a "distance" metric for sorting).
        public float distance;

        // Indices into the endpoint list.
        public int indexA;
        public int indexB;
    }

    private class Connectedness
    {
        public Connectedness parent;

        public Connectedness Group()
        {
            // If no parent, this node is the root; otherwise walk upward.
            return parent == null ? this : parent.Group();
        }
    }
}
