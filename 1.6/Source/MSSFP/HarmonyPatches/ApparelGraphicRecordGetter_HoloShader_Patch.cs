using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Apparel rendering goes through <see cref="ApparelGraphicRecordGetter.TryGetGraphicApparel"/>,
/// NOT through <see cref="PawnRenderNode.GraphicFor"/>. That helper builds its own
/// Graphic via <see cref="GraphicDatabase"/> using either <c>Cutout</c> or
/// <c>CutoutComplex</c> depending on apparel def — none of which routes through the
/// node-level <c>ShaderFor</c> hook.
///
/// We can't trivially read the pawn here (the method only sees an <see cref="Apparel"/>),
/// but holo apparel always exists as a registered clone in
/// <see cref="HoloApparelRegistry"/> — so if the apparel is a clone, we know its
/// wearer is a holo and we can swap.
///
/// Wearer lookup: <see cref="Apparel.Wearer"/>. May briefly be null between Wear-call
/// recursion and ThingOwner placement — null-guard accordingly.
/// </summary>
[HarmonyPatch(typeof(ApparelGraphicRecordGetter), nameof(ApparelGraphicRecordGetter.TryGetGraphicApparel))]
public static class ApparelGraphicRecordGetter_HoloShader_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Apparel apparel, ref ApparelGraphicRecord rec, bool __result)
    {
        if (!__result) return;
        if (rec.graphic == null) return;
        if (apparel == null) return;

        // Only holo-worn clones get the shader swap. Non-clone apparel on regular pawns
        // is untouched. Clones are only ever worn by holos by construction (registry
        // marks them at Wear-time inside Pawn_ApparelTracker_Wear_HoloClone_Patch).
        HoloApparelRegistry registry = HoloApparelRegistry.Instance;
        if (registry == null) return;
        if (!registry.IsClone(apparel)) return;

        Pawn wearer = apparel.Wearer;
        if (wearer == null) return;
        if (!MSSFPHoloUtil.IsHolo(wearer)) return;

        rec = new ApparelGraphicRecord(
            HoloGraphicSwap.SwapToHoloShader(rec.graphic, wearer),
            apparel
        );
    }
}
