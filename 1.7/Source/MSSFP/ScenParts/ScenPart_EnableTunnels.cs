using RimWorld;
using Verse;

namespace MSSFP.ScenParts;

/// <summary>
/// Scenario-editor opt-in for the tunnel system. No editable fields yet — presence
/// on the scenario is itself the toggle, read via <c>Find.Scenario.AllParts</c> at
/// world-gen time (commit 2).
/// </summary>
public class ScenPart_EnableTunnels : ScenPart
{
    public override void DoEditInterface(Listing_ScenEdit listing) { }

    public override string Summary(Scenario scen)
    {
        return "MSSFP_Tunnels_ScenPart_Summary".Translate();
    }
}
