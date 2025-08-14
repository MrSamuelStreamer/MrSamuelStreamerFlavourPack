using System;
using System.Collections.Generic;
using MSSFP.Comps;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Jobs;

public class JobDriver_SusDoBill : JobDriver_DoBill
{
    protected override IEnumerable<Toil> MakeNewToils()
    {
        if (!pawn.HasComp<CompImpostor>() || !pawn.GetComp<CompImpostor>().IsSus)
        {
            foreach (Toil makeNewToil in base.MakeNewToils())
            {
                yield return makeNewToil;
            }
            yield break;
        }

        AddEndCondition(
            delegate
            {
                Thing thing = GetActor().jobs.curJob.GetTarget(TargetIndex.A).Thing;
                if (thing is Building && !thing.Spawned)
                {
                    return JobCondition.Incompletable;
                }
                return JobCondition.Ongoing;
            }
        );
        this.FailOnBurningImmobile(TargetIndex.A);
        this.FailOn(
            delegate
            {
                if (job.GetTarget(TargetIndex.A).Thing is IBillGiver billGiver)
                {
                    if (job.bill.DeletedOrDereferenced)
                    {
                        return true;
                    }
                    if (!billGiver.CurrentlyUsableForBills())
                    {
                        return true;
                    }
                }
                return false;
            }
        );
        Toil gotoBillGiver = Toils_Goto.GotoThing(
            TargetIndex.A,
            PathEndMode.InteractionCell,
            false
        );
        Toil toil = ToilMaker.MakeToil("MakeNewToils");
        toil.initAction = delegate
        {
            if (job.targetQueueB is { Count: 1 })
            {
                if (job.targetQueueB[0].Thing is UnfinishedThing unfinishedThing)
                {
                    unfinishedThing.BoundBill = (Bill_ProductionWithUft)job.bill;
                }
            }
            job.bill.Notify_DoBillStarted(pawn);
        };
        yield return toil;
        yield return Toils_Jump.JumpIf(
            gotoBillGiver,
            () => job.GetTargetQueue(TargetIndex.B).NullOrEmpty()
        );
        foreach (
            Toil toil2 in CollectIngredientsToils(
                TargetIndex.B,
                TargetIndex.A,
                TargetIndex.C,
                false,
                true,
                BillGiver is Building_WorkTableAutonomous
            )
        )
        {
            yield return toil2;
        }
        yield return gotoBillGiver;
        yield return Toils_Recipe.MakeUnfinishedThingIfNeeded();
        yield return DoFakeRecipeWork()
            .FailOnDespawnedNullOrForbiddenPlacedThings(TargetIndex.A)
            .FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
        yield return Toils_Recipe.CheckIfRecipeCanFinishNow();
    }

    public static Toil DoFakeRecipeWork()
    {
        Toil toil = ToilMaker.MakeToil(nameof(DoFakeRecipeWork));
        toil.initAction = (Action)(
            () =>
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                JobDriver_DoBill curDriver = (JobDriver_DoBill)actor.jobs.curDriver;
                Thing thing = curJob.GetTarget(TargetIndex.B).Thing;
                UnfinishedThing unfinishedThing = thing as UnfinishedThing;
                if (unfinishedThing is { Initialized: true })
                {
                    curDriver.workLeft = unfinishedThing.workLeft;
                }
                else
                {
                    curDriver.workLeft = curJob.bill.GetWorkAmount(thing);
                    if (unfinishedThing != null)
                        unfinishedThing.workLeft = !unfinishedThing.debugCompleted
                            ? curDriver.workLeft
                            : (curDriver.workLeft = 0.0f);
                }
                curDriver.billStartTick = Find.TickManager.TicksGame;
                curDriver.ticksSpentDoingRecipeWork = 0;
            }
        );
        toil.tickAction = (Action)(
            () =>
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;
                JobDriver_DoBill curDriver = (JobDriver_DoBill)actor.jobs.curDriver;
                UnfinishedThing unfinishedThing =
                    curJob.GetTarget(TargetIndex.B).Thing as UnfinishedThing;
                if (unfinishedThing is { Destroyed: true })
                {
                    actor.jobs.EndCurrentJob(JobCondition.Incompletable);
                }
                else
                {
                    ++curDriver.ticksSpentDoingRecipeWork;

                    float workDone =
                        curJob.RecipeDef.workSpeedStat == null
                            ? 1f
                            : actor.GetStatValue(curJob.RecipeDef.workSpeedStat);
                    if (
                        curJob.RecipeDef.workTableSpeedStat != null
                        && curDriver.BillGiver is Building_WorkTable billGiver2
                    )
                        workDone *= billGiver2.GetStatValue(curJob.RecipeDef.workTableSpeedStat);
                    curDriver.workLeft -= workDone;
                    if (unfinishedThing != null)
                        unfinishedThing.workLeft = !unfinishedThing.debugCompleted
                            ? curDriver.workLeft
                            : (curDriver.workLeft = 0.0f);

                    if (curDriver.workLeft <= 0.0)
                    {
                        curDriver.ReadyForNextToil();
                    }
                    else
                    {
                        if (!curJob.bill.recipe.UsesUnfinishedThing)
                            return;
                        int ticksWorked = Find.TickManager.TicksGame - curDriver.billStartTick;
                        if (ticksWorked < 3000 || ticksWorked % 1000 != 0)
                            return;
                        actor.jobs.CheckForJobOverride();
                    }
                }
            }
        );
        toil.defaultCompleteMode = ToilCompleteMode.Never;
        toil.WithEffect(
            (Func<EffecterDef>)(() => toil.actor.CurJob.bill.recipe.effectWorking),
            TargetIndex.A
        );
        toil.PlaySustainerOrSound(
            (Func<SoundDef>)(() => toil.actor.CurJob.bill.recipe.soundWorking)
        );
        toil.WithProgressBar(
            TargetIndex.A,
            (Func<float>)(
                () =>
                {
                    Pawn actor = toil.actor;
                    Job curJob = actor.CurJob;
                    Thing thing5 = curJob.GetTarget(TargetIndex.B).Thing;
                    return (float)(
                        1.0
                        - ((JobDriver_DoBill)actor.jobs.curDriver).workLeft
                            / (
                                curJob.bill is not Bill_Mech { State: FormingState.Formed }
                                    ? curJob.bill.recipe.WorkAmountTotal(thing5)
                                    : 300.0
                            )
                    );
                }
            )
        );
        return toil;
    }
}
