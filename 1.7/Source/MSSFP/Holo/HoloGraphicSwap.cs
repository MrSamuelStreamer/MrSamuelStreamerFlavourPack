using UnityEngine;
using Verse;

namespace MSSFP.Holo;

/// <summary>
/// Builds a holo-shaded replacement <see cref="Graphic"/> from any vanilla Graphic.
/// The replacement re-uses the same texture path + drawSize + colorTwo + class type,
/// but swaps shader → <see cref="HoloShaders.HoloMono"/> and color → persona tint.
///
/// Result is cached inside <see cref="GraphicDatabase"/> (keyed on the full GraphicRequest
/// including shader + color), so repeated calls with the same (pawn, original) pair
/// return the same instance — no churn, no leak.
///
/// CARDINALITY: cache size ≈ (#unique source graphics) × (#unique persona tints).
/// Personas are scarce (handful of AIPersonalityDefs), graphics are bounded by what's
/// equipped + body/head. Acceptable footprint.
/// </summary>
public static class HoloGraphicSwap
{
    /// <summary>
    /// Resolve the persona tint for a pawn. Reads the back-reference comp set at
    /// projection time; falls back to <see cref="Color.cyan"/> if the chain breaks
    /// (which would indicate a bug — we want it visible, not silent).
    /// </summary>
    public static Color TintForHolo(Pawn pawn)
    {
        if (pawn == null) return Color.cyan;
        CompHoloProjected projected = pawn.TryGetComp<CompHoloProjected>();
        Thing projector = projected?.sourceProjector;
        CompHoloProjector projectorComp = projector?.TryGetComp<CompHoloProjector>();
        if (projectorComp == null) return Color.cyan;
        return projectorComp.Tint;
    }

    /// <summary>
    /// Rebuild <paramref name="original"/> with the holo shader + persona tint baked in.
    /// Returns the original unchanged if anything is missing (no shader, no path, null).
    /// </summary>
    public static Graphic SwapToHoloShader(Graphic original, Pawn pawn)
    {
        if (original == null) return null;
        if (HoloShaders.HoloMono == null) return original;
        if (string.IsNullOrEmpty(original.path)) return original;

        Color tint = TintForHolo(pawn);

        // Non-generic GraphicDatabase.Get takes a runtime Type; this preserves the
        // concrete Graphic subclass (Graphic_Multi for body/apparel, Graphic_Single
        // for some hair, etc.) without a compile-time switch.
        return GraphicDatabase.Get(
            original.GetType(),
            original.path,
            HoloShaders.HoloMono,
            original.drawSize,
            tint,
            original.colorTwo,
            null
        );
    }
}
