using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP;

public class SpiritOfFroggeMapComponent(Map map) : MapComponent(map)
{
    public int LastFiredTick = 0;

    public override void MapComponentTick()
    {
        //It's ok if we miss some ticks, so the simple check is fine
        if (LastFiredTick + GenDate.TicksPerDay < Find.TickManager.TicksGame)
        {
            LastFiredTick = Find.TickManager.TicksGame + GenDate.TicksPerHour;
            IEnumerable<Thing> graves = map.listerThings.ThingsOfDef(ThingDefOf.Grave)
                .Where(grave => grave is Building_Grave g && g.Corpse != null && g.Corpse.InnerPawn.def == MSSFPDefOf.MSSFP_Frogge);
            IEnumerable<Thing> sarcophaguseseseses = map.listerThings.ThingsOfDef(ThingDefOf.Sarcophagus)
                .Where(grave => grave is Building_Sarcophagus g && g.Corpse != null && g.Corpse.InnerPawn.def == MSSFPDefOf.MSSFP_Frogge);

            IEnumerable<Pawn> pawnPool = graves
                .Concat(sarcophaguseseseses)
                .SelectMany(thing=>GenRadial.RadialCellsAround(thing.Position, 10, false))
                .Distinct()
                .SelectMany(cell=>
                    map.thingGrid
                        .ThingsAt(cell))
                .OfType<Pawn>()
                .Where(pawn=>!pawn.health.hediffSet.HasHediff(MSSFPDefOf.MSS_FP_FroggeHaunt));

            Pawn pawn = pawnPool.RandomElementWithFallback();

            if(pawn == null) return;
            LastFiredTick += 3*GenDate.TicksPerDay;
            pawn.health.AddHediff(MSSFPDefOf.MSS_FP_FroggeHaunt);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref LastFiredTick, "LastFiredTick");
    }
}
