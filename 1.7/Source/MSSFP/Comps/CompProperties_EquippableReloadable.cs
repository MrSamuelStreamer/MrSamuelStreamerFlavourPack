using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class CompProperties_EquippableReloadable : CompProperties
{
    public int maxCharges;
    public ThingDef ammoDef;
    public int ammoCountToRefill;
    public int ammoCountPerCharge;
    public int baseReloadTicks = 60;
    public bool replenishAfterCooldown;
    public SoundDef soundReload;

    [MustTranslate]
    public string chargeNoun = "charge";

    [MustTranslate]
    public string cooldownGerund = "on cooldown";

    public CompProperties_EquippableReloadable()
    {
        compClass = typeof(CompEquippableReloadable);
    }

    public NamedArgument CooldownVerbArgument =>
        cooldownGerund.CapitalizeFirst().Named("COOLDOWNGERUND");

    public NamedArgument ChargeNounArgument => chargeNoun.Named("CHARGENOUN");

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string configError in base.ConfigErrors(parentDef))
            yield return configError;

        if (ammoDef != null && ammoCountToRefill == 0 && ammoCountPerCharge == 0)
        {
            yield return "Reloadable component has ammoDef but one of ammoCountToRefill or ammoCountPerCharge must be set";
        }

        if (ammoCountToRefill != 0 && ammoCountPerCharge != 0)
        {
            yield return "Reloadable component: specify only one of ammoCountToRefill and ammoCountPerCharge";
        }
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        foreach (StatDrawEntry specialDisplayStat in base.SpecialDisplayStats(req))
            yield return specialDisplayStat;

        if (!req.HasThing)
        {
            yield return new StatDrawEntry(
                StatCategoryDefOf.Weapon,
                "Stat_Thing_ReloadMaxCharges_Name".Translate(ChargeNounArgument),
                maxCharges.ToString(),
                "Stat_Thing_ReloadMaxCharges_Desc".Translate(ChargeNounArgument),
                5440
            );
        }

        if (ammoDef == null)
            yield break;

        if (ammoCountToRefill != 0)
        {
            yield return new StatDrawEntry(
                StatCategoryDefOf.Weapon,
                "Stat_Thing_ReloadRefill_Name".Translate(ChargeNounArgument),
                $"{ammoCountToRefill} {ammoDef.label}",
                "Stat_Thing_ReloadRefill_Desc".Translate(ChargeNounArgument),
                5440
            );
        }
        else
        {
            yield return new StatDrawEntry(
                StatCategoryDefOf.Weapon,
                "Stat_Thing_ReloadPerCharge_Name".Translate(ChargeNounArgument),
                $"{ammoCountPerCharge} {ammoDef.label}",
                "Stat_Thing_ReloadPerCharge_Desc".Translate(ChargeNounArgument),
                5440
            );
        }
    }
}
