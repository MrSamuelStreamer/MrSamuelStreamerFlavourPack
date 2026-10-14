using System.Collections.Generic;

namespace MSSFP.VFE.Structures.Export;

/// <summary>What the export dialog collects. Defaults match a typical viewer submission.</summary>
public class PoiExportOptions
{
    public string defName = "";
    public string author = "";

    // StructureDefModExtension fields.
    public bool standalone = true;
    public bool doLoot = true;
    public bool anyHostile;
    public bool excludeFromRandomGen;
    public bool restrictToCurrentBiome;

    // Pawns and corpses go through the pawn exporter instead of KCSG's pawn-kind symbols.
    public bool exportPawns = true;
    public bool exportCorpses = true;
    public bool snapshotMode = true;

    // KCSG's own export toggles.
    public bool exportNatural;
    public bool exportFilth;
    public bool exportPlant;
    public bool forceGenerateRoof;
    public bool spawnConduits;
}

public class PoiExportResult
{
    public string Path;
    public int PawnCount;
    public int CorpseCount;
    public readonly List<string> Warnings = new();
}
