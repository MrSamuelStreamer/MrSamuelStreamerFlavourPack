using System.Collections.Generic;
using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Leash-aware filter on every JobGiver result for holo-projection pawns.
///
/// PROBLEM: vanilla JobGivers (apparel, haul, hunt, mine, gather, etc.) have no concept of
/// the holo's <see cref="Area_HoloLeash"/>. They happily issue jobs targeting things anywhere
/// on the map. When the resulting target sits outside the leash, the pawn walks toward it,
/// crosses the leash boundary, and <see cref="CompHoloProjected.CompTickRare"/> interrupts
/// with a forced <c>Goto</c> back inside (line ~118 of that file). The think tree then
/// re-picks the same out-of-leash target, producing visible oscillation (the user reported
/// alternating "moving" / "wearing flak vest" inspector lines).
///
/// FIX: postfix <see cref="ThinkNode_JobGiver.TryIssueJobPackage"/> — the single funnel that
/// every JobGiver subclass (vanilla + modded) routes through. If the proposed job has any
/// positional target outside the leash, replace the <see cref="ThinkResult"/> with
/// <see cref="ThinkResult.NoJob"/>. Vanilla then walks to the next sibling node and ultimately
/// falls back to <see cref="JobGiver_Idle"/> / <c>Wait_Wander</c> — well-trodden code path,
/// no <see cref="Pawn_JobTracker"/> state corruption.
///
/// COVERAGE: catches all job types. The forced-Goto fallback in <see cref="CompHoloProjected"/>
/// remains as a safety net for transient out-of-leash states (player drag, projector moved).
///
/// PERFORMANCE: non-holo pawns short-circuit on the first <see cref="MSSFPHoloUtil.IsHolo"/>
/// check (one <c>TryGetComp</c>). For holos, at most ~5 BoolGrid lookups (O(1) each) per
/// <c>TryIssueJobPackage</c>. Negligible at the expected colony scale (≤10 holos).
/// </summary>
[HarmonyPatch(typeof(ThinkNode_JobGiver), nameof(ThinkNode_JobGiver.TryIssueJobPackage))]
public static class ThinkNode_JobGiver_HoloLeashFilter_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn pawn, ref ThinkResult __result)
    {
        Job job = __result.Job;
        if (job == null) return;
        if (pawn == null) return;
        if (!MSSFPHoloUtil.IsHolo(pawn)) return;
        // Player control wins over leash — drafted holos can be ordered anywhere.
        if (pawn.Drafted) return;

        Area_HoloLeash leash = pawn.TryGetComp<CompHoloProjected>()?.ProjectorComp?.area;
        if (leash == null) return;
        Map map = pawn.Map;
        if (map == null) return;

        if (TargetOutside(job.targetA, map, leash)
            || TargetOutside(job.targetB, map, leash)
            || TargetOutside(job.targetC, map, leash)
            || QueueOutside(job.targetQueueA, map, leash)
            || QueueOutside(job.targetQueueB, map, leash))
        {
            __result = ThinkResult.NoJob;
        }
    }

    /// <summary>
    /// True when the target resolves to a valid in-bounds cell that is NOT inside the leash.
    /// Invalid / out-of-bounds targets return false — vanilla error paths stay visible.
    /// Wait / idle jobs have <c>targetA.IsValid == false</c> and therefore are never rejected.
    /// </summary>
    private static bool TargetOutside(LocalTargetInfo t, Map map, Area_HoloLeash leash)
    {
        if (!t.IsValid) return false;
        IntVec3 c = t.HasThing ? t.Thing.PositionHeld : t.Cell;
        if (!c.IsValid || !c.InBounds(map)) return false;
        return !leash[c];
    }

    private static bool QueueOutside(List<LocalTargetInfo> q, Map map, Area_HoloLeash leash)
    {
        if (q == null) return false;
        for (int i = 0; i < q.Count; i++)
        {
            if (TargetOutside(q[i], map, leash)) return true;
        }
        return false;
    }
}
