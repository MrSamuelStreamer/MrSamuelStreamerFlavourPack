using System.Collections.Generic;
using System.Linq;
using MSSFP.Genes;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Comps.World;

public class GeneMutatorWorldComponent(RimWorld.Planet.World world) : WorldComponent(world)
{
    private static List<GeneMutatorDef> _cachedDefs;
    private static List<GeneMutatorDef> AllMutatorDefs =>
        _cachedDefs ??= DefDatabase<GeneMutatorDef>.AllDefsListForReading.ToList();

    public override void WorldComponentTick()
    {
        if (Find.TickManager.TicksGame % 250 != 0)
            return;
        base.WorldComponentTick();
        foreach (GeneMutatorDef def in AllMutatorDefs)
        {
            def.Worker.WorldTick(world);
        }
    }

    public override void FinalizeInit(bool fromLoad)
    {
        base.FinalizeInit(fromLoad);
        foreach (GeneMutatorDef def in AllMutatorDefs)
        {
            def.Worker.WorldFinalizeInit(world);
        }
    }

    public override void WorldComponentUpdate()
    {
        base.WorldComponentUpdate();
        foreach (GeneMutatorDef def in AllMutatorDefs)
        {
            def.Worker.WorldComponentUpdate(world);
        }
    }
}
