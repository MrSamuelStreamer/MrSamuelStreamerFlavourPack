using MSSFP.Hediffs;
using RimWorld;
using Verse;

namespace MSSFP.Genes;

/// <summary>
/// Pawn can "sense" ghosts nearby, gaining a brief mood buff.
/// The weighted PawnPool logic and 2× progression multiplier live in
/// HauntedMapComponent and HediffComp_HauntProgression respectively.
/// </summary>
public class Gene_HauntSensitive : Gene
{
    // Check every ~15 in-game seconds at 1× speed.
    private const int CheckInterval = 600;

    // Radius within which a haunted pawn triggers the mood buff.
    private const float SenseRadius = 10f;

    public override void Tick()
    {
        if (!pawn.IsHashIntervalTick(CheckInterval))
            return;
        if (pawn.Map == null)
            return;

        foreach (Pawn other in pawn.Map.mapPawns.AllHumanlike)
        {
            if (other == pawn)
                continue;
            if (!HauntsCache.Haunts.ContainsKey(other.thingIDNumber))
                continue;
            if (!other.Position.InHorDistOf(pawn.Position, SenseRadius))
                continue;

            pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(
                MSSFPDefOf.MSS_FP_Gene_SenseGhost
            );
            return;
        }
    }
}
