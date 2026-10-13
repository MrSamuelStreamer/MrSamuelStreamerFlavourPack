using HarmonyLib;
using MSSFP.ModExtensions;
using MSSFP.Utils;
using RimWorld;
using System.Linq;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// When a healer mech serum with the MSSFP HealerModExtension actually heals a condition,
/// apply a guaranteed downside (passion drop / negative trait / fallback mood memory).
/// Gated on snapshot of "bad hediff count" — only fires when count actually decreased so
/// the player isn't punished for a no-op administration.
/// </summary>
[HarmonyPatch(typeof(CompUseEffect_FixWorstHealthCondition))]
[HarmonyPatch(nameof(CompUseEffect_FixWorstHealthCondition.DoEffect))]
public static class CompUseEffect_FixWorstHealthCondition_Patch
{
    [System.ThreadStatic]
    private static int _badHediffCountSnapshot;

    [HarmonyPrefix]
    public static void Prefix(CompUseEffect_FixWorstHealthCondition __instance, Pawn usedBy)
    {
        if (usedBy?.health?.hediffSet == null)
        {
            _badHediffCountSnapshot = -1;
            return;
        }
        HealerModExtension ext = __instance.parent?.def?.GetModExtension<HealerModExtension>();
        if (ext is not { EnableDownsides: true })
        {
            _badHediffCountSnapshot = -1;
            return;
        }
        _badHediffCountSnapshot = usedBy.health.hediffSet.hediffs.Count(h => h.def.isBad);
    }

    [HarmonyPostfix]
    public static void Postfix(CompUseEffect_FixWorstHealthCondition __instance, Pawn usedBy)
    {
        if (_badHediffCountSnapshot < 0) return;
        if (usedBy?.health?.hediffSet == null) return;

        int after = usedBy.health.hediffSet.hediffs.Count(h => h.def.isBad);
        if (after >= _badHediffCountSnapshot)
        {
            // No heal occurred — silent skip.
            return;
        }

        MechSerumDownsides.ApplyGuaranteedDownside(usedBy, "healer mech serum");
    }
}
