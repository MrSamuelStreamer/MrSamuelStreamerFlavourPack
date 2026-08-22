using System.Collections.Generic;
using RimWorld.Planet;

namespace MSSFP.Tunnels;

public static class SurfaceTileExtensions
{
    private static readonly List<TunnelGenData.TunnelLink> EmptyLinks = [];

    extension(SurfaceTile tile)
    {
        /// <summary>
        /// Read-only lookup. Never mutates TunnelGenData.potentialTunnels — a tile with
        /// no entry simply has no links yet. Use TunnelGenData.OverlayTunnel to create entries.
        /// </summary>
        public List<TunnelGenData.TunnelLink> potentialTunnels =>
            TunnelGenData.Instance.potentialTunnels.TryGetValue(tile, out List<TunnelGenData.TunnelLink> links)
                ? links
                : EmptyLinks;
    }
}
