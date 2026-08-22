using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.Tunnels.HarmonyPatches;

/// <summary>
/// Prevents the "no pawns left" game-over check from firing while player pawns are
/// in transit inside a tunnel (loading into a <see cref="Building_Tunnel"/>) or
/// riding a <see cref="TunnelCaravan"/> — both states have no pawn present on any map.
/// </summary>
[HarmonyPatch]
public static class GameOver_Patch
{
  [HarmonyPatch(typeof(GameEnder), nameof(GameEnder.CheckOrUpdateGameOver))]
  [HarmonyPrefix]
  public static bool CheckOrUpdateGameOver_Prefix(GameEnder __instance)
  {
    if (!TunnelUtilities.IsEnabled()) return true;

    // If we find any player pawn in a tunnel or tunnel caravan,
    // we prevent GameEnder from continuing its check, which would
    // trigger the "no pawns left" game over if no other pawns were on maps.

    if (AnyPlayerPawnInTunnels())
    {
      // Reset the gameEnding state if it was somehow set,
      // or just prevent it from being set now.
      __instance.gameEnding = false;
      return false; // Skip original method
    }

    return true; // Continue with original check
  }

  private static bool AnyPlayerPawnInTunnels()
  {
    // 1. Check TunnelCaravans (WorldObjects)
    foreach (TunnelCaravan tunnelCaravan in Find.WorldObjects.AllWorldObjects.OfType<TunnelCaravan>())
    {
      if (tunnelCaravan.PawnsListForReading.Any(p => p.Faction == Faction.OfPlayer))
      {
        return true;
      }
    }

    // 2. Check Building_Tunnels on all maps (for pawns currently loading/waiting).
    // ThingsOfDef is a maintained per-def index, unlike AllThings which is every thing
    // on the map (items, filth, plants, pawns, buildings, ...).
    foreach (Map map in Find.Maps)
    {
      foreach (Thing thing in map.listerThings.ThingsOfDef(MSSFPDefOf.MSSFP_TunnelEntrance))
      {
        if (thing is Building_Tunnel tunnel && tunnel.innerContainer.OfType<Pawn>().Any(p => p.Faction == Faction.OfPlayer))
        {
          return true;
        }
      }
    }

    return false;
  }

  [HarmonyPatch(typeof(MapPawns), nameof(MapPawns.AnyPawnBlockingMapRemoval), MethodType.Getter)]
  [HarmonyPostfix]
  public static void AnyPawnBlockingMapRemoval_Postfix(MapPawns __instance, ref bool __result)
  {
    if (!TunnelUtilities.IsEnabled()) return;

    if (__result) return;

    Map map = Find.Maps.FirstOrDefault(m => m.mapPawns == __instance);
    if (map == null) return;

    foreach (Thing thing in map.listerThings.ThingsOfDef(MSSFPDefOf.MSSFP_TunnelEntrance))
    {
      if (thing is Building_Tunnel tunnel && tunnel.innerContainer.OfType<Pawn>().Any(p => p.Faction == Faction.OfPlayer))
      {
        __result = true;
        return;
      }
    }
  }
}
