using MSSFP.ReversePickpocket;
using Verse;

namespace MSSFP.Hediffs;

/// <summary>
/// Planted explosive with a short fuse. Explodes when the fuse runs out, or at
/// once if the carrier dies first. Cancelled silently if the carrier is no longer
/// on a map. Never merges, so two plants mean two explosions.
/// </summary>
public class Hediff_ReversePickpocketFuse : HediffWithComps
{
    private int ticksLeft = ReversePickpocketUtility.FuseTicks;
    private ThingDef explosiveDef;
    private Pawn instigator;
    private bool finished;

    public void Arm(ThingDef def, Pawn planter)
    {
        explosiveDef = def;
        instigator = planter;
        ticksLeft = ReversePickpocketUtility.FuseTicks;
    }

    public override bool ShouldRemove => finished || explosiveDef == null;

    public override string LabelInBrackets =>
        ((float)ticksLeft / GenTicks.TicksPerRealSecond).ToString("0.0") + "s";

    public override bool TryMergeWith(Hediff other) => false;

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (finished)
            return;
        ticksLeft -= delta;
        if (ticksLeft <= 0)
            Detonate();
    }

    public override void Notify_PawnKilled()
    {
        base.Notify_PawnKilled();
        Detonate();
    }

    private void Detonate()
    {
        // Set first: the blast itself may kill the carrier and re-enter via Notify_PawnKilled.
        if (finished)
            return;
        finished = true;
        if (pawn.MapHeld == null)
            return;
        ReversePickpocketUtility.Detonate(explosiveDef, pawn.PositionHeld, pawn.MapHeld, instigator);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksLeft, "ticksLeft", ReversePickpocketUtility.FuseTicks);
        Scribe_Values.Look(ref finished, "finished");
        Scribe_Defs.Look(ref explosiveDef, "explosiveDef");
        Scribe_References.Look(ref instigator, "instigator");
    }
}
