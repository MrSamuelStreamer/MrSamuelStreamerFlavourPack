using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.GRF;

/// <summary>
/// Right-click a piece of graffiti with a pawn selected to order that one cleaned.
///
/// Discovered by reflection (<c>FloatMenuMakerMap</c> instantiates every non-abstract
/// <see cref="FloatMenuOptionProvider"/> subclass at startup), so no def registration.
///
/// This is the manual escape hatch for the "graffiti is permanent" setting: that setting only
/// suppresses automatic assignment via <c>WorkGiver.HasJobOnThing</c>, and a player order does
/// not go through a WorkGiver. The option is therefore offered whether or not the setting is on.
/// </summary>
public class FloatMenuOptionProvider_CleanGraffiti : FloatMenuOptionProvider
{
    protected override bool Drafted => false;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool RequiresManipulation => true;

    public override bool TargetThingValid(Thing thing, FloatMenuContext context) =>
        GraffitiUtils.IsGraffiti(thing) && base.TargetThingValid(thing, context);

    protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
    {
        if (!GraffitiUtils.IsGraffiti(clickedThing))
        {
            return null;
        }

        Pawn pawn = context.FirstSelectedPawn;
        string label = "MSSFP_GRF_CleanGraffiti".Translate();

        if (!pawn.CanReach(clickedThing, PathEndMode.Touch, Danger.Deadly))
        {
            return new FloatMenuOption(label + ": " + "NoPath".Translate().CapitalizeFirst(), null);
        }

        if (!pawn.CanReserve(clickedThing))
        {
            return new FloatMenuOption(
                label + ": " + "Reserved".Translate().CapitalizeFirst(),
                null
            );
        }

        return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(
                label,
                delegate
                {
                    Job job = JobMaker.MakeJob(GraffitiDefOf.MSSFP_CleanGraffitiManual, clickedThing);
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }
            ),
            pawn,
            clickedThing
        );
    }
}
