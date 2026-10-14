using HarmonyLib;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Drops a carried pawn before its carrier despawns, so the passenger is not taken off
/// the map inside <c>Pawn_CarryTracker.innerContainer</c>.
///
/// Job cleanup only runs while the carrier is on a map with a live JobTracker. Caravan
/// formation, gravship launch, quest despawn and map removal despawn the carrier directly,
/// and the carried pawn is not enumerated as a caravan or gravship member — so it ends up
/// neither on the map nor in the departing group.
///
/// DELIBERATELY NARROW. A blanket "drop anything carried on despawn" would fight vanilla,
/// which legitimately carries downed pawns across map edges during caravan and transport
/// pod flows. This fires only for MSSFP's own pawn-lift job, matched by defName string so
/// that the core assembly needs no reference to the Rimbody compat layer (which is absent
/// for players without Rimbody).
///
/// The periodic sweep in <see cref="MSSFP.Comps.Game.CarriedPawnRescueGameComponent"/> is
/// the backstop for the case this cannot see: the compat assembly already unloaded.
/// </summary>
[HarmonyPatch(typeof(Pawn), nameof(Pawn.DeSpawn))]
public static class Pawn_DeSpawn_CarriedPawnGuard_Patch
{
    private const string PawnLiftJobDefName = "MSSFP_DoPawnLift";

    public static void Prefix(Pawn __instance)
    {
        if (__instance?.carryTracker?.CarriedThing is not Pawn)
            return;
        if (__instance.CurJobDef?.defName != PawnLiftJobDefName)
            return;
        if (!__instance.Spawned)
            return;

        __instance.carryTracker.TryDropCarriedThing(__instance.Position, ThingPlaceMode.Near, out _);
    }
}
