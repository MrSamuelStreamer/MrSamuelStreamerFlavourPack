using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Holos cannot serve as traders, chiefs, or carriers in a caravan. Patches the
/// <see cref="TraderCaravanUtility.GetTraderCaravanRole"/> extension method to return
/// <see cref="TraderCaravanRole.None"/> for any pawn carrying a live projection back-ref.
/// </summary>
[HarmonyPatch(typeof(TraderCaravanUtility), nameof(TraderCaravanUtility.GetTraderCaravanRole))]
public static class TraderCaravanUtility_GetTraderCaravanRole_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn p, ref TraderCaravanRole __result)
    {
        if (MSSFPHoloUtil.IsHolo(p))
            __result = TraderCaravanRole.None;
    }
}
