using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Tunnels;

public class WorkGiver_HaulToTunnel : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.MapPortal);

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override Danger MaxPathDanger(Pawn pawn)
    {
        return Danger.Deadly;
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        Building_Tunnel tunnel = t as Building_Tunnel;
        return TunnelUtilities.HasJobOnTunnel(pawn, tunnel);
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        Building_Tunnel tunnel = t as Building_Tunnel;
        return TunnelUtilities.JobOnTunnel(pawn, tunnel);
    }
}
