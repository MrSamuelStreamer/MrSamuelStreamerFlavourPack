using RimWorld.Planet;
using Verse;

namespace MSSFP.Genes;

public class GeneMutatorWorker
{
    public GeneMutatorDef def;

    public virtual void Initialize(GeneMutatorDef defIn)
    {
        def = defIn;
    }

    public virtual void WorldFinalizeInit(World world) { }

    public virtual void WorldTick(World world) { }

    public virtual void WorldComponentUpdate(World world) { }

    public virtual void MapComponentUpdate(Map map) { }

    public virtual void MapComponentTick(Map map) { }

    public virtual void MapComponentOnGUI(Map map) { }

    public virtual void MapFinalizeInit(Map map) { }

    public virtual void MapGenerated(Map map) { }

    public virtual void MapRemoved(Map map) { }
}
