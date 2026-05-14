using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Holo pawns wear PROJECTED clones, not real items. Prefix on
/// <see cref="Pawn_ApparelTracker.Wear(Apparel, bool, bool)"/> intercepts the wear:
///
///   1. Skip non-holo pawns (vanilla flow)
///   2. Skip if apparel is already a registry-marked clone (recursion guard for our own
///      __instance.Wear call below)
///   3. Drop the source apparel at holo's feet via <see cref="GenPlace.TryPlaceThing"/>.
///      Source carried in pawn.carryTracker at this point — DropFromInventory equivalent.
///   4. Build a clone via <see cref="HoloApparelFactory.Clone"/>, register in
///      <see cref="HoloApparelRegistry"/>, recurse into Wear with the clone.
///   5. Return false to skip vanilla on the source.
///
/// BIOCODE: vanilla Wear early-returns on biocoded apparel not bonded to the wearer. Clone is
/// unbiocoded by design, so the recursive call passes vanilla's check.
///
/// FLICKER: source apparel briefly hits the ground at holo's feet before clone is worn —
/// that's the spec ("drop at holo's feet"). Player retrieves the original from there.
/// </summary>
[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Wear))]
public static class Pawn_ApparelTracker_Wear_HoloClone_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Pawn_ApparelTracker __instance, Apparel newApparel, bool dropReplacedApparel, bool locked)
    {
        Pawn pawn = __instance.pawn;
        if (!MSSFPHoloUtil.IsHolo(pawn)) return true;
        if (newApparel == null) return true;

        HoloApparelRegistry registry = HoloApparelRegistry.Instance;
        if (registry == null) return true; // No game context — let vanilla handle.

        // Recursion guard: our own clone re-entering Wear. Allow vanilla flow.
        if (registry.IsClone(newApparel)) return true;

        // Build clone first; if anything fails, abort cleanly without touching source.
        Apparel clone = HoloApparelFactory.Clone(newApparel);
        if (clone == null) return false;
        registry.Mark(clone);

        // Drop source at holo's feet. newApparel may currently be in pawn.carryTracker
        // (post-pickup, mid-Wear job) or already despawned (mod paths). Try to place; if
        // it ends up elsewhere on the map that's still fine — player can retrieve.
        IntVec3 dropCell = pawn.PositionHeld;
        Map map = pawn.MapHeld;
        if (map != null)
        {
            // Remove from carry tracker if present, then place.
            if (newApparel.holdingOwner != null)
                newApparel.holdingOwner.Remove(newApparel);
            if (newApparel.Spawned)
                newApparel.DeSpawn();
            GenPlace.TryPlaceThing(newApparel, dropCell, map, ThingPlaceMode.Near);
        }

        // Recursive call wears the clone. Registry.IsClone short-circuits at the top of
        // the next prefix invocation so vanilla flow runs and the clone enters wornApparel.
        __instance.Wear(clone, dropReplacedApparel, locked);
        return false;
    }
}
