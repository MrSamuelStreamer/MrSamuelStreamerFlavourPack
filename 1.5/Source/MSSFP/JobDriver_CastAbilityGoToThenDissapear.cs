using System;
using System.Collections.Generic;
using MSSFP.Comps;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP;

public class JobDriver_CastAbilityGoToThenDissapear: JobDriver_CastAbilityGoTo
{
    protected override IEnumerable<Toil> MakeNewToils()
    {
        AddFinishAction((JobCondition c) =>
        {
            // SkipUtility.SkipTo(pawn, pawn.Position, null);
            ThingComp thingComp = (ThingComp) Activator.CreateInstance(typeof(CompSkipDespawnNextTick));
            thingComp.parent = pawn;
            pawn.AllComps.Add(thingComp);
            thingComp.Initialize(new CompProperties());
        });
        this.FailOnDespawnedOrNull<JobDriver_CastAbilityGoTo>(TargetIndex.A);
        yield return Toils_Goto.Goto(TargetIndex.B, PathEndMode.OnCell);
        yield return Toils_Combat.CastVerb(TargetIndex.A, TargetIndex.B, false);
    }
}
