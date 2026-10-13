using HarmonyLib;
using RimWorld.Planet;
using Verse;
using RimWorld;

namespace MSSFP.Tunnels.HarmonyPatches;

/// <summary>
/// Adjusts needs handling for pawns riding a <see cref="TunnelCaravan"/>: no rest need
/// while in transit (minecart travel is short), and a reduced-effectiveness rest tick
/// when the vanilla rest-need code path is reached anyway.
/// </summary>
[HarmonyPatch(typeof(Caravan_NeedsTracker))]
public static class Caravan_NeedsTracker_Patch
{
    [HarmonyPatch(nameof(Caravan_NeedsTracker.AnyPawnsNeedRest), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool AnyPawnsNeedRestPrefix(Caravan_NeedsTracker __instance, ref bool __result)
    {
        if (!TunnelUtilities.IsEnabled()) return true;

        if (__instance.caravan is TunnelCaravan)
        {
            __result = false;
            return false;
        }
        return true;
    }

    [HarmonyPatch("TrySatisfyRestNeed")]
    [HarmonyPrefix]
    public static bool TrySatisfyRestNeedPrefix(Caravan_NeedsTracker __instance, Pawn pawn, Need_Rest rest)
    {
        if (!TunnelUtilities.IsEnabled()) return true;

        if (__instance.caravan is TunnelCaravan)
        {
            // Allow resting in the minecarts. Minecarts are considered comfortable enough for 80% rest effectiveness?
            // Vanilla bed rest effectiveness is 1.0, floor is 0.8?
            // Let's use 0.8f as if they are resting on the floor of a moving vehicle.

            rest.TickResting(0.8f);
            return false;
        }
        return true;
    }
}
