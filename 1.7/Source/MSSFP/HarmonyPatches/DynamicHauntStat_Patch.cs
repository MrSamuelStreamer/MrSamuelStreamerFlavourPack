using HarmonyLib;
using MSSFP.Hediffs;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Applies dynamic haunt stat offsets derived from a dead colonist's skill profile.
/// Postfix on StatWorker.GetValueUnfinalized — a hot path. Uses HauntsCache for O(1)
/// lookup instead of walking the hediff list on every stat call.
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

        if (!HauntsCache.DynamicHaunts.TryGetValue(pawn.thingIDNumber, out HediffComp_DynamicHaunt comp))
            return;
        if (comp.Profile == null)
            return;

        float offset = comp.Profile.GetStatOffset(___stat, comp.parent.Severity);
        if (offset != 0f)
            __result += offset;
    }
}
