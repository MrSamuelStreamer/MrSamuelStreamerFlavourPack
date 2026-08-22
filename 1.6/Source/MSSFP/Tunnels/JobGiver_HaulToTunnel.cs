using Verse;
using Verse.AI;

namespace MSSFP.Tunnels;

public class JobGiver_HaulToTunnel : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        Building_Tunnel tunnel = pawn.mindState?.duty?.focus.Thing as Building_Tunnel;
        if (tunnel != null && TunnelUtilities.HasJobOnTunnel(pawn, tunnel))
        {
            return TunnelUtilities.JobOnTunnel(pawn, tunnel);
        }
        return null;
    }
}
