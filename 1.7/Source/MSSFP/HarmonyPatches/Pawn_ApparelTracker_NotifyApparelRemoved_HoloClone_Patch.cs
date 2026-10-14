using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Cloned holo apparel must NEVER exist off-pawn. Postfix on
/// <see cref="Pawn_ApparelTracker.Notify_ApparelRemoved(Apparel)"/> catches every removal
/// path (TryDrop, Remove, Wear-replace, mod-direct ThingOwner manipulation) and destroys
/// the clone immediately.
///
/// Why postfix here and not <see cref="Pawn_ApparelTracker.TryDrop(Apparel, out Apparel, IntVec3, bool)"/>:
///   - Wear's <c>dropReplacedApparel=false</c> path calls <c>Remove(apparel)</c> directly,
///     bypassing TryDrop entirely. Patching only TryDrop leaks clones via that branch.
///   - Notify_ApparelRemoved is the single funnel — vanilla calls it after EVERY removal.
///   - Trade-off: TryDrop momentarily spawns the clone on the ground before Notify fires.
///     Microsecond visual flicker, no interactable window for the player. Acceptable.
///
/// SAFETY:
///   - Null-guard on apparel.
///   - Null-guard on registry (Notify can fire during scribe or mod-init when no game live).
///   - Already-destroyed clone (mod re-entry): Destroy() is idempotent at the engine level.
/// </summary>
[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Notify_ApparelRemoved))]
public static class Pawn_ApparelTracker_NotifyApparelRemoved_HoloClone_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Apparel apparel)
    {
        if (apparel == null) return;
        HoloApparelRegistry registry = HoloApparelRegistry.Instance;
        if (registry == null) return;
        if (!registry.IsClone(apparel)) return;
        if (apparel.Destroyed) return;
        apparel.Destroy(DestroyMode.Vanish);
    }
}
