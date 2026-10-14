using RimWorld;
using Verse;

namespace MSSFP.Projectiles;

public class Projectile_Heals : Projectile
{
    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        if (
            hitThing is Pawn pawn
            && HealthUtility.TryGetWorstHealthCondition(pawn, out Hediff _, out BodyPartRecord _)
        )
        {
            TaggedString details = HealthUtility.FixWorstHealthCondition(pawn);
            Messages.Message(details, pawn, MessageTypeDefOf.NeutralEvent);
        }

        base.Impact(hitThing, blockedByShield);
    }
}
