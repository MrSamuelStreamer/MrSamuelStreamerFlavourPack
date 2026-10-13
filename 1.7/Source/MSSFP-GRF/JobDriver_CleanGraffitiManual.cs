using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MSSFP.GRF;

/// <summary>
/// Cleans one piece of graffiti, on a player's explicit order.
///
/// Deliberately NOT Graffiti Mod's own <c>JobDriver_CleanGraffiti</c>. That one drives off a
/// target QUEUE and carries a <c>JumpIfOutsideHomeArea</c> guard, both of which suit a
/// WorkGiver sweeping a room but not a player pointing at one wall: outside the home area it
/// would silently do nothing, which reads as a bug when you have just ordered it.
///
/// Nothing here consults the "graffiti is permanent" setting. That setting suppresses
/// automatic work by patching <c>WorkGiver.HasJobOnThing</c>, and a player-issued job never
/// passes through a WorkGiver, so a manual order bypasses it by construction.
/// </summary>
public class JobDriver_CleanGraffitiManual : JobDriver
{
    private const TargetIndex GraffitiInd = TargetIndex.A;

    private float cleaningWorkDone;
    private float totalCleaningWorkRequired;

    private Thing Graffiti => job.GetTarget(GraffitiInd).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(job.GetTarget(GraffitiInd), job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(GraffitiInd);

        yield return Toils_Goto.GotoThing(GraffitiInd, PathEndMode.Touch)
            .FailOnDespawnedNullOrForbidden(GraffitiInd);

        Toil clean = ToilMaker.MakeToil(nameof(JobDriver_CleanGraffitiManual));
        clean.initAction = delegate
        {
            cleaningWorkDone = 0f;
            // Their def carries a filth props block even though the thing is a Building_Art,
            // so the authored work value is still the right number to honour here.
            totalCleaningWorkRequired = Graffiti?.def?.filth?.cleaningWorkToReduceThickness ?? 1200f;
        };
        clean.tickIntervalAction = delegate(int delta)
        {
            Thing graffiti = Graffiti;
            if (graffiti == null || graffiti.Destroyed)
            {
                ReadyForNextToil();
                return;
            }

            cleaningWorkDone += pawn.GetStatValue(StatDefOf.CleaningSpeed) * delta;
            if (cleaningWorkDone < totalCleaningWorkRequired)
            {
                return;
            }

            graffiti.Destroy(DestroyMode.Vanish);
            pawn.records.Increment(RecordDefOf.MessesCleaned);
            ReadyForNextToil();
        };
        clean.defaultCompleteMode = ToilCompleteMode.Never;
        clean.WithEffect(EffecterDefOf.Clean, GraffitiInd);
        clean.WithProgressBar(GraffitiInd, () => cleaningWorkDone / totalCleaningWorkRequired);
        clean.PlaySustainerOrSound(() =>
        {
            SoundDef sound = Graffiti?.def?.filth?.cleaningSound;
            return sound.NullOrUndefined() ? SoundDefOf.Interact_CleanFilth : sound;
        });
        clean.FailOnDespawnedNullOrForbidden(GraffitiInd);
        yield return clean;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref cleaningWorkDone, "MSSFP_GRF_cleaningWorkDone");
        Scribe_Values.Look(ref totalCleaningWorkRequired, "MSSFP_GRF_totalCleaningWorkRequired");
    }
}
