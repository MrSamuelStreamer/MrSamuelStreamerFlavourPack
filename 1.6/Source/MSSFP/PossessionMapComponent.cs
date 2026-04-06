using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP;

public class PossessionMapComponent(Map map) : MapComponent(map)
{
    public Map Map => map;

    public bool MapHasPossessedPawn =>
        map.mapPawns.AllHumanlikeSpawned.Any(pawn =>
            pawn.health.hediffSet.HasHediff(MSSFPDefOf.MSS_FP_Echo)
        );
    public IEnumerable<Pawn> PossessedPawns =>
        map.mapPawns.AllHumanlikeSpawned.Where(pawn =>
            pawn.health.hediffSet.HasHediff(MSSFPDefOf.MSS_FP_Echo)
        );

    public int CheckInterval = GenDate.TicksPerHour;

    public override void MapComponentTick()
    {
        if (Find.TickManager.TicksGame % CheckInterval == 0 && MapHasPossessedPawn)
        {
            List<Pawn> possessedOrNearPossessed = new();
            foreach (Pawn possessed in PossessedPawns)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(possessed.Position, 5, true))
                {
                    foreach (Thing thing in map.thingGrid.ThingsAt(cell))
                    {
                        if (thing is Pawn p)
                            possessedOrNearPossessed.Add(p);
                    }
                }
            }

            Pawn target = possessedOrNearPossessed.RandomElementWithFallback();
            if (target != null)
            {
                target.needs.mood.thoughts.memories.TryGainMemory(
                    MSSFPDefOf.MSS_FP_PossessedThought
                );
            }
        }
    }
}
