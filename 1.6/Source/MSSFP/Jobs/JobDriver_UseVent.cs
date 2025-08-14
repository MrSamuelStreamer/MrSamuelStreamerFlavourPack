using System.Collections.Generic;
using System.Linq;
using MSSFP.ModExtensions;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Jobs;

public class JobDriver_UseVent : JobDriver
{
    public VentModExtension Extension =>
        MSSFPDefOf.MSSFP_UseVent.GetModExtension<VentModExtension>();

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

        Toil enterVent = new Toil
        {
            initAction = () =>
            {
                // Find random other vent/geyser
                Thing randomVent = Map
                    .listerThings.AllThings.Where(t => Extension.VentableThings.Contains(t.def))
                    .RandomElementWithFallback();
                if (randomVent != null)
                {
                    if (randomVent != job.targetA.Thing)
                    {
                        pawn.Position = randomVent.Position;
                        pawn.Notify_Teleported();
                    }
                }
            },
        };
        yield return enterVent;
    }
}
