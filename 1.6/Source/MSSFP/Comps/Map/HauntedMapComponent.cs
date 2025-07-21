using System.Collections.Generic;
using System.Linq;
using MSSFP.Hediffs;
using RimWorld;
using Verse;

namespace MSSFP.Comps.Map;

public class HauntedMapComponent(Verse.Map map) : MapComponent(map)
{
    public int LastFiredTick = 0;
    public int SearchRadius = 10;

    public IEnumerable<Building_Grave> Graves => map.listerThings.AllThings.OfType<Building_Grave>().Where(g => g.Corpse != null);

    public IEnumerable<Pawn> PawnsNearGraves =>
        Graves.SelectMany(thing => GenRadial.RadialCellsAround(thing.Position, SearchRadius, true)).Distinct().SelectMany(cell => map.thingGrid.ThingsAt(cell)).OfType<Pawn>();

    public IEnumerable<Pawn> PawnPool =>
        PawnsNearGraves.Where(pawn => !pawn.health.hediffSet.HasHediff(MSSFPDefOf.MSS_FP_PawnDisplayer) && !pawn.health.hediffSet.HasHediff(MSSFPDefOf.MSS_FP_FroggeHaunt));

    public override void MapComponentTick()
    {
        if (!MSSFPMod.settings.EnablePossession)
            return;

        //It's ok if we miss some ticks, so the simple check is fine
        if (LastFiredTick + 2 * GenDate.TicksPerDay >= Find.TickManager.TicksGame)
            return;

        LastFiredTick = Find.TickManager.TicksGame + 2 * GenDate.TicksPerDay;

        Pawn pawn = PawnPool.RandomElementWithFallback();
        if (pawn == null)
            return;

        Building_Grave grave = GenRadial.RadialCellsAround(pawn.Position, SearchRadius, true).SelectMany(cell => Graves.Where(g => g.Position == cell)).RandomElementWithFallback();

        LastFiredTick += 4 * GenDate.TicksPerDay;

        if (pawn.def == MSSFPDefOf.MSSFP_Frogge)
        {
            pawn.health.AddHediff(MSSFPDefOf.MSS_FP_FroggeHaunt);
        }
        else
        {
            Hediff hediff = pawn.health.AddHediff(MSSFPDefOf.MSS_FP_PawnDisplayer);

            if (hediff.TryGetComp(out HediffComp_Haunt comp))
            {
                comp.SetPawnToDraw(grave.Corpse.InnerPawn);
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
        if (!MSSFPMod.settings.ShowHaunts)
            return;
        foreach (Pawn pawn in map.mapPawns.AllHumanlike)
        {
            HauntsCache.TryDrawAt(pawn.thingIDNumber, pawn.TrueCenter());
        }
    }
}
