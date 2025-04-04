using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.VFE;

public class RitualPosition_Sit : RitualPosition
{
    [NoTranslate]
    public string ofPawn;

    public override PawnStagePosition GetCell(IntVec3 spot, Pawn p, LordJob_Ritual ritual)
    {
        // Pawn pawn = ofPawn == null ? p : ritual.assignments.FirstAssignedPawn(ofPawn);

        if (ritual.selectedTarget != null && ritual.selectedTarget.Thing.def.defName == "MSSFP_CuckChair")
            return new PawnStagePosition(ritual.selectedTarget.Thing.Position, null, Rot4.Invalid, highlight);

        List<Building_CuckChair> chairs = GenRadial
            .RadialCellsAround(ritual.selectedTarget.Thing.Position, 20, true)
            .SelectMany(pos => ritual.selectedTarget.Thing.Map.thingGrid.ThingsAt(pos))
            .OfType<Building_CuckChair>()
            .ToList();

        if (chairs.Count > 0)
        {
            return new PawnStagePosition(chairs.RandomElement().Position, null, Rot4.Invalid, highlight);
        }
        return new PawnStagePosition(IntVec3.Invalid, null, Rot4.Invalid, highlight);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ofPawn, "ofPawn");
    }
}
