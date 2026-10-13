using System.Reflection;
using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Holos cannot be carried (rescue / capture / haul-to-bed). The projection has no physical
/// body to pick up. Prefix on
/// <see cref="JobDriver_CarryDownedPawn.TryMakePreToilReservations"/> fails the reservation
/// when the takee is a holo so the job aborts before any toils run.
/// <c>Takee</c> is a private property; resolved via reflection.
/// </summary>
[HarmonyPatch(typeof(JobDriver_CarryDownedPawn), nameof(JobDriver_CarryDownedPawn.TryMakePreToilReservations))]
public static class JobDriver_CarryDownedPawn_Patch
{
    private static readonly PropertyInfo TakeeProp = AccessTools.Property(typeof(JobDriver_CarryDownedPawn), "Takee");

    [HarmonyPrefix]
    public static bool Prefix(JobDriver_CarryDownedPawn __instance, ref bool __result)
    {
        Pawn takee = TakeeProp?.GetValue(__instance) as Pawn;
        if (!MSSFPHoloUtil.IsHolo(takee))
            return true;
        __result = false;
        return false;
    }
}
