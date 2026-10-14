using RimWorld;
using Verse;

namespace MSSFP.Thoughts;

public class ThoughtWorker_Frogge : ThoughtWorker
{
    private const float Radius = 15f;

    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        return p.Spawned
            && p.Map.listerThings.ThingsOfDef(MSSFPDefOf.MSSFP_Frogge)
                .Any(thing => p.Position.InHorDistOf(thing.Position, Radius));
    }
}
