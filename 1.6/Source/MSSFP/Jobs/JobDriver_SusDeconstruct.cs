using System.Collections.Generic;
using MSSFP.Comps;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Jobs;

public class JobDriver_SusDeconstruct : JobDriver_Deconstruct
{
    private float susWorkLeft;
    private float susTotalNeededWork;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref susWorkLeft, "susWorkLeft");
        Scribe_Values.Look(ref susTotalNeededWork, "susTotalNeededWork");
    }

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
        this.FailOnThingMissingDesignation(TargetIndex.A, DesignationDefOf.Deconstruct);
        this.FailOnForbidden(TargetIndex.A);
        this.FailOn(
            delegate
            {
                CompExplosive compExplosive;
                return Building.TryGetComp(out compExplosive) && compExplosive.wickStarted;
            }
        );
        yield return Toils_Goto.GotoThing(
            TargetIndex.A,
            (Target is Building_Trap) ? PathEndMode.OnCell : PathEndMode.Touch,
            false
        );
        Toil doWork = ToilMaker
            .MakeToil("MakeNewToils")
            .FailOnDestroyedNullOrForbidden(TargetIndex.A)
            .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
        doWork.initAction = delegate
        {
            susTotalNeededWork = TotalNeededWork;
            susWorkLeft = susTotalNeededWork;
        };
        doWork.tickAction = delegate
        {
            susWorkLeft -= pawn.GetStatValue(StatDefOf.ConstructionSpeed, true, -1) * 1.7f;
            if (susWorkLeft <= 0f)
            {
                doWork.actor.jobs.curDriver.ReadyForNextToil();
            }
        };
        doWork.defaultCompleteMode = ToilCompleteMode.Never;

        doWork.WithProgressBar(
            TargetIndex.A,
            () => 1f - susWorkLeft / susTotalNeededWork,
            false,
            -0.5f,
            false
        );
        doWork.activeSkill = () => SkillDefOf.Construction;
        yield return doWork;
    }
}
