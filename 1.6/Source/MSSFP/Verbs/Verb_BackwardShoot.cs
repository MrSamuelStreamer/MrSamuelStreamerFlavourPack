using RimWorld;
using Verse;
using Verse.Sound;

namespace MSSFP.Verbs;

public class Verb_BackwardShoot : Verb_Shoot
{
    protected override bool TryCastShot()
    {
        if (CasterPawn == null || verbProps?.defaultProjectile?.projectile == null)
            return false;

        if (!verbProps.soundCast.NullOrUndefined())
        {
            verbProps.soundCast.PlayOneShot(new TargetInfo(CasterPawn.Position, CasterPawn.Map));
        }

        if (verbProps.muzzleFlashScale > 0.01f && CasterPawn.Map != null)
        {
            FleckMaker.ThrowMicroSparks(CasterPawn.DrawPos, CasterPawn.Map);
        }

        var projectileProps = verbProps.defaultProjectile.projectile;
        int damageAmount = projectileProps.GetDamageAmount(EquipmentSource);

        if (damageAmount <= 0)
            return false;

        float armorPenetration = projectileProps.GetArmorPenetration(EquipmentSource);

        DamageInfo damageInfo = new DamageInfo(
            projectileProps.damageDef,
            damageAmount,
            armorPenetration,
            -1f,
            CasterPawn,
            null,
            EquipmentSource?.def,
            DamageInfo.SourceCategory.ThingOrUnknown,
            currentTarget.Thing
        );

        CasterPawn.TakeDamage(damageInfo);

        BattleLogEntry_RangedImpact logEntry = new BattleLogEntry_RangedImpact(
            CasterPawn,
            CasterPawn,
            currentTarget.Thing,
            EquipmentSource?.def,
            verbProps.defaultProjectile,
            null
        );
        Find.BattleLog.Add(logEntry);

        return true;
    }

    public override bool CanHitTarget(LocalTargetInfo targ)
    {
        return true;
    }

    public override bool Available()
    {
        return base.Available() && CasterPawn != null;
    }
}
