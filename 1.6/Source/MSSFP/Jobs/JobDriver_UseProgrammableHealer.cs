using System.Collections.Generic;
using MSSFP.Dialogs;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace MSSFP.Jobs;

/// <summary>
/// User walks to serum (TargetA), picks it up, walks to target pawn (TargetB),
/// force-waits target for the prep duration, then opens the Programmable Healer
/// picker dialog. The dialog handles destroy-on-confirm; cancel preserves the serum
/// (dropped at the user's feet on dialog open).
/// </summary>
public class JobDriver_UseProgrammableHealer : JobDriver
{
    private const int PrepTicks = 600;
    private const TargetIndex SerumInd = TargetIndex.A;
    private const TargetIndex TargetPawnInd = TargetIndex.B;

    private Thing Serum => job.GetTarget(SerumInd).Thing;
    private Pawn TargetPawn => (Pawn)job.GetTarget(TargetPawnInd).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(Serum, job, 1, -1, null, errorOnFailed)
            && pawn.Reserve(TargetPawn, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        // Per-toil FailOn checks only — a job-level check on SerumInd would fire as soon
        // as the carry toil despawns the serum from the map (it lives in the carry tracker
        // until the OpenDialog toil), aborting the job mid-walk. Mirrors vanilla
        // JobDriver_InstallImplant which scopes its checks per-toil.

        yield return Toils_Goto.GotoThing(SerumInd, PathEndMode.Touch)
            .FailOnDespawnedNullOrForbidden(SerumInd)
            .FailOnDespawnedOrNull(TargetPawnInd);

        yield return Toils_Haul.StartCarryThing(SerumInd, false, false, false, true, false);

        yield return Toils_Goto.GotoThing(TargetPawnInd, PathEndMode.Touch)
            .FailOnDespawnedOrNull(TargetPawnInd);

        Toil wait = Toils_General.WaitWith(TargetPawnInd, PrepTicks, true, false, false, TargetPawnInd, PathEndMode.Touch);
        wait.FailOnDespawnedOrNull(TargetPawnInd);
        yield return wait;

        yield return Toils_General.Do(OpenDialog);
    }

    private void OpenDialog()
    {
        Pawn targetPawn = TargetPawn;
        Thing serumRef = Serum;
        if (serumRef == null || serumRef.Destroyed || targetPawn == null || targetPawn.Dead)
            return;

        // Drop the carried stack at the user's feet so it's a stable map-side reference
        // for the destroy callback (the dialog is async; the job ends here).
        Thing dropped;
        pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out dropped);
        Thing finalRef = dropped ?? serumRef;

        // Play the vanilla mech serum sound (we bypass CompUsable.UsedBy, which would normally
        // fire CompUseEffect_PlaySound).
        SoundDefOf.MechSerumUsed.PlayOneShot(new TargetInfo(targetPawn.Position, targetPawn.Map));

        Find.WindowStack.Add(new Dialog_ProgrammableHealer(targetPawn, () =>
        {
            if (finalRef != null && !finalRef.Destroyed)
            {
                finalRef.SplitOff(1).Destroy();
            }
        }));
    }
}
