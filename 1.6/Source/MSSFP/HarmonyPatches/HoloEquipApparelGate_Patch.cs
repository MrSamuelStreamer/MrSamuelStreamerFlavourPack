using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Holos cannot wield weapons (non-combat by design). Prefix on
/// <see cref="Pawn_EquipmentTracker.AddEquipment"/> drops any incoming weapon at the holo's
/// feet and rejects the call.
///
/// APPAREL is NOT gated here — see <see cref="Pawn_ApparelTracker_Wear_HoloClone_Patch"/>
/// which clones apparel into a projected-only copy. The clone destroys on removal, so the
/// wealth-abuse concern that originally motivated a blanket apparel gate is moot (the source
/// returns to ground, the clone never leaves the holo's body).
///
/// GENERATOR PATH ALLOW: <c>!pawn.Spawned</c> passes through. <see cref="PawnGenerator"/>
/// runs on freshly constructed (unspawned) pawns; the holo PawnKindDef already zeroes
/// <c>weaponMoney</c> so the generator path is benign even when allowed.
///
/// DROP HANDLING: spawned items de-spawn first to avoid double-spawn warnings; unspawned
/// items (carry tracker, inventory) place directly via <see cref="GenPlace.TryPlaceThing"/>.
/// </summary>
[HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.AddEquipment))]
public static class Pawn_EquipmentTracker_AddEquipment_HoloGate_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps newEq)
    {
        Pawn pawn = __instance.pawn;
        if (!MSSFPHoloUtil.IsHolo(pawn))
            return true;
        if (!pawn.Spawned)
            return true;
        if (newEq == null)
            return false;

        HoloGateUtil.DropRefused(newEq, pawn);
        return false;
    }
}

internal static class HoloGateUtil
{
    /// <summary>
    /// Place a refused item at the holo's cell. De-spawns if currently spawned. Silent on
    /// failure (item leak is preferable to a crash — vanilla TryPlaceThing logs its own error
    /// on failure).
    /// </summary>
    public static void DropRefused(Thing item, Pawn pawn)
    {
        if (item.Spawned)
            item.DeSpawn();

        Map map = pawn.MapHeld;
        if (map == null)
            return;

        GenPlace.TryPlaceThing(item, pawn.PositionHeld, map, ThingPlaceMode.Near);

        Messages.Message(
            $"{pawn.LabelShortCap} cannot hold gear (projection).",
            pawn,
            MessageTypeDefOf.RejectInput,
            historical: false
        );
    }
}
