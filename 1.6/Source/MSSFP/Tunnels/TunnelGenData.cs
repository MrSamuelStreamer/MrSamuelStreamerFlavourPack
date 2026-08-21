using RimWorld.Planet;
using Verse;

namespace MSSFP.Tunnels;

/// <summary>
/// Sticky per-save snapshot of whether the tunnel system is enabled for this world.
/// Set once during world-gen (commit 2) and never re-derived from live settings, so
/// flipping the mod settings mid-game never retroactively enables/disables an
/// existing save. Also carries the "fired once per game" flags for tunnel-specific
/// story events, since the DFP GameComponent equivalent was folded into this
/// WorldComponent for MSSFP.
/// </summary>
public class TunnelGenData(World world) : WorldComponent(world)
{
    public bool tunnelsEnabledForSave;
    public bool wasSet;
    public bool ossuaryFired;
    public bool brendaFired;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref tunnelsEnabledForSave, "tunnelsEnabledForSave");
        Scribe_Values.Look(ref wasSet, "wasSet");
        Scribe_Values.Look(ref ossuaryFired, "ossuaryFired");
        Scribe_Values.Look(ref brendaFired, "brendaFired");
    }
}
