using RimWorld;
using Verse;

namespace MSSFP.Hediffs;

/// <summary>
/// Drives the map-level Golden Cube obsession effect from a pawn-borne implant
/// instead of a placed Thing. Logic mirrors <c>RimWorld.CompGoldenCube.CompTickInterval</c>:
/// every <c>Props.tickInterval</c> ticks, scan all map pawns and roll MTB to add
/// <see cref="HediffDefOf.CubeInterest"/> to eligible colonists/slaves.
///
/// Why reimplement instead of attaching <c>CompGoldenCube</c> to a hidden inventory item:
/// <c>CompGoldenCube</c> dereferences <c>parent.MapHeld</c> directly, which is null for
/// inventory things, NPE-ing on every tick.
/// </summary>
public class HediffComp_CubeImplant : HediffComp
{
    public HediffCompProperties_CubeImplant Props => (HediffCompProperties_CubeImplant)props;

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);

        if (!ModsConfig.AnomalyActive) return;
        if (!Pawn.IsHashIntervalTick(Props.tickInterval)) return;
        if (!Pawn.Spawned || Pawn.Map == null) return;

        // Mirror CompGoldenCube semantics: target colonists and slaves of colony,
        // skip the carrier, skip pawns already affected by the cube cycle.
        foreach (Pawn p in Pawn.Map.mapPawns.AllPawnsSpawned)
        {
            if (p == Pawn) continue;
            if (!p.IsColonist && !p.IsSlaveOfColony) continue;
            if (p.Dead || p.health == null) continue;
            if (p.health.hediffSet.HasHediff(HediffDefOf.CubeInterest)) continue;
            if (p.health.hediffSet.HasHediff(HediffDefOf.CubeWithdrawal)) continue;
            if (p.health.hediffSet.HasHediff(HediffDefOf.CubeComa)) continue;

            if (Rand.MTBEventOccurs(Props.mtbDaysAddInterest, GenDate.TicksPerDay, Props.tickInterval))
            {
                p.health.AddHediff(HediffDefOf.CubeInterest);
            }
        }
    }
}
