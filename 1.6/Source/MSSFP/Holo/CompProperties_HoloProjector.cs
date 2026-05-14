using UnityEngine;
using Verse;

namespace MSSFP.Holo;

/// <summary>
/// Comp props for the holo-projector building. Pairs with <see cref="CompHoloProjector"/>.
/// </summary>
public class CompProperties_HoloProjector : CompProperties
{
    /// <summary>Default tint applied to the projection when first spawned.</summary>
    public Color defaultTint = new(1f, 1f, 1f, 1f);

    /// <summary>Leash radius (cells) around projector. Projection cannot stray beyond.</summary>
    public float defaultRadius = 12f;

    /// <summary>PawnKindDef used when generating the stored hologram pawn.</summary>
    public string pawnKindDefName = "MSSFP_HoloProjection";

    public CompProperties_HoloProjector()
    {
        compClass = typeof(CompHoloProjector);
    }
}
