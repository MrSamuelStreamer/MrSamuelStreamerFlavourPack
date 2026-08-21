using HarmonyLib;
using Verse;

namespace MSSFP.Tunnels.HarmonyPatches;

/// <summary>
/// Ensures <see cref="Building_Tunnel"/> is included in the <c>MapPortal</c> thing
/// request group, so vanilla systems that query for map portals (e.g. pathing,
/// caravan-exit detection) also see tunnel entrances.
/// </summary>
[HarmonyPatch(typeof(ThingListGroupHelper))]
public static class ThingListGroupHelper_Patch
{
  [HarmonyPatch(nameof(ThingListGroupHelper.Includes))]
  [HarmonyPostfix]
  public static void Includes_Patch(ThingRequestGroup group, ThingDef def, ref bool __result)
  {
    if (!TunnelUtilities.IsEnabled()) return;

    if (group != ThingRequestGroup.MapPortal) return;

    if (typeof(Building_Tunnel).IsAssignableFrom(def.thingClass))
    {
      __result = true;
    }
  }
}
