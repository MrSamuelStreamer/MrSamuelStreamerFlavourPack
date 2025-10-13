using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Comps;

public class CompHaunted : ThingComp
{
    CompProperties_Haunted Props => (CompProperties_Haunted)props;

    Pawn pawn =>
        parent is Apparel { ParentHolder: Pawn_ApparelTracker { pawn: not null } tracker }
            ? tracker.pawn
            : null;

    public override void CompDrawWornExtras()
    {
        base.CompDrawWornExtras();
        ModLog.Debug("CompHaunted.CompDrawWornExtras:Start");
        if (!MSSFPMod.settings.ShowHaunts)
            return;
        if (pawn == null || Props.onlyRenderWhenDrafted && pawn.drafter is not { Drafted: true })
            return;

        Vector3 offset = new();

        if (Props.offsets != null)
        {
            offset =
                Props.offsets.Count == 4 ? Props.offsets[parent.Rotation.AsInt] : Props.offsets[0];
        }

        Vector3 pos =
            new Vector3(pawn.DrawPos.x, AltitudeLayer.PawnUnused.AltitudeFor(), pawn.DrawPos.z)
            + offset;
        Props.graphicData.Graphic.Draw(pos, pawn.Rotation, pawn);
        ModLog.Debug("CompHaunted.CompDrawWornExtras:End");
    }
}
