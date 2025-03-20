﻿using System.Collections.Generic;
using System.Linq;
using MSSFP.Hediffs;
using RimWorld;
using Verse;

namespace MSSFP.Comps.Map;

public class HauntedMapComponent(Verse.Map map) : MapComponent(map)
{
    public int LastFiredTick = 0;

    public override void MapComponentTick()
    {
        if(!MSSFPMod.settings.ShowHaunts) return;

        //It's ok if we miss some ticks, so the simple check is fine
        if (LastFiredTick + 2 * GenDate.TicksPerDay < Find.TickManager.TicksGame)
        {
            LastFiredTick = Find.TickManager.TicksGame + 2 * GenDate.TicksPerHour;

            IEnumerable<Thing> graves = map.listerThings.ThingsOfDef(ThingDefOf.Grave)
                .Where(grave => grave is Building_Grave { Corpse: not null } or Building_Sarcophagus { Corpse: not null });

            IEnumerable<Pawn> pawnPool = graves
                .SelectMany(thing=>GenRadial.RadialCellsAround(thing.Position, 10, false))
                .Distinct()
                .SelectMany(cell=>
                    map.thingGrid
                        .ThingsAt(cell))
                .OfType<Pawn>()
                .Where(pawn=>!pawn.health.hediffSet.HasHediff(MSSFPDefOf.MSS_FP_PawnDisplayer) && !pawn.health.hediffSet.HasHediff(MSSFPDefOf.MSS_FP_FroggeHaunt));

            Pawn pawn = pawnPool.RandomElementWithFallback();

            if(pawn == null) return;
            LastFiredTick += 4*GenDate.TicksPerDay;

            if (pawn.def == MSSFPDefOf.MSSFP_Frogge)
            {
                pawn.health.AddHediff(MSSFPDefOf.MSS_FP_FroggeHaunt);
            }
            else
            {
                Hediff hediff = pawn.health.AddHediff(MSSFPDefOf.MSS_FP_PawnDisplayer);

                // if (hediff.TryGetComp(out HediffComp_Haunt comp))
                // {
                //     comp.SetPawnToDraw(pawn);
                // }
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref LastFiredTick, "LastFiredTick");
    }


    public override void MapComponentUpdate()
    {
        foreach (Pawn pawn in map.mapPawns.AllHumanlike)
        {
            HauntsCache.TryDrawAt(pawn.thingIDNumber, pawn.TrueCenter());
        }
    }
}
