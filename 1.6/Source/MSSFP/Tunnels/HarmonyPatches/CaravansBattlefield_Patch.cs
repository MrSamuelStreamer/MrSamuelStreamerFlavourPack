using HarmonyLib;
using RimWorld.Planet;

namespace MSSFP.Tunnels.HarmonyPatches;

/// <summary>
/// Suppresses the "Caravan battle won" letter for non-combat tunnel encounters.
///
/// <c>CaravansBattlefield.CheckWonBattle</c> (private) fires
/// <c>LetterLabelCaravansBattlefieldVictory</c> the first tick no hostile threats
/// remain. On non-combat maps (e.g. Forgotten Ossuary) there are never any threats,
/// so the letter fires immediately on map load.
///
/// Fix: skip <c>CheckWonBattle</c> entirely when the map holds a
/// <see cref="MSSFP.Tunnels.MapComponents.MapComponent_SuppressBattleWon"/> marker component.
/// Non-combat incident workers add this component in
/// <c>PostProcessGeneratedPawnsAfterSpawning</c>.
/// </summary>
[HarmonyPatch(typeof(CaravansBattlefield), "CheckWonBattle")]
public static class CaravansBattlefield_Patch
{
    static bool Prefix(CaravansBattlefield __instance)
    {
        if (!TunnelUtilities.IsEnabled()) return true;

        if (__instance.Map?.GetComponent<MSSFP.Tunnels.MapComponents.MapComponent_SuppressBattleWon>() != null)
            return false;  // skip CheckWonBattle entirely

        return true;
    }
}
