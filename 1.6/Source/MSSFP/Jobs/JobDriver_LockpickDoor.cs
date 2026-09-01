using System.Collections.Generic;
using MSSFP.Lockpicking;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Jobs;

/// <summary>
/// Walk to a hostile-faction door, then spend the Manipulation-scaled lockpick
/// wait. After that wait, either auto-succeed or open the timing-bar minigame.
/// Success marks the door lockpicked without claiming it. The minigame uses
/// forcePause, so the Window callback must EndJobWith — job ticks do not run
/// while the dialog is open.
/// </summary>
public class JobDriver_LockpickDoor : JobDriver
{
    private const TargetIndex DoorInd = TargetIndex.A;

    private Building_Door Door => job.GetTarget(DoorInd).Thing as Building_Door;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return Door != null && pawn.Reserve(Door, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(DoorInd);
        this.FailOn(() => !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
        this.FailOn(() => Door == null || !LockpickUtility.IsLockpickTarget(Door, pawn));

        yield return Toils_Goto.GotoThing(DoorInd, PathEndMode.Touch);

        int workTicks = LockpickUtility.WorkTicksFor(pawn);
        Toil wait = Toils_General.Wait(workTicks, DoorInd);
        wait.WithProgressBarToilDelay(DoorInd);
        wait.activeSkill = () => SkillDefOf.Crafting;
        wait.tickAction = () => pawn.skills?.Learn(SkillDefOf.Crafting, 0.1f);
        wait.handlingFacing = true;
        yield return wait;

        if (MSSFPMod.settings?.EnableLockpickingMinigame != true)
        {
            yield return Toils_General.Do(() => LockpickUtility.ApplySuccess(pawn, Door));
            yield break;
        }

        Toil play = ToilMaker.MakeToil("LockpickMinigame");
        play.initAction = () =>
        {
            Building_Door door = Door;
            JobDriver driver = this;
            Find.WindowStack.Add(
                new Dialog_LockpickMinigame(
                    pawn,
                    door,
                    ok =>
                    {
                        if (pawn.jobs?.curDriver != driver)
                            return;
                        if (ok)
                        {
                            LockpickUtility.ApplySuccess(pawn, door);
                            pawn.skills?.Learn(
                                SkillDefOf.Crafting,
                                LockpickUtility.MinigameWinCraftingXp
                            );
                        }
                        driver.EndJobWith(
                            ok ? JobCondition.Succeeded : JobCondition.Incompletable
                        );
                    }
                )
            );
        };
        play.defaultCompleteMode = ToilCompleteMode.Never;
        play.handlingFacing = true;
        yield return play;
    }
}
