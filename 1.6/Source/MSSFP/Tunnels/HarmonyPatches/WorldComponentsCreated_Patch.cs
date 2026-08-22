using HarmonyLib;
using RimWorld.Planet;

namespace MSSFP.Tunnels.HarmonyPatches;

/// <summary>
/// Refreshes <see cref="TunnelUtilities"/>'s cached enabled-state whenever a world's
/// components are (re)constructed — covers both fresh world-gen and loading an existing
/// save, since <see cref="World.FillComponents"/> runs in both cases.
/// </summary>
[HarmonyPatch(typeof(World), "FillComponents")]
public static class WorldComponentsCreated_Patch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        TunnelUtilities.RefreshCache();
    }
}
