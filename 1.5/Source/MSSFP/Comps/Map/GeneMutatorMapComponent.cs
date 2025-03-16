using MSSFP.Genes;
using Verse;

namespace MSSFP.Comps.Map;

public class GeneMutatorMapComponent(Verse.Map map) : MapComponent(map)
{
    public override void MapComponentUpdate()
    {
        base.MapComponentUpdate();
        foreach (GeneMutatorDef def in DefDatabase<GeneMutatorDef>.AllDefs)
        {
            def.Worker.MapComponentUpdate(map);
        }
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        foreach (GeneMutatorDef def in DefDatabase<GeneMutatorDef>.AllDefs)
        {
            def.Worker.MapComponentTick(map);
        }
    }

    public override void MapComponentOnGUI()
    {
        base.MapComponentOnGUI();
        foreach (GeneMutatorDef def in DefDatabase<GeneMutatorDef>.AllDefs)
        {
            def.Worker.MapComponentOnGUI(map);
        }
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        foreach (GeneMutatorDef def in DefDatabase<GeneMutatorDef>.AllDefs)
        {
            def.Worker.MapFinalizeInit(map);
        }
    }

    public override void MapGenerated()
    {
        base.MapGenerated();
        foreach (GeneMutatorDef def in DefDatabase<GeneMutatorDef>.AllDefs)
        {
            def.Worker.MapGenerated(map);
        }
    }

    public override void MapRemoved()
    {
        base.MapRemoved();
        foreach (GeneMutatorDef def in DefDatabase<GeneMutatorDef>.AllDefs)
        {
            def.Worker.MapRemoved(map);
        }
    }
}
