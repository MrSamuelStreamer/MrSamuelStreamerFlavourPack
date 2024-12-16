using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP;

public class SpiritOfFroggeMapComponent(Map map) : MapComponent(map)
{
    //Merged into HauntedMapComponent - keeping for save compat
    public int LastFiredTick = 0;
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref LastFiredTick, "LastFiredTick");
    }
}
