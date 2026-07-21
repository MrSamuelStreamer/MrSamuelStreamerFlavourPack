using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.VFE.HarmonyPatches;

/// <summary>
/// Works around a bug in KCSG.SymbolUtils.PreparePawnCorpse: it removes rottable inventory items
/// via `foreach (var item in inv) { ... inv.Remove(item); }`, mutating the collection it's
/// enumerating. The first rottable item found throws InvalidOperationException ("Collection was
/// modified"), caught and logged per-symbol by KCSG's own caller — cosmetic (the grave/sarcophagus
/// just loses its corpse), but noisy, and hit reliably by any graveyard-heavy structure since
/// generated pawns routinely carry rottable inventory (food).
///
/// Fix: strip rottable items ourselves first, via a safe snapshot-then-remove. By the time the
/// original method's own loop runs, there's nothing left matching CompRottable, so its buggy
/// Remove() call never executes and the exception never fires. A PR upstream to KCSG is the real
/// fix (github.com/... — see MSSFP's own commit); this is the stopgap until that lands.
/// </summary>
[HarmonyPatch]
public static class KCSG_PreparePawnCorpse_Patch
{
    private static bool Prepare()
    {
        return AccessTools.Method(typeof(KCSG.SymbolUtils), "PreparePawnCorpse", [typeof(Pawn), typeof(bool)]) != null;
    }

    private static System.Reflection.MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(KCSG.SymbolUtils), "PreparePawnCorpse", [typeof(Pawn), typeof(bool)]);
    }

    public static void Prefix(Pawn pawn)
    {
        if (pawn?.inventory == null)
            return;

        ThingOwner inv = pawn.inventory.GetDirectlyHeldThings();
        foreach (Thing item in inv.ToList())
        {
            if (item.TryGetComp<CompRottable>() != null)
                inv.Remove(item);
        }
    }
}
