using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.ReversePickpocket;

/// <summary>
/// A device a colonist can slip onto a hostile pawn. <see cref="Source"/> is the
/// worn grenade belt or the inventory shell stack; <see cref="ExplosiveDef"/> is
/// the def the eventual explosion is rebuilt from (belt projectile or shell item).
/// </summary>
public readonly struct ReversePickpocketDevice
{
    public readonly Thing Source;
    public readonly ThingDef ExplosiveDef;
    public readonly string Label;

    public ReversePickpocketDevice(Thing source, ThingDef explosiveDef, string label)
    {
        Source = source;
        ExplosiveDef = explosiveDef;
        Label = label;
    }

    public bool IsBelt => Source is Apparel;
}

/// <summary>
/// Shared logic for the reverse pick-pocket job: which devices a pawn carries,
/// which targets are valid, consuming a device, and rebuilding the explosion.
/// </summary>
public static class ReversePickpocketUtility
{
    public const int FuseTicks = 300;
    public const int PlantTicks = 45;
    private const float FleeMargin = 2f;

    public static bool Enabled => MSSFPMod.settings?.EnableReversePickpocket ?? true;

    public static bool IsValidTarget(Pawn planter, Pawn target)
    {
        return target != null
            && target != planter
            && target.Spawned
            && !target.Dead
            && target.HostileTo(planter);
    }

    public static IEnumerable<ReversePickpocketDevice> DevicesFor(Pawn pawn)
    {
        if (pawn.apparel != null)
        {
            foreach (Apparel apparel in pawn.apparel.WornApparel)
            {
                ThingDef projectile = BeltProjectile(apparel);
                if (projectile != null)
                    yield return new ReversePickpocketDevice(
                        apparel,
                        projectile,
                        "MSSFP_ReversePickpocket_AGrenade".Translate()
                    );
            }
        }

        if (pawn.inventory == null)
            yield break;

        HashSet<ThingDef> seen = new();
        foreach (Thing thing in pawn.inventory.innerContainer)
        {
            if (IsShell(thing.def) && seen.Add(thing.def))
                yield return new ReversePickpocketDevice(
                    thing,
                    thing.def,
                    Find.ActiveLanguageWorker.WithIndefiniteArticle(thing.def.label)
                );
        }
    }

    /// <summary>
    /// Re-checks that a device chosen from the float menu is still usable when the
    /// plant completes (belt still worn with a charge, shell still in inventory).
    /// </summary>
    public static bool StillAvailable(Pawn pawn, Thing source)
    {
        if (source == null || source.Destroyed)
            return false;
        if (source is Apparel apparel)
            return pawn.apparel?.WornApparel.Contains(apparel) == true
                && BeltProjectile(apparel) != null;
        return pawn.inventory?.innerContainer.Contains(source) == true && IsShell(source.def);
    }

    public static ThingDef ExplosiveDefFor(Thing source)
    {
        return source is Apparel apparel ? BeltProjectile(apparel) : source?.def;
    }

    public static void Consume(Thing source)
    {
        if (source is Apparel apparel)
        {
            apparel.GetComp<CompApparelReloadable>()?.UsedOnce();
            return;
        }
        source.SplitOff(1).Destroy(DestroyMode.Vanish);
    }

    public static float BlastRadius(ThingDef explosiveDef)
    {
        if (explosiveDef?.projectile != null)
            return explosiveDef.projectile.explosionRadius;
        return explosiveDef?.GetCompProperties<CompProperties_Explosive>()?.explosiveRadius ?? 0f;
    }

    /// <summary>
    /// Queues a move to a cell outside the blast so the planter does not stand in
    /// their own explosion. Player orders still override it.
    /// </summary>
    public static void QueueFlee(Pawn planter, Pawn target, ThingDef explosiveDef)
    {
        float distance = BlastRadius(explosiveDef) + FleeMargin;
        if (planter.Position.DistanceTo(target.Position) >= distance)
            return;

        IntVec3 dest = CellFinderLoose.GetFleeDest(planter, new List<Thing> { target }, distance);
        if (!dest.IsValid || dest == planter.Position)
            return;

        Job flee = JobMaker.MakeJob(JobDefOf.Goto, dest);
        flee.locomotionUrgency = LocomotionUrgency.Sprint;
        flee.playerForced = true;
        planter.jobs.jobQueue.EnqueueFirst(flee);
    }

    public static void Detonate(ThingDef explosiveDef, IntVec3 cell, Map map, Thing instigator)
    {
        if (explosiveDef == null || map == null || !cell.IsValid)
            return;
        if (explosiveDef.projectile != null)
            DetonateProjectile(explosiveDef, cell, map, instigator);
        else
            DetonateExplosiveComp(explosiveDef, cell, map, instigator);
    }

    private static ThingDef BeltProjectile(Apparel apparel)
    {
        CompApparelReloadable comp = apparel.GetComp<CompApparelReloadable>();
        if (comp == null || comp.RemainingCharges <= 0 || apparel.def.Verbs == null)
            return null;
        foreach (VerbProperties verb in apparel.def.Verbs)
        {
            ProjectileProperties props = verb.defaultProjectile?.projectile;
            if (props != null && props.explosionRadius > 0f && props.damageDef != null)
                return verb.defaultProjectile;
        }
        return null;
    }

    private static bool IsShell(ThingDef def)
    {
        return def.projectileWhenLoaded != null
            && def.GetCompProperties<CompProperties_Explosive>()?.explosiveRadius > 0f;
    }

    // Mirrors Projectile_Explosive.Explode, minus the flight direction.
    private static void DetonateProjectile(ThingDef def, IntVec3 cell, Map map, Thing instigator)
    {
        ProjectileProperties p = def.projectile;
        TriggerEffect(p.explosionEffect, cell, map);
        GenExplosion.DoExplosion(
            cell,
            map,
            p.explosionRadius,
            p.damageDef,
            instigator,
            p.GetDamageAmount((Thing)null),
            p.GetArmorPenetration(),
            p.soundExplode,
            null,
            def,
            null,
            p.postExplosionSpawnThingDef ?? (p.explosionSpawnsSingleFilth ? null : p.filth),
            p.postExplosionSpawnChance,
            p.postExplosionSpawnThingCount,
            p.postExplosionGasType,
            null,
            255,
            p.applyDamageToExplosionCellsNeighbors,
            p.preExplosionSpawnThingDef,
            p.preExplosionSpawnChance,
            p.preExplosionSpawnThingCount,
            p.explosionChanceToStartFire,
            p.explosionDamageFalloff,
            null,
            null,
            null,
            p.doExplosionVFX,
            p.damageDef.expolosionPropagationSpeed,
            0f,
            true,
            p.postExplosionSpawnThingDefWater,
            p.screenShakeFactor,
            null,
            null,
            p.postExplosionSpawnSingleThingDef,
            p.preExplosionSpawnSingleThingDef
        );
    }

    // Mirrors CompExplosive.Detonate for a single (stackCount 1) item.
    private static void DetonateExplosiveComp(ThingDef def, IntVec3 cell, Map map, Thing instigator)
    {
        CompProperties_Explosive p = def.GetCompProperties<CompProperties_Explosive>();
        if (p == null || p.explosiveRadius <= 0f)
            return;
        TriggerEffect(p.explosionEffect, cell, map);
        GenExplosion.DoExplosion(
            cell,
            map,
            p.explosiveRadius,
            p.explosiveDamageType,
            instigator,
            p.damageAmountBase,
            p.armorPenetrationBase,
            p.explosionSound,
            null,
            null,
            null,
            p.postExplosionSpawnThingDef,
            p.postExplosionSpawnChance,
            p.postExplosionSpawnThingCount,
            p.postExplosionGasType,
            p.postExplosionGasRadiusOverride,
            p.postExplosionGasAmount,
            p.applyDamageToExplosionCellsNeighbors,
            p.preExplosionSpawnThingDef,
            p.preExplosionSpawnChance,
            p.preExplosionSpawnThingCount,
            p.chanceToStartFire,
            p.damageFalloff,
            null,
            null,
            null,
            p.doVisualEffects,
            p.propagationSpeed,
            0f,
            p.doSoundEffects,
            null,
            1f,
            null,
            null,
            p.postExplosionSpawnSingleThingDef,
            p.preExplosionSpawnSingleThingDef
        );
    }

    private static void TriggerEffect(EffecterDef effect, IntVec3 cell, Map map)
    {
        if (effect == null)
            return;
        Effecter effecter = effect.Spawn();
        effecter.Trigger(new TargetInfo(cell, map), new TargetInfo(cell, map));
        effecter.Cleanup();
    }
}
