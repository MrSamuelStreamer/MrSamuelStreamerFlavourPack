using System.Collections.Generic;
using RimWorld;
using RimWorld.Utility;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MSSFP.Comps;

public class CompEquippableReloadable : CompEquippable, IReloadableComp
{
    private int replenishTicksRemaining = -1;
    private int remainingCharges;

    public CompProperties_EquippableReloadable Props =>
        (CompProperties_EquippableReloadable)props;

    public Thing ReloadableThing => parent;
    public ThingDef AmmoDef => Props.ammoDef;
    public int MaxCharges => Props.maxCharges;
    public int BaseReloadTicks => Props.baseReloadTicks;

    public int RemainingCharges
    {
        get => remainingCharges;
        set => remainingCharges = value;
    }

    public string LabelRemaining => $"{RemainingCharges} / {MaxCharges}";

    private bool HasAmmoRequirement => Props.ammoDef != null;
    private bool UsesRefillCount => Props.ammoCountToRefill != 0;
    private int ChargesMissing => MaxCharges - RemainingCharges;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref replenishTicksRemaining, "replenishInTicks", -1);
        Scribe_Values.Look(ref remainingCharges, "remainingCharges");
    }

    public override void CompTick()
    {
        base.CompTick();
        if (!Props.replenishAfterCooldown || RemainingCharges != 0)
            return;

        if (replenishTicksRemaining > 0)
        {
            replenishTicksRemaining--;
            return;
        }

        RemainingCharges = MaxCharges;
    }

    public override string CompInspectStringExtra()
    {
        return "ChargesRemaining".Translate(Props.ChargeNounArgument) + ": " + LabelRemaining;
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        IEnumerable<StatDrawEntry> baseEntries = base.SpecialDisplayStats();
        if (baseEntries != null)
        {
            foreach (StatDrawEntry entry in baseEntries)
                yield return entry;
        }

        yield return new StatDrawEntry(
            StatCategoryDefOf.Weapon,
            "Stat_Thing_ReloadChargesRemaining_Name".Translate(Props.ChargeNounArgument),
            LabelRemaining,
            "Stat_Thing_ReloadChargesRemaining_Desc".Translate(Props.ChargeNounArgument),
            5441
        );
    }

    public override IEnumerable<Gizmo> CompGetEquippedGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetEquippedGizmosExtra())
            yield return gizmo;

        if (!DebugSettings.ShowDevGizmos || !NeedsReload(allowForcedReload: false))
            yield break;

        yield return new Command_Action
        {
            defaultLabel = "DEV: Reload to full",
            action = () => RemainingCharges = MaxCharges,
        };
    }

    public bool NeedsReload(bool allowForcedReload)
    {
        if (!HasAmmoRequirement)
            return false;

        if (UsesRefillCount)
            return allowForcedReload ? RemainingCharges != MaxCharges : RemainingCharges == 0;

        return RemainingCharges != MaxCharges;
    }

    public void ReloadFrom(Thing ammo)
    {
        if (!NeedsReload(allowForcedReload: true))
            return;

        if (UsesRefillCount)
        {
            if (ammo.stackCount < Props.ammoCountToRefill)
                return;

            ConsumeAndDestroyAmmo(ammo, Props.ammoCountToRefill);
            RemainingCharges = MaxCharges;
            PlayReloadSound();
            return;
        }

        if (ammo.stackCount < Props.ammoCountPerCharge)
            return;

        int chargesToAdd = Mathf.Clamp(
            ammo.stackCount / Props.ammoCountPerCharge,
            0,
            ChargesMissing
        );
        if (chargesToAdd <= 0)
            return;

        ConsumeAndDestroyAmmo(ammo, chargesToAdd * Props.ammoCountPerCharge);
        RemainingCharges += chargesToAdd;
        PlayReloadSound();
    }

    public int MinAmmoNeeded(bool allowForcedReload)
    {
        if (!NeedsReload(allowForcedReload))
            return 0;

        return UsesRefillCount ? Props.ammoCountToRefill : Props.ammoCountPerCharge;
    }

    public int MaxAmmoNeeded(bool allowForcedReload)
    {
        if (!NeedsReload(allowForcedReload))
            return 0;

        return UsesRefillCount
            ? Props.ammoCountToRefill
            : Props.ammoCountPerCharge * ChargesMissing;
    }

    public int MaxAmmoAmount()
    {
        if (!HasAmmoRequirement)
            return 0;

        return UsesRefillCount ? Props.ammoCountToRefill : Props.ammoCountPerCharge * MaxCharges;
    }

    public bool CanBeUsed(out string reason)
    {
        if (RemainingCharges > 0)
        {
            reason = null;
            return true;
        }

        reason = DisabledReason(MinAmmoNeeded(false), MaxAmmoNeeded(false));
        return false;
    }

    public string DisabledReason(int minNeeded, int maxNeeded)
    {
        if (Props.replenishAfterCooldown)
        {
            return "CommandReload_Cooldown".Translate(
                Props.CooldownVerbArgument,
                replenishTicksRemaining.ToStringTicksToPeriod().Named("TIME")
            );
        }

        if (!HasAmmoRequirement)
            return "CommandReload_NoCharges".Translate(Props.ChargeNounArgument);

        string countText = UsesRefillCount
            ? Props.ammoCountToRefill.ToString()
            : (minNeeded == maxNeeded ? minNeeded.ToString() : $"{minNeeded}-{maxNeeded}");

        return "CommandReload_NoAmmo".Translate(
            Props.ChargeNounArgument,
            Props.ammoDef.Named("AMMO"),
            countText.Named("COUNT")
        );
    }

    private static void ConsumeAndDestroyAmmo(Thing ammo, int count)
    {
        ammo.SplitOff(count).Destroy();
    }

    private void PlayReloadSound()
    {
        if (Props.soundReload == null)
            return;

        Props.soundReload.PlayOneShot(new TargetInfo(parent.PositionHeld, parent.MapHeld));
    }
}
