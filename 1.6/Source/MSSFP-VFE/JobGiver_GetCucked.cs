using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.VFE;

public class JobGiver_GetCucked : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        LordJob_Ritual lordRitual =
            pawn.Map.lordManager.lords.FirstOrDefault(lord =>
                lord.LordJob is LordJob_Ritual && lord.ownedPawns.Contains(pawn)
            ).LordJob as LordJob_Ritual;

        if (lordRitual == null || lordRitual.selectedTarget == null)
            return null;

        Building_CuckChair chair;
        if (lordRitual.selectedTarget.Thing is Building_CuckChair)
        {
            chair = lordRitual.selectedTarget.Thing as Building_CuckChair;
        }
        else
        {
            List<Building_CuckChair> chairs = GenRadial
                .RadialCellsAround(lordRitual.selectedTarget.Thing.Position, 20, true)
                .SelectMany(pos => lordRitual.selectedTarget.Thing.Map.thingGrid.ThingsAt(pos))
                .OfType<Building_CuckChair>()
                .ToList();

            chair = chairs.RandomElementWithFallback();
        }

        if (chair == null)
            return null;

        if (pawn.Position != chair.Position)
        {
            Job job = JobMaker.MakeJob(JobDefOf.Goto, chair.Position);
            job.locomotionUrgency = LocomotionUrgency.Jog;
            return job;
        }
        return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("MSSFP_Sit"), 120);
    }
}
