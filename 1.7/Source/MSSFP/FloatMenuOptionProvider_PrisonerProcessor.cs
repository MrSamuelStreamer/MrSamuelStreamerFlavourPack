using MSSFP.Comps;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP;

/// <summary>
/// Right-click a colony prisoner to carry them to the nearest powered, empty processor.
/// </summary>
public class FloatMenuOptionProvider_PrisonerProcessor : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;
    protected override bool RequiresManipulation => true;

    protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
    {
        Pawn actor = context.FirstSelectedPawn;

        if (!actor.IsColonistPlayerControlled)
            return null;
        if (clickedPawn == actor)
            return null;
        if (!clickedPawn.RaceProps.Humanlike || clickedPawn.Dead || !clickedPawn.IsPrisonerOfColony)
            return null;

        string label = "MSSFP_PrisonerProcessor_Assign".Translate(clickedPawn.LabelShortCap);

        if (!actor.CanReserveAndReach(clickedPawn, PathEndMode.Touch, Danger.Deadly))
            return new FloatMenuOption("MSSFP_PrisonerProcessor_NoPath".Translate(label), null);

        Thing processor = ClosestAvailableProcessor(actor, clickedPawn, out string reason);
        if (processor == null)
        {
            string why = reason ?? (string)"MSSFP_PrisonerProcessor_NoProcessor".Translate();
            return new FloatMenuOption("MSSFP_PrisonerProcessor_Unavailable".Translate(label, why), null);
        }

        void Action()
        {
            Job job = JobMaker.MakeJob(MSSFPDefOf.MSSFP_CarryPawnToPrisonerProcessor, clickedPawn, processor);
            job.count = 1;
            actor.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, Action), actor, clickedPawn);
    }

    private static Thing ClosestAvailableProcessor(Pawn actor, Pawn target, out string failReason)
    {
        Thing bestThing = null;
        float bestDistanceSquared = float.MaxValue;
        failReason = null;

        PrisonerProcessorTracker tracker = actor.Map.GetComponent<PrisonerProcessorTracker>();
        if (tracker == null)
            return null;

        foreach (CompPrisonerProcessor comp in tracker.Processors)
        {
            if (!comp.CanAcceptPawn(target, out failReason))
                continue;

            Thing thing = comp.ParentThing;
            if (!actor.CanReserveAndReach(thing, PathEndMode.InteractionCell, Danger.Deadly))
                continue;

            float distanceSquared = actor.Position.DistanceToSquared(thing.Position);
            if (distanceSquared >= bestDistanceSquared)
                continue;

            bestThing = thing;
            bestDistanceSquared = distanceSquared;
        }

        return bestThing;
    }
}
