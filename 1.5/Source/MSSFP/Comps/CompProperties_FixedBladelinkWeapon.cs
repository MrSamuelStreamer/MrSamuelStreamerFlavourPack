using System.Collections.Generic;
using RimWorld;

namespace MSSFP.Comps;

public class CompProperties_FixedBladelinkWeapon : CompProperties_BladelinkWeapon
{
    public List<WeaponTraitDef> traits;

    public CompProperties_FixedBladelinkWeapon()
    {
        compClass = typeof(CompFixedBladelinkWeapon);
    }
}
