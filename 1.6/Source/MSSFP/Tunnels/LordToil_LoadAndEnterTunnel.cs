using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace MSSFP.Tunnels;

public class LordToil_LoadAndEnterTunnel : LordToil
{
    public Building_Tunnel tunnel;

    public override bool AllowSatisfyLongNeeds => false;

    public LordToil_LoadAndEnterTunnel(Building_Tunnel tunnel) => this.tunnel = tunnel;

    public override void UpdateAllDuties()
    {
        foreach (var t in lord.ownedPawns)
            t.mindState.duty = new PawnDuty(MSSFPDefOf.MSSFP_LoadAndEnterTunnel)
            {
                focus = new LocalTargetInfo(tunnel),
            };
    }
}
