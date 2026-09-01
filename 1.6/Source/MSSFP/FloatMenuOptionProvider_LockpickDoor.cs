using System;
using MSSFP.Lockpicking;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP;

/// <summary>
/// Right-click "Lockpick" on hostile-faction doors the selected pawn cannot open.
/// </summary>
public class FloatMenuOptionProvider_LockpickDoor : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => false;

    protected override bool Multiselect => false;

    protected override bool RequiresManipulation => true;

    protected override bool MechanoidCanDo => false;

    protected override bool AppliesInt(FloatMenuContext context)
    {
        return LockpickUtility.Enabled;
    }

    public override bool TargetThingValid(Thing thing, FloatMenuContext context)
    {
        return base.TargetThingValid(thing, context) && thing is Building_Door;
    }

    protected override FloatMenuOption GetSingleOptionFor(
        Thing clickedThing,
        FloatMenuContext context
    )
    {
        if (clickedThing is not Building_Door door)
            return null;

        Pawn pawn = context.FirstSelectedPawn;
        if (pawn == null)
            return null;

        if (!LockpickUtility.IsLockpickTarget(door, pawn))
            return null;

        if (!pawn.CanReach(door, PathEndMode.Touch, Danger.Deadly))
        {
            return new FloatMenuOption(
                "MSSFP_CannotLockpick".Translate(door.LabelShort)
                    + ": "
                    + "NoPath".Translate().CapitalizeFirst(),
                null
            );
        }

        if (!pawn.CanReserve(door))
        {
            Pawn reserver = door.Map.reservationManager.FirstRespectedReserver(door, pawn);
            string reason =
                reserver != null
                    ? "ReservedBy".Translate(reserver.LabelShort, reserver)
                    : "Reserved".Translate();
            return new FloatMenuOption(
                "MSSFP_CannotLockpick".Translate(door.LabelShort) + ": " + reason,
                null
            );
        }

        return FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(
                "MSSFP_Lockpick".Translate(door.LabelShort),
                (Action)(
                    () =>
                    {
                        Job job = JobMaker.MakeJob(MSSFPDefOf.MSSFP_LockpickDoor, door);
                        job.playerForced = true;
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }
                )
            ),
            pawn,
            door
        );
    }
}
