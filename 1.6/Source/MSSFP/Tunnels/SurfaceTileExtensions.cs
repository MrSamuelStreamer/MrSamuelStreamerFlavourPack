using System.Collections.Generic;
using RimWorld.Planet;

namespace MSSFP.Tunnels;

public static class SurfaceTileExtensions
{
    extension(SurfaceTile tile)
    {
        public List<TunnelGenData.TunnelLink> potentialTunnels
        {
            get
            {
                if (!TunnelGenData.Instance.potentialTunnels.ContainsKey(tile))
                {
                    TunnelGenData.Instance.potentialTunnels.Add(tile, []);
                }

                return TunnelGenData.Instance.potentialTunnels[tile];
            }
        }
    }
}
