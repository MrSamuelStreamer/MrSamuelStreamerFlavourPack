using Verse;

namespace MSSFP.Comps.Map;

public class SpiritOfFroggeMapComponent(Verse.Map map) : MapComponent(map)
{
    //Merged into HauntedMapComponent - keeping for save compat
    public int LastFiredTick = 0;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref LastFiredTick, "LastFiredTick");
    }
}
