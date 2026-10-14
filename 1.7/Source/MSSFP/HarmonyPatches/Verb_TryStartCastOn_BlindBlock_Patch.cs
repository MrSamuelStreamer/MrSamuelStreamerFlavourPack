using HarmonyLib;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Echolocation psycast (C3a): blind-hediff carriers cannot cast ranged
/// verbs past melee range — vanilla Sight-capacity accuracy malus alone
/// is negligible (V7), so block outright rather than rely on it.
/// </summary>
[HarmonyPatch(
    typeof(Verb),
    nameof(Verb.TryStartCastOn),
    [
        typeof(LocalTargetInfo),
        typeof(LocalTargetInfo),
        typeof(bool),
        typeof(bool),
        typeof(bool),
        typeof(bool),
    ]
)]
public static class Verb_TryStartCastOn_BlindBlock_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(Verb __instance, ref bool __result)
    {
        if (__instance.verbProps.IsMeleeAttack || __instance.EffectiveRange <= 1.42f)
            return true;

        if (
            __instance.caster is Pawn casterPawn
            && casterPawn.health.hediffSet.HasHediff(MSSFPDefOf.MSSFP_EcholocationBlindness)
        )
        {
            __result = false;
            return false;
        }

        return true;
    }
}
