using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Echolocation psycast (C3b): blind-hediff carriers wander erratically —
/// nudge the chosen wander cell off-target, falling back to the original
/// pick if the noisy cell is out of bounds or unstandable.
/// </summary>
[HarmonyPatch(typeof(RCellFinder), nameof(RCellFinder.RandomWanderDestFor))]
public static class RCellFinder_RandomWanderDestFor_BlindNoise_Patch
{
    private const int NoiseRadius = 3;

    [HarmonyPostfix]
    public static void Postfix(Pawn pawn, ref IntVec3 __result)
    {
        if (pawn?.health == null || pawn.Map == null)
            return;

        if (!pawn.health.hediffSet.HasHediff(MSSFPDefOf.MSSFP_EcholocationBlindness))
            return;

        IntVec3 noisy = __result + new IntVec3(
            Rand.RangeInclusive(-NoiseRadius, NoiseRadius),
            0,
            Rand.RangeInclusive(-NoiseRadius, NoiseRadius)
        );

        if (noisy.InBounds(pawn.Map) && noisy.Standable(pawn.Map))
            __result = noisy;
    }
}
