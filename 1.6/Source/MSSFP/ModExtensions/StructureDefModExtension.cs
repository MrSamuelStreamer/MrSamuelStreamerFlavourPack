using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.ModExtensions;

/// <summary>
/// Attached to a KCSG.StructureLayoutDef to describe a viewer-submitted structure.
/// Ported from GoldenDuckFlavourPack (GDFP.StructureDefModExtension) so the original
/// submissions load unchanged; see Archive/README.md for provenance.
/// </summary>
public class StructureDefModExtension : DefModExtension
{
    /// <summary>Viewer who submitted the structure. Shown when crediting the build.</summary>
    public string author;

    /// <summary>
    /// False means the layout is a fragment meant to sit alongside other structures rather than
    /// fill a map on its own. Settlement replacement draws from the non-standalone pool.
    /// </summary>
    public bool standalone = true;

    public bool doLoot = true;
    public bool anyHostile = false;

    /// <summary>Opt a layout out of random selection without deleting it.</summary>
    public bool excludeFromRandomGen = false;

    /// <summary>When set, the layout only generates in this biome.</summary>
    public BiomeDef biome;

    public IntVec2 size;
    public IntVec3 lordCenter = IntVec3.Invalid;
    public FactionDef pawnFaction;
    public string pawnFactionSearchString;

    /// <summary>Specific pawns to place with the structure, typically the submitting viewer.</summary>
    public List<PawnRepr> spawnedPawns;

    public List<GenStepDef> extraGenSteps;
}
