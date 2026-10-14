using System;
using System.Collections.Generic;
using MSSFP.Comps.Map;
using RimWorld;
using Verse.AI;

namespace MSSFP.Jobs;

public class JobDriver_GoToThen : JobDriver_Goto
{
    protected override IEnumerable<Toil> MakeNewToils()
    {
        bool readyToGo = false;
        Toil goToCell = Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
        goToCell.AddFinishAction(
            (Action)(
                () =>
                {
                    if (job.controlGroupTag == null || job.controlGroupTag == null)
                        return;
                    pawn.GetOverseer()
                        ?.mechanitor.GetControlGroup(pawn)
                        .SetTag(pawn, job.controlGroupTag);
                }
            )
        );
        goToCell.tickAction = () =>
        {
            if (readyToGo || pawn.Position.InHorDistOf(TargetPawnC.Position, 16f))
            {
                readyToGo = true;
                pawn.jobs.curDriver.ReadyForNextToil();
            }
        };
        yield return goToCell;

        Toil waitForOtherPawn = Toils_General.Wait(GenDate.TicksPerHour * 2);
        waitForOtherPawn.tickAction = () =>
        {
            if (readyToGo || pawn.Position.InHorDistOf(TargetPawnC.Position, 16f))
            {
                readyToGo = true;
                pawn.jobs.curDriver.ReadyForNextToil();
            }
        };
        yield return waitForOtherPawn;

        Toil goToMapEdge = Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
        goToMapEdge.AddFinishAction(
            (Action)(
                () =>
                {
                    pawn.Map.GetComponent<LoversRetreatMapComponent>()?.StorePawn(pawn);
                }
            )
        );
        yield return goToMapEdge;
    }
}
