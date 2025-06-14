using Verse;
using Verse.AI;

namespace MSSFP.Comps;

public class CompLeaveMap : ThingComp
{
    public bool isFirstTick = true;
    public Job job;

    public override void CompTick()
    {
        if (isFirstTick)
        {
            isFirstTick = false;
            return;
        }

        if (parent.HasComp<CompLeaveMap>())
        {
            parent.AllComps.Remove(this);
            if (job != null)
                ((parent as Pawn)!).jobs.StartJob(job, JobCondition.InterruptForced);
        }
    }
}
