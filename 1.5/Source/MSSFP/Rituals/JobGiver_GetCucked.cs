using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Rituals;

public class JobGiver_GetCucked : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        LordJob_Ritual lordRitual = pawn.Map.lordManager.lords.FirstOrDefault(lord => lord.LordJob is LordJob_Ritual && lord.ownedPawns.Contains(pawn)).LordJob as LordJob_Ritual;

        if (lordRitual == null || lordRitual.selectedTarget == null || lordRitual.selectedTarget.Thing.def.defName != "MSSFP_CuckChair")
            return null;

        if (pawn.Position != lordRitual.selectedTarget.Thing.Position)
        {
            Job job = JobMaker.MakeJob(JobDefOf.Goto, lordRitual.selectedTarget.Thing.Position);
            job.locomotionUrgency = LocomotionUrgency.Jog;
            return job;
        }
        return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("MSSFP_Sit"), 120);
    }
}
