using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Comps.Map;

public class LoversRetreatMapComponent(Verse.Map map) : MapComponent(map), IThingHolder
{
    private ThingOwner<Pawn> innerContainer = new ThingOwner<Pawn>();
    public List<Pawn> PawnsToStoreNextTick = new List<Pawn>();

    public int NextCheck;
    public class Pair: IExposable
    {
        public Pawn firstLover;
        public Pawn secondLover;

        public int LeftAtTick;
        public int ExcpectedBackTick;

        public void ExposeData()
        {
            Scribe_References.Look(ref firstLover, "firstLover");
            Scribe_References.Look(ref secondLover, "secondLover");
            Scribe_Values.Look(ref LeftAtTick, "LeftAtTick", 0);
            Scribe_Values.Look(ref ExcpectedBackTick, "ExcpectedBackTick", 0);
        }
    }

    public List<Pair> pairs = [];

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref pairs, "pairs", LookMode.Deep);
        Scribe_Collections.Look(ref PawnsToStoreNextTick, "PawnsToStoreNextTick", LookMode.Reference);
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        Scribe_Values.Look(ref NextCheck, "NextCheck", GenDate.TicksPerDay * 5);

        innerContainer ??= new ThingOwner<Pawn>(this);
    }

    public virtual void AddPair(Pawn first, Pawn second, int ticksAway)
    {
        Pair p = new Pair { firstLover = first, secondLover = second, ExcpectedBackTick = ticksAway + Find.TickManager.TicksGame, LeftAtTick = Find.TickManager.TicksGame };
        pairs.Add(p);
    }

    public override void MapComponentTick()
    {
        if (Find.TickManager.TicksGame > NextCheck)
        {
            NextCheck = Find.TickManager.TicksGame + Rand.RangeInclusive(GenDate.TicksPerDay * 10, GenDate.TicksPerDay * 30);
            IncidentParms parms = new IncidentParms { target = map };
            if (MSSFPDefOf.MSSFP_Lovers_Retreat.Worker.CanFireNow(parms))
            {
                MSSFPDefOf.MSSFP_Lovers_Retreat.Worker.TryExecute(parms);
            }
        }

        if(Find.TickManager.TicksGame % 600 == 0) return;

        List<Pair> toRemove = [];
        foreach (Pair pair in pairs)
        {
            if (pair.ExcpectedBackTick <= Find.TickManager.TicksGame)
            {
                toRemove.Add(pair);

                if (RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 loc, map, CellFinder.EdgeRoadChance_Ignore))
                {
                    GenSpawn.Spawn(pair.firstLover, loc, map);
                    GenSpawn.Spawn(pair.secondLover, loc, map);
                    innerContainer.Remove(pair.firstLover);
                    innerContainer.Remove(pair.secondLover);
                    if (Rand.Chance(0.5f))
                    {
                        Hediff_Pregnant preg = pair.firstLover.health.AddHediff(HediffDefOf.PregnantHuman) as Hediff_Pregnant;
                        preg!.SetParents(pair.firstLover, pair.secondLover, PregnancyUtility.GetInheritedGeneSet(pair.secondLover, pair.firstLover));
                    }
                    if (Rand.Chance(0.5f))
                    {
                        Hediff_Pregnant preg = pair.secondLover.health.AddHediff(HediffDefOf.PregnantHuman) as Hediff_Pregnant;
                        preg!.SetParents(pair.secondLover, pair.firstLover, PregnancyUtility.GetInheritedGeneSet(pair.firstLover, pair.secondLover));
                    }
                }

            }
        }

        pairs.RemoveAll(p=>toRemove.Contains(p));


        foreach (Pawn pawn in PawnsToStoreNextTick)
        {
            IntVec3 loc = pawn.Position;
            if(pawn.Spawned)
                pawn.DeSpawn();

            if (!innerContainer.Contains(pawn) && innerContainer.TryAdd(pawn))
            {
                map.mapDrawer?.MapMeshDirty(loc, MapMeshFlagDefOf.Things);
            }
        }

        PawnsToStoreNextTick.Clear();
    }

    public void StorePawn(Pawn pawn)
    {
        PawnsToStoreNextTick.Add(pawn);
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public ThingOwner GetDirectlyHeldThings() => innerContainer;

    public IThingHolder ParentHolder => null;
}
