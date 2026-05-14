using HarmonyLib;
using MSSFP.Holo;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Stripping a projected holo is a no-op (apparel is part of the projection illusion).
/// Vanilla has no <c>Recipe_Strip</c> entry point — the canonical strip path is
/// <see cref="Pawn.Strip"/>.
/// </summary>
[HarmonyPatch(typeof(Pawn), nameof(Pawn.Strip))]
public static class Pawn_Strip_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Pawn __instance)
    {
        return !MSSFPHoloUtil.IsHolo(__instance);
    }
}
