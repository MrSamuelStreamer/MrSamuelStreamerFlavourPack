using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP;

public class DamageWorker_DestructiveExtinguish : DamageWorker_AddInjury
{
    private static readonly FloatRange FireSizeRange = new(0.4f, 0.8f);

    public static FloatRange PawnDamageModifier = new(0.0001f, 0.02f);

    public override DamageResult Apply(DamageInfo dinfo, Thing victim)
    {
        Map map = victim.Map;
        IntVec3 pos = victim.Position;

        if (map is { roofGrid: not null } && map.roofGrid.Roofed(pos))
        {
            victim.Map.roofGrid.SetRoof(pos, null);
        }

        DamageResult damageResult = base.Apply(dinfo, victim);
        Fire fire = victim as Fire;

        if (fire is null || fire.Destroyed)
        {
            Thing attachment = victim?.GetAttachment(ThingDefOf.Fire);
            if (attachment != null)
                fire = (Fire)attachment;
        }

        if (fire is { Destroyed: false })
        {
            base.Apply(dinfo, victim);
            fire.fireSize -= dinfo.Amount * 0.1f;
            if (fire.fireSize < 0.1f)
                fire.Destroy(DestroyMode.Vanish);
        }
        if (victim is not Pawn pawn)
            return damageResult;

        Hediff hediff = HediffMaker.MakeHediff(dinfo.Def.hediff, pawn);
        hediff.Severity = dinfo.Amount * PawnDamageModifier.RandomInRange;
        pawn.health.AddHediff(hediff, dinfo: dinfo);
        return damageResult;
    }

    public override void ExplosionStart(Explosion explosion, List<IntVec3> cellsToAffect)
    {
        base.ExplosionStart(explosion, cellsToAffect);
        Effecter effecter = EffecterDefOf.Vaporize_Heatwave.Spawn();
        effecter.Trigger(new TargetInfo(explosion.Position, explosion.Map), TargetInfo.Invalid);
        effecter.Cleanup();
    }

    public override void ExplosionAffectCell(Explosion explosion, IntVec3 c, List<Thing> damagedThings, List<Thing> ignoredThings, bool canThrowMotes)
    {
        bool inRange = c.DistanceTo(explosion.Position) <= explosion.radius;
        base.ExplosionAffectCell(explosion, c, damagedThings, ignoredThings, canThrowMotes && inRange);
        if (!inRange)
            return;
        FleckMaker.ThrowSmoke(c.ToVector3Shifted(), explosion.Map, explosion.radius);
    }

    protected override void ExplosionDamageThing(Explosion explosion, Thing t, List<Thing> damagedThings, List<Thing> ignoredThings, IntVec3 cell)
    {
        if (cell.DistanceTo(explosion.Position) > explosion.radius)
            return;
        base.ExplosionDamageThing(explosion, t, damagedThings, ignoredThings, cell);
    }

    private static List<IntVec3> openCells = [];
    private static List<IntVec3> adjWallCells = [];

    public override IEnumerable<IntVec3> ExplosionCellsToHit(
        IntVec3 center,
        Map map,
        float radius,
        IntVec3? needLOSToCell1 = null,
        IntVec3? needLOSToCell2 = null,
        FloatRange? affectedAngle = null
    )
    {
        openCells.Clear();
        adjWallCells.Clear();

        int cellsInRadius = GenRadial.NumCellsInRadius(radius);
        for (int i = 0; i < cellsInRadius; ++i)
        {
            IntVec3 cellToCheck = center + GenRadial.RadialPattern[i];
            if (cellToCheck.InBounds(map))
            {
                openCells.Add(cellToCheck);
            }
        }

        foreach (IntVec3 openCell in openCells)
        {
            if (!openCell.Walkable(map))
                continue;
            for (int index2 = 0; index2 < 4; ++index2)
            {
                IntVec3 c = openCell + GenAdj.CardinalDirections[index2];
                if (c.InHorDistOf(center, radius) && c.InBounds(map) && !c.Standable(map) && c.GetEdifice(map) != null && !openCells.Contains(c) && !adjWallCells.Contains(c))
                    adjWallCells.Add(c);
            }
        }

        foreach (IntVec3 cell in openCells)
            yield return cell;
        foreach (IntVec3 cell in adjWallCells)
            yield return cell;
    }
}
