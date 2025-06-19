using MSSFP.Genes;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Comps.World;

public class GeneMutatorWorldComponent(RimWorld.Planet.World world) : WorldComponent(world)
{
    public override void WorldComponentTick()
    {
        base.WorldComponentTick();
        foreach (GeneMutatorDef def in DefDatabase<GeneMutatorDef>.AllDefs)
        {
            def?.Worker?.WorldTick(world);
        }
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        foreach (GeneMutatorDef def in DefDatabase<GeneMutatorDef>.AllDefs)
        {
            def?.Worker?.WorldFinalizeInit(world);
        }
    }

    public override void WorldComponentUpdate()
    {
        base.WorldComponentUpdate();
        foreach (GeneMutatorDef def in DefDatabase<GeneMutatorDef>.AllDefs)
        {
            def?.Worker?.WorldComponentUpdate(world);
        }
    }
}
