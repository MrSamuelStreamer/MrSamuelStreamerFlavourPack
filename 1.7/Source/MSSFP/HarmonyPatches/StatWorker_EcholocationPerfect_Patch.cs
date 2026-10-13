using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Echolocation caster buff: MeleeDodgeChance and ShootingAccuracyPawn are
/// both curve-capped in vanilla (dodge maxes at 50%, accuracy at 99.9%
/// regardless of offset) — patched here to force a literal 100% for the
/// duration, bypassing those curves entirely.
/// </summary>
[HarmonyPatch(typeof(StatWorker), nameof(StatWorker.FinalizeValue))]
public static class StatWorker_FinalizeValue_EcholocationPerfect_Patch
{
    [HarmonyPostfix]
    public static void Postfix(StatDef ___stat, StatRequest req, ref float val)
    {
        if (___stat != StatDefOf.MeleeDodgeChance && ___stat != StatDefOf.ShootingAccuracyPawn)
            return;

        if (req.Thing is not Pawn pawn)
            return;

        if (pawn.health.hediffSet.HasHediff(MSSFPDefOf.MSSFP_EcholocationCasterBuff))
        {
            val = 1f;
        }
    }
}
