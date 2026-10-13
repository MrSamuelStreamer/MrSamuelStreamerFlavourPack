using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Tunnels.HarmonyPatches;

/// <summary>
/// Blocks the player from reforming a caravan while a Cave-In has uncleared debris.
///
/// Patches ALL overloads of CaravanExitMapUtility.ExitMapAndCreateCaravan using
/// TargetMethods() — the same technique as ExitMapAndCreateCaravan_TunnelPatch —
/// so we are not tied to a specific parameter signature.
///
/// When the departure map has a MapComponent_CaveInBlocker whose IsCleared is false,
/// the Prefix returns false to cancel the method and shows a rejection message.
/// The Postfix in ExitMapAndCreateCaravan_TunnelPatch never fires in this case.
/// </summary>
[HarmonyPatch]
public static class CaveInReformBlocker_Patch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        return typeof(CaravanExitMapUtility)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.Name == "ExitMapAndCreateCaravan" && m.ReturnType == typeof(Caravan));
    }

    [HarmonyPrefix]
    public static bool BlockIfDebrisRemains(object[] __args)
    {
        if (!TunnelUtilities.IsEnabled()) return true;

        if (__args?.Length == 0 || __args[0] is not IEnumerable<Pawn> pawns)
            return true;

        Map map = pawns.FirstOrDefault()?.MapHeld;
        if (map == null)
            return true;

        MSSFP.Tunnels.MapComponents.MapComponent_CaveInBlocker blocker = map.GetComponent<MSSFP.Tunnels.MapComponents.MapComponent_CaveInBlocker>();
        if (blocker != null && !blocker.IsCleared)
        {
            Messages.Message("MSSFP_Tunnels_Incidents_CaveInReformBlocked".Translate(),
                MessageTypeDefOf.RejectInput, historical: false);
            return false; // cancel ExitMapAndCreateCaravan
        }

        return true;
    }
}
