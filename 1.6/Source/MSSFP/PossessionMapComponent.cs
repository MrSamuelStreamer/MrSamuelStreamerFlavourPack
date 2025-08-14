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
            pawn.health.hediffSet.HasHediff(MSSFPDefOf.MSS_FP_PossessionHaunt)
        );
    public IEnumerable<Pawn> PossessedPawns =>
        map.mapPawns.AllHumanlikeSpawned.Where(pawn =>
            pawn.health.hediffSet.HasHediff(MSSFPDefOf.MSS_FP_PossessionHaunt)
        );

    public int CheckInterval = GenDate.TicksPerHour;

    public override void MapComponentTick()
    {
        if (Find.TickManager.TicksGame % CheckInterval == 0 && MapHasPossessedPawn)
        {
            IEnumerable<Pawn> possessedOrNearPossessed = PossessedPawns
                .SelectMany(p => GenRadial.RadialCellsAround(p.Position, 5, true))
                .SelectMany(cell => map.thingGrid.ThingsAt(cell))
                .OfType<Pawn>();

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
