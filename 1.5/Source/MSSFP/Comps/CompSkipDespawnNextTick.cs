using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class CompSkipDespawnNextTick : ThingComp
{
    public bool isFirstTick = true;

    public override void CompTick()
    {
        if (isFirstTick)
        {
            isFirstTick = false;
            return;
        }

        if (parent.HasComp<CompSkipDespawnNextTick>())
        {
            parent.AllComps.Remove(this);
            SkipUtility.SkipDeSpawn(parent);
        }
    }
}
