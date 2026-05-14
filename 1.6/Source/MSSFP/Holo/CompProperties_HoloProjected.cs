using Verse;

namespace MSSFP.Holo;

/// <summary>
/// Marker comp props attached to the projected pawn. Pairs with <see cref="CompHoloProjected"/>.
/// Carries a back-reference to the source projector so render/persona patches can resolve
/// without scanning the map.
/// </summary>
public class CompProperties_HoloProjected : CompProperties
{
    public CompProperties_HoloProjected()
    {
        compClass = typeof(CompHoloProjected);
    }
}
