using Verse;

namespace MSSFP.Hediffs;

public class HediffComp_ExplodeOnDeath : HediffComp
{
    public HediffCompProperties_ExplodeOnDeath Props => (HediffCompProperties_ExplodeOnDeath)props;

    public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
    {
        base.Notify_PawnDied(dinfo, culprit);
        Props.effecter?.SpawnAttached(parent.pawn, parent.pawn.Map);
    }
}
