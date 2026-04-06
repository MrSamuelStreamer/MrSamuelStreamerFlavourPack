using Verse;

namespace MSSFP.Genes;

/// <summary>
/// Marker gene for haunt resistance.
/// Behavior is enforced externally:
///   - HauntedMapComponent excludes these pawns from the PawnPool
///   - HediffComp_HauntProgression halves the progression speed multiplier
/// </summary>
public class Gene_HauntResistant : Gene { }
