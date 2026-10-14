using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace MSSFP.Verbs;

public class Verb_ShootRandom : Verb_Shoot
{
    private static List<ThingDef> cachedProjectiles;

    public override ThingDef Projectile
    {
        get
        {
            if (cachedProjectiles.NullOrEmpty())
            {
                cachedProjectiles = DefDatabase<ThingDef>.AllDefsListForReading.FindAll(def =>
                    def.projectile != null
                );
            }

            return cachedProjectiles.RandomElement();
        }
    }

    protected override bool TryCastShot()
    {
        if (!base.TryCastShot())
            return false;

        if (Caster.Map != null)
        {
            MSSFPDefOf.MSSFP_GachaGun_Fire.PlayOneShot(
                new TargetInfo(Caster.Position, Caster.Map)
            );
        }

        return true;
    }
}
