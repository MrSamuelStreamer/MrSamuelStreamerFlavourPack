using System;
using System.Linq;
using HarmonyLib;
using MSSFP.Comps.Map;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Echolocation psycast (C5): the caster is immune to their own blind
/// maluses (structural — T3 never applies the hediff to Caster, so
/// T4/T5's HasHediff gates never trigger for them) and gets an accuracy
/// bonus shooting at a target with a blip within the last ~30 ticks.
/// </summary>
[HarmonyPatch(typeof(ShotReport), nameof(ShotReport.HitReportFor))]
public static class ShotReport_EcholocationBonus_Patch
{
    private const int FreshnessWindow = 30;
    private const float AccuracyBonus = 0.5f;

    [HarmonyPostfix]
    public static void Postfix(Thing caster, LocalTargetInfo target, ref ShotReport __result)
    {
        if (caster is not Pawn casterPawn || casterPawn.Map == null)
            return;

        EcholocationMapComponent comp = casterPawn.Map.GetComponent<EcholocationMapComponent>();
        if (comp is not { Active: true } || comp.Caster != casterPawn)
            return;

        IntVec3 targetPos = target.Cell;
        int now = Find.TickManager.TicksGame;
        bool fresh = comp.Blips.Any(b =>
            b.Position.DistanceTo(targetPos) <= 2f && now - b.Tick <= FreshnessWindow
        );
        if (!fresh)
            return;

        var offsetFromDarknessField = AccessTools.Field(typeof(ShotReport), "offsetFromDarkness");
        TypedReference resultRef = __makeref(__result);
        float current = (float)offsetFromDarknessField.GetValueDirect(resultRef);
        offsetFromDarknessField.SetValueDirect(resultRef, current + AccuracyBonus);
    }
}
