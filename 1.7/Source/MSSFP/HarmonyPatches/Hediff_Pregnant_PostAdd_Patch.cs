using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Holos cannot become pregnant. If something does manage to attach a
/// <see cref="Hediff_Pregnant"/> (e.g. dev-mode add, ritual outcome, mod interaction), the
/// prefix on <c>PostAdd</c> skips the vanilla initializer so no pregnancy state is set up.
/// The hediff itself remains attached — a paired patch on a removal pass could strip it, but
/// gating PostAdd is enough to prevent gestation ticks / outcomes.
///
/// Note: vanilla has no <c>PostMake</c> on <see cref="Hediff_Pregnant"/>; <c>PostAdd</c> is
/// the entry point that runs immediately after the hediff is attached.
/// </summary>
[HarmonyPatch(typeof(Hediff_Pregnant), nameof(Hediff_Pregnant.PostAdd))]
public static class Hediff_Pregnant_PostAdd_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Hediff_Pregnant __instance)
    {
        return !MSSFPHoloUtil.IsHolo(__instance?.pawn);
    }
}
