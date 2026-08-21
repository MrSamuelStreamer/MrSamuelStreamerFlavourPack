using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Tunnels;

public class TunnelEntrance : Site, INameableWorldObject
{
    public string nameInt;
    public string Name
    {
        get => nameInt;
        set => nameInt = value;
    }

    public override string Label => nameInt ?? base.Label;

    public override bool HasName => !nameInt.NullOrEmpty();

    public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
    {
        alsoRemoveWorldObject = false;
        return base.ShouldRemoveMapNow(out _);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref nameInt, "nameInt");
    }
}
