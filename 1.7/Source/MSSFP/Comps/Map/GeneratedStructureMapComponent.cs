using System.Collections.Generic;
using Verse;

namespace MSSFP.Comps.Map;

public class GeneratedStructureRecord : IExposable
{
    public string DefName;
    public string Author;

    public void ExposeData()
    {
        Scribe_Values.Look(ref DefName, "defName");
        Scribe_Values.Look(ref Author, "author");
    }
}

/// <summary>
/// Records which viewer-submitted structures were generated onto this map, so their authors can be
/// credited. Populated from MSSFP-VFE (optional, VEF-gated); lives in the core assembly so it can be
/// queried unconditionally even when that optional assembly isn't loaded.
/// </summary>
public class GeneratedStructureMapComponent(Verse.Map map) : MapComponent(map)
{
    public List<GeneratedStructureRecord> Structures = [];

    public void RecordStructure(string defName, string author)
    {
        Structures.Add(new GeneratedStructureRecord { DefName = defName, Author = author });
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref Structures, "structures", LookMode.Deep);
        Structures ??= [];
    }
}
