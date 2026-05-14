using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Holos must not appear as sellable colony pawns. Vanilla has no <c>PawnSellable</c> entry
/// point — the actual filter is <see cref="TradeUtility.AllSellableColonyPawns"/>, consumed by
/// <c>Pawn_TraderTracker</c> and <c>TradeShip</c>. Postfix wraps the iterator with a holo filter.
/// </summary>
[HarmonyPatch(typeof(TradeUtility), nameof(TradeUtility.AllSellableColonyPawns))]
public static class TradeUtility_AllSellableColonyPawns_Patch
{
    [HarmonyPostfix]
    public static IEnumerable<Pawn> Postfix(IEnumerable<Pawn> __result)
    {
        return __result.Where(p => !MSSFPHoloUtil.IsHolo(p));
    }
}
