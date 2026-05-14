using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Holos cannot be enslaved. Holos should never reach prisoner state in the first place
/// (CompHoloProjected absorbs damage, blocks arrest, etc.) — this patch is a defensive
/// safety net at the actual enslave entry point.
/// </summary>
[HarmonyPatch(typeof(GenGuest), nameof(GenGuest.TryEnslavePrisoner))]
public static class GenGuest_TryEnslavePrisoner_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Pawn prisoner, ref bool __result)
    {
        if (!MSSFPHoloUtil.IsHolo(prisoner))
            return true;
        __result = false;
        return false;
    }
}
