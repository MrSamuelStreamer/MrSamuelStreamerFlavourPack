using MSSFP.PawnPortability.Defs;
using Verse;

namespace MSSFP.ModExtensions;

/// <summary>
/// A pawn (or corpse) exported alongside a structure. Spawned from a PawnTemplateDef after the
/// layout generates, so it keeps everything the pawn exporter captures rather than the thinner
/// <see cref="PawnRepr"/> format.
/// </summary>
public class StructurePawnEntry
{
    public PawnTemplateDef template;

    /// <summary>Cell offset from the structure rect's minimum corner (x = column, z = row).</summary>
    public IntVec3 offset;

    /// <summary>Spawn the pawn, then kill it so it leaves a corpse at the cell.</summary>
    public bool dead;

    /// <summary>Only meaningful with <see cref="dead"/>: start the corpse in the rotting stage.</summary>
    public bool rotten;
}
