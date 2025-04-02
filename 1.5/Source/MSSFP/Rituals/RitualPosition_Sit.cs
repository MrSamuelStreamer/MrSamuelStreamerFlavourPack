using RimWorld;
using Verse;

namespace MSSFP.Rituals;

public class RitualPosition_Sit : RitualPosition
{
    [NoTranslate]
    public string ofPawn;

    public override PawnStagePosition GetCell(IntVec3 spot, Pawn p, LordJob_Ritual ritual)
    {
        Pawn pawn = ofPawn == null ? p : ritual.assignments.FirstAssignedPawn(ofPawn);

        if (ritual.selectedTarget != null && ritual.selectedTarget.Thing.def.defName == "MSSFP_CuckChair")
            return new PawnStagePosition(ritual.selectedTarget.Thing.Position, null, Rot4.Invalid, highlight);
        return new PawnStagePosition(IntVec3.Invalid, null, Rot4.Invalid, highlight);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ofPawn, "ofPawn");
    }
}
