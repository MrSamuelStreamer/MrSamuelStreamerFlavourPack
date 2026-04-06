using HarmonyLib;
using MSSFP.Hediffs;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Applies dynamic haunt stat offsets derived from a dead colonist's skill profile.
/// Postfix on StatWorker.GetValueUnfinalized — a hot path, but the early exits are O(1)
/// for pawns without a dynamic haunt (the overwhelming majority of stat requests).
/// </summary>
[HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized))]
public static class DynamicHauntStat_Patch
{
    public static void Postfix(StatDef ___stat, StatRequest req, ref float __result)
    {
        if (!req.HasThing || req.Thing is not Pawn pawn)
            return;
        if (!pawn.RaceProps.Humanlike)
            return;

        HediffComp_DynamicHaunt comp = FindDynamicHauntComp(pawn);
        if (comp?.Profile == null)
            return;

        float offset = comp.Profile.GetStatOffset(___stat, comp.parent.Severity);
        if (offset != 0f)
            __result += offset;
    }

    private static HediffComp_DynamicHaunt FindDynamicHauntComp(Pawn pawn)
    {
        if (pawn.health?.hediffSet?.hediffs == null)
            return null;

        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
        {
            if (hediff is not HediffWithComps hwc)
                continue;
            foreach (HediffComp comp in hwc.comps)
            {
                if (comp is HediffComp_DynamicHaunt dynamic)
                    return dynamic;
            }
        }

        return null;
    }
}
