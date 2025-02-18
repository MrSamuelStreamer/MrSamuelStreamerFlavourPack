using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Buildings;

public class Building_FirefoamTurretGun: Building_TurretGun
{
    public override LocalTargetInfo TryFindNewTarget()
    {
        IAttackTargetSearcher searcher = this;
        Faction faction = searcher.Thing.Faction;
        float range = AttackVerb.verbProps.range;

        if (Rand.Value < 0.5 && faction.IsPlayer && Map.listerThings.AllThings.Where(x =>
            {
                float num = AttackVerb.verbProps.EffectiveMinRange((LocalTargetInfo) x, this);
                float squared = x.Position.DistanceToSquared(Position);
                return squared > num * (double) num && squared < range * (double) range;
            }).Where(IsValidTarget).TryRandomElement(out Thing result))
            return (LocalTargetInfo) result;

        TargetScanFlags flags = TargetScanFlags.NeedAutoTargetable | TargetScanFlags.NeedNotUnderThickRoof;

        return (LocalTargetInfo) (Thing) AttackTargetFinder.BestShootTargetFromCurrentPosition(searcher, flags, IsValidTarget);
    }

    private bool IsValidTarget(Thing t)
    {
        if (t is Fire) return true;
        Thing attachment = t?.GetAttachment(ThingDefOf.Fire);
        return attachment != null;
    }

}
