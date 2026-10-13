using HarmonyLib;
using MSSFP.Holo;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Funnel point for body / head / skin / beard render nodes. Postfix on the BASE
/// <see cref="PawnRenderNode.GraphicFor(Pawn)"/>. Hair + apparel override this method
/// (their overrides bypass the base entirely) — they are handled by separate patches:
/// <see cref="PawnRenderNode_Hair_GraphicFor_HoloShader_Patch"/> and
/// <see cref="ApparelGraphicRecordGetter_HoloShader_Patch"/>.
///
/// Why postfix on the result Graphic rather than postfix on <c>ShaderFor</c>:
///   1. <c>ShaderFor</c> only fires on this base flow; hair/apparel never call it.
///   2. Replacing the returned Graphic gives us a single coherent place to apply the
///      shader + tint together, and the new Graphic is cached by GraphicDatabase so
///      repeat-rebuilds are deduplicated.
///
/// Recursion guard: <see cref="HoloGraphicSwap.SwapToHoloShader"/> internally calls
/// <c>GraphicDatabase.Get</c>, which does NOT re-enter <c>GraphicFor</c>. No guard needed.
/// </summary>
[HarmonyPatch(typeof(PawnRenderNode), nameof(PawnRenderNode.GraphicFor))]
public static class PawnRenderNode_GraphicFor_HoloShader_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn pawn, ref Graphic __result)
    {
        if (__result == null) return;
        if (!MSSFPHoloUtil.IsHolo(pawn)) return;
        __result = HoloGraphicSwap.SwapToHoloShader(__result, pawn);
    }
}
