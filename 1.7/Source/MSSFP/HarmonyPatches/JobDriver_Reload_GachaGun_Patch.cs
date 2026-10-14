using System.Collections.Generic;
using HarmonyLib;
using MSSFP.Comps;
using RimWorld;
using RimWorld.Utility;
using Verse;
using Verse.AI;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(JobDriver_Reload), "MakeNewToils")]
public static class JobDriver_Reload_GachaGun_Patch
{
    public static bool Prefix(JobDriver_Reload __instance, ref IEnumerable<Toil> __result)
    {
        Thing gear = __instance.job.GetTarget(TargetIndex.A).Thing;
        CompEquippableReloadable reloadable = gear?.TryGetComp<CompEquippableReloadable>();
        if (reloadable == null)
            return true;

        __result = ReloadToils(__instance, reloadable);
        return false;
    }

    private static IEnumerable<Toil> ReloadToils(
        JobDriver_Reload driver,
        CompEquippableReloadable reloadable
    )
    {
        driver.FailOn(() => reloadable == null || reloadable.parent.DestroyedOrNull());
        driver.FailOn(() => ReloadableUtility.OwnerOf(reloadable) != driver.pawn);
        driver.FailOn(() => !reloadable.NeedsReload(true));
        driver.FailOnDestroyedOrNull(TargetIndex.A);
        driver.FailOnIncapable(PawnCapacityDefOf.Manipulation);

        Toil getNextIngredient = Toils_General.Label();
        yield return getNextIngredient;

        foreach (Toil toil in ReloadAsMuchAsPossible(driver, reloadable))
            yield return toil;

        yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.B, true);
        yield return Toils_Goto
            .GotoThing(TargetIndex.B, PathEndMode.ClosestTouch, false)
            .FailOnDespawnedNullOrForbidden(TargetIndex.B)
            .FailOnSomeonePhysicallyInteracting(TargetIndex.B);
        yield return Toils_Haul
            .StartCarryThing(TargetIndex.B, false, true, false, true, false)
            .FailOnDestroyedNullOrForbidden(TargetIndex.B);
        yield return Toils_Jump.JumpIf(
            getNextIngredient,
            () => !driver.job.GetTargetQueue(TargetIndex.B).NullOrEmpty()
        );

        foreach (Toil toil in ReloadAsMuchAsPossible(driver, reloadable))
            yield return toil;

        Toil dropLeftovers = ToilMaker.MakeToil("MakeNewToils");
        dropLeftovers.initAction = () =>
        {
            Thing carried = driver.pawn.carryTracker.CarriedThing;
            if (carried != null && !carried.Destroyed)
            {
                driver.pawn.carryTracker.TryDropCarriedThing(
                    driver.pawn.Position,
                    ThingPlaceMode.Near,
                    out Thing _
                );
            }
        };
        dropLeftovers.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return dropLeftovers;
    }

    private static IEnumerable<Toil> ReloadAsMuchAsPossible(
        JobDriver_Reload driver,
        CompEquippableReloadable reloadable
    )
    {
        Toil done = Toils_General.Label();
        yield return Toils_Jump.JumpIf(
            done,
            () =>
                driver.pawn.carryTracker.CarriedThing == null
                || driver.pawn.carryTracker.CarriedThing.stackCount < reloadable.MinAmmoNeeded(true)
        );
        yield return Toils_General
            .Wait(reloadable.BaseReloadTicks, TargetIndex.None)
            .WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);

        Toil reload = ToilMaker.MakeToil("ReloadAsMuchAsPossible");
        reload.initAction = () =>
        {
            Thing carried = driver.pawn.carryTracker.CarriedThing;
            reloadable.ReloadFrom(carried);
        };
        reload.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return reload;
        yield return done;
    }
}
