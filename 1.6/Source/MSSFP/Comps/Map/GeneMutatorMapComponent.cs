using System.Collections.Generic;
using System.Linq;
using MSSFP.Genes;
using Verse;

namespace MSSFP.Comps.Map;

public class GeneMutatorMapComponent(Verse.Map map) : MapComponent(map)
{
    private static List<GeneMutatorDef> _cachedDefs;
    private static List<GeneMutatorDef> AllMutatorDefs =>
        _cachedDefs ??= DefDatabase<GeneMutatorDef>.AllDefsListForReading.ToList();

    public override void MapComponentUpdate()
    {
        if (!MSSFPMod.settings.EnableGeneMutators)
            return;
        base.MapComponentUpdate();
        foreach (GeneMutatorDef def in AllMutatorDefs)
        {
            def.Worker.MapComponentUpdate(map);
        }
    }

    public override void MapComponentTick()
    {
        if (!MSSFPMod.settings.EnableGeneMutators)
            return;
        if (Find.TickManager.TicksGame % 250 != 0)
            return;
        base.MapComponentTick();
        foreach (GeneMutatorDef def in AllMutatorDefs)
        {
            def.Worker.MapComponentTick(map);
        }
    }

    public override void MapComponentOnGUI()
    {
        if (!MSSFPMod.settings.EnableGeneMutators)
            return;
        base.MapComponentOnGUI();
        foreach (GeneMutatorDef def in AllMutatorDefs)
        {
            def.Worker.MapComponentOnGUI(map);
        }
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        foreach (GeneMutatorDef def in AllMutatorDefs)
        {
            def.Worker.MapFinalizeInit(map);
        }
    }

    public override void MapGenerated()
    {
        base.MapGenerated();
        foreach (GeneMutatorDef def in AllMutatorDefs)
        {
            def.Worker.MapGenerated(map);
        }
    }

    public override void MapRemoved()
    {
        base.MapRemoved();
        foreach (GeneMutatorDef def in AllMutatorDefs)
        {
            def.Worker.MapRemoved(map);
        }
    }
}
