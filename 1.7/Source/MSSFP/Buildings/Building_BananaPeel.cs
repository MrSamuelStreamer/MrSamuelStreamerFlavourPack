using RimWorld;
using Verse;
using Verse.Sound;

namespace MSSFP.Buildings;

public class Building_BananaPeel : Building_Trap
{
    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (respawningAfterLoad || BeingTransportedOnGravship)
            return;

        SoundDefOf.TrapArm.PlayOneShot(new TargetInfo(Position, map));
    }

    protected override void SpringSub(Pawn p)
    {
        if (Spawned)
            MSSFPDefOf.MSSFP_BananaFall.PlayOneShot(new TargetInfo(Position, Map));

        p?.stances?.stunner?.StunFor(600, this, false);
    }
}
