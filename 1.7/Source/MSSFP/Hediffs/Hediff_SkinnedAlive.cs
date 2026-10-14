using Verse;

namespace MSSFP.Hediffs;

/// <summary>
/// Permanent hediff applied when the human processor ejects a survivor.
/// The def only sets skinColorOverride, so vanilla will not refresh cached
/// body and head graphics on add or remove. PostAdd and PostRemoved dirty the
/// render tree so Harmony_PeeledPawn runs again.
/// </summary>
public class Hediff_SkinnedAlive : HediffWithComps
{
    public override void PostAdd(DamageInfo? dinfo)
    {
        base.PostAdd(dinfo);
        pawn.Drawer.renderer.SetAllGraphicsDirty();
    }

    public override void PostRemoved()
    {
        base.PostRemoved();
        pawn.Drawer.renderer.SetAllGraphicsDirty();
    }
}
