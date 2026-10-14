using Verse;
using Verse.AI.Group;

namespace MSSFP.Tunnels;

public class LordJob_LoadAndEnterTunnel : LordJob
{
    public Building_Tunnel tunnel;

    public override bool AllowStartNewGatherings => false;

    public override bool AllowStartNewRituals => true;

    public override bool AddFleeToil => false;

    public LordJob_LoadAndEnterTunnel()
    {
    }

    public LordJob_LoadAndEnterTunnel(Building_Tunnel tunnel) => this.tunnel = tunnel;

    public override void ExposeData() => Scribe_References.Look(ref tunnel, "tunnel");

    public override StateGraph CreateGraph()
    {
        StateGraph graph = new StateGraph();
        graph.StartingToil = new LordToil_LoadAndEnterTunnel(tunnel);
        graph.AddToil(new LordToil_End());
        return graph;
    }
}
