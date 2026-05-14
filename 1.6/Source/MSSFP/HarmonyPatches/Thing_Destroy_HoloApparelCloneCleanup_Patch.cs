using HarmonyLib;
using MSSFP.Holo;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Registry-leak cleanup. Postfix on <see cref="Thing.Destroy"/> drops the marker for
/// destroyed clones so <see cref="HoloApparelRegistry"/>.<c>cloneIds</c> doesn't grow
/// monotonically over playtime.
///
/// Why postfix (not prefix): destroy is one-way and we don't read the ID after — fine to
/// unmark after the engine completes destruction. Postfix also avoids interfering with
/// any other prefix that might cancel destruction.
///
/// Coverage: triggers for ALL paths into Thing.Destroy:
///   - <see cref="Pawn_ApparelTracker_NotifyApparelRemoved_HoloClone_Patch"/> destroy
///   - Vanilla drop-then-rot, fire, ChunkBurner, etc.
///   - Mod-issued direct destroys
///   - Map clear / mod-unload
/// </summary>
[HarmonyPatch(typeof(Thing), nameof(Thing.Destroy))]
public static class Thing_Destroy_HoloApparelCloneCleanup_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Thing __instance)
    {
        if (__instance == null) return;
        HoloApparelRegistry registry = HoloApparelRegistry.Instance;
        if (registry == null) return;
        registry.Unmark(__instance);
    }
}
