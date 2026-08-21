using Verse;

namespace MSSFP.Tunnels;

/// <summary>
/// Single-source gate for whether the tunnel system is active on the currently
/// loaded world. Backed by <see cref="TunnelGenData.tunnelsEnabledForSave"/>,
/// which is snapshotted once at world-gen and never changes for the life of a save.
/// The cache is refreshed via a Harmony postfix on <c>World.FillComponents</c>
/// (see <see cref="MSSFP.Tunnels.HarmonyPatches.WorldComponentsCreated_Patch"/>)
/// so it stays valid across world loads without a per-call component lookup.
/// </summary>
public static class TunnelUtilities
{
    private static bool? _cached;

    public static void RefreshCache()
    {
        _cached = Find.World?.GetComponent<TunnelGenData>()?.tunnelsEnabledForSave;
    }

    public static bool IsEnabled() => _cached ?? false;
}
