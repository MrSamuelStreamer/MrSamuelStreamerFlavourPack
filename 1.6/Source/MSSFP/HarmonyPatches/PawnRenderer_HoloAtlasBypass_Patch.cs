using System.Reflection;
using HarmonyLib;
using MSSFP.Holo;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Bypass the global pawn texture atlas for holo projections.
///
/// PROBLEM: at <c>CameraDriver.ZoomRootSize &gt; 18</c> (every normal-play zoom level),
/// <c>PawnRenderer.ParallelGetPreRenderResults</c> sets <c>useCached = true</c> for
/// humanlike pawns, routing them through <c>GlobalTextureAtlasManager</c>. The atlas
/// blit uses <c>ShaderDatabase.Cutout</c> (alpha-test). Our <c>MSSFP/HoloMono</c> shader
/// produces semi-transparent pixels (<c>_BaseAlpha 0.55–0.85</c>), so the alpha-test
/// clips most of the pawn — holo becomes invisible at default zoom and only reappears
/// when the camera zooms inside the 18-unit threshold.
///
/// FIX: the useCached predicate ends with <c>!PawnNeedsHediffMaterial(out var _)</c>.
/// That method is private but Harmony-reachable. Postfixing it to return <c>true</c> for
/// holo pawns makes <c>useCached</c> evaluate to <c>false</c>, so every frame falls
/// through to <c>PawnRenderTree.Draw</c> — which respects the material's real blend mode
/// and renders the translucent holo correctly.
///
/// WHY NOT ALSO SET THE Invisible FLAG: <c>DefaultRenderFlagsNow</c> calls this method
/// with an <c>out var renderFlags</c> and applies the flag bits to the final render
/// state. Setting <c>renderFlags |= PawnRenderFlags.Invisible</c> would push the pawn
/// through <c>InvisibilityMatPool</c> (vanilla cloak/invisible-mat shader) and overwrite
/// our HoloMono shader entirely. We only want the return-value side-effect (skip cache),
/// not the flag side-effect — so we mutate <c>__result</c> only.
///
/// COST: O(1) <see cref="ThingWithComps.TryGetComp{T}"/> per render frame on every pawn.
/// Holo is rare; the comp lookup is a hashed dispatch — negligible.
/// </summary>
[HarmonyPatch(typeof(PawnRenderer), "PawnNeedsHediffMaterial")]
public static class PawnRenderer_HoloAtlasBypass_Patch
{
    private static readonly FieldInfo PawnField = AccessTools.Field(typeof(PawnRenderer), "pawn");

    [HarmonyPostfix]
    public static void Postfix(PawnRenderer __instance, ref bool __result)
    {
        if (__result) return;
        if (__instance == null) return;
        if (PawnField?.GetValue(__instance) is not Pawn p) return;
        if (!MSSFPHoloUtil.IsHolo(p)) return;
        __result = true;
    }
}
