using System.Reflection;
using HarmonyLib;
using MSSFP.Holo;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Holo-projection tint hook (P1-A Patch #1).
///
/// TARGET: <c>PawnRenderer.GetDrawParms(...)</c> (private, returns <c>PawnDrawParms</c>).
///
/// PRIOR TARGET WAS WRONG: an earlier revision postfixed <c>PawnRenderTree.AdjustParms</c>.
/// <c>PawnDrawParms</c> is a STRUCT (see <c>default(PawnDrawParms)</c> in
/// <c>PawnRenderer.GetDrawParms</c>). <c>ParallelPreDraw(PawnDrawParms parms)</c> takes the
/// struct by value, and <c>AdjustParms</c> mutates only that local copy. <c>RenderPawnInternal</c>
/// then calls <c>renderTree.Draw(parms)</c> with the ORIGINAL un-adjusted struct, and
/// <c>PawnRenderNodeWorker.PreDraw</c> / <c>GetMaterialPropertyBlock</c> bake
/// <c>parms.tint * material.color</c> into the <c>MaterialPropertyBlock</c> using Draw's parms.
/// Net effect: AdjustParms-only tint mutations never reach the rendered material — confirmed
/// by CHECK 3 (Tint=blue, patch fired, pawn unchanged on screen).
///
/// GetDrawParms is the SINGLE construction point for the per-render <c>PawnDrawParms</c>
/// struct. Mutating <c>__result.tint</c> here propagates to BOTH ParallelPreDraw (for
/// ShouldRecache + AppendRequests) AND RenderPawnInternal → Draw → PreDraw → matPropBlock.
///
/// PAWN ACCESS: <c>PawnRenderer.pawn</c> is <c>private readonly Pawn</c>. Reflected once
/// into a cached <see cref="FieldInfo"/>.
///
/// PRIORITY: <see cref="Priority.Last"/> — our tint is the final write so other mods'
/// flasher/invisibility tints compose first and we multiply on top.
///
/// EARLY-OUT: skip if the pawn has no <see cref="CompHoloProjected"/> or no source projector.
/// Zero cost on non-holo pawns (one TryGetComp per render).
///
/// MUTATION: multiplicative — keeps vanilla damage-flasher / invisibility-alpha tint
/// compositional. <see cref="CompHoloProjected.Tint"/> defaults to <c>Color.white</c> when no
/// override is set, which is the identity element (1,1,1,1).
/// </summary>
[HarmonyPatch(typeof(PawnRenderer), "GetDrawParms")]
public static class PawnRenderer_ParmsForPawn_HoloTint_Patch
{
    private static readonly FieldInfo PawnField = AccessTools.Field(typeof(PawnRenderer), "pawn");

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void Postfix(PawnRenderer __instance, ref PawnDrawParms __result)
    {
        if (__instance == null)
            return;
        Pawn p = PawnField.GetValue(__instance) as Pawn;
        if (p == null)
            return;
        CompHoloProjected comp = p.TryGetComp<CompHoloProjected>();
        if (comp?.sourceProjector == null)
            return;
        __result.tint *= comp.Tint;
    }
}
