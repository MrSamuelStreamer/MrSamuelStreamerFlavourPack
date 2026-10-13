using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.RB.HarmonyPatches;

/// <summary>
/// Reports a pawn being lifted as lying down, so the renderer lays their head and body out
/// on the same axis instead of spinning the body under an upright head.
///
/// DERIVED, NOT STORED. The obvious approach — writing
/// <c>liftee.jobs.posture = LayingOnGroundFaceUp</c> when the set starts — does not hold: a
/// carried pawn is still tick-live, its own <c>Pawn_JobTracker</c> keeps running, and it
/// recomputes posture from its own job every tick, clobbering the write within a frame.
/// Re-writing it each tick would work but races the liftee's own tick order and can flicker.
///
/// Deriving it here is deterministic and, more importantly, leaves nothing behind: there is
/// no field to reset, so no exit path can strand a colonist permanently horizontal the way a
/// stored posture or a stored body angle can.
///
/// GetPosture is called often — during rendering and pathing — so the guards are ordered
/// cheapest-first. Almost every caller bails on the null <c>holdingOwner</c> check.
/// </summary>
[HarmonyPatch(typeof(PawnUtility), nameof(PawnUtility.GetPosture))]
public static class PawnUtility_GetPosture_PawnLift_Patch
{
    public static void Postfix(Pawn p, ref PawnPosture __result)
    {
        if (__result != PawnPosture.Standing)
            return;
        if (p?.holdingOwner?.Owner is not Pawn_CarryTracker carryTracker)
            return;
        if (carryTracker.pawn?.CurJobDef != DefOf_MSSFPRB.MSSFP_DoPawnLift)
            return;

        __result = PawnPosture.LayingOnGroundFaceUp;
    }
}
