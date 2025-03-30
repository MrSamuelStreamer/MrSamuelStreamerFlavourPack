using System;
using RimWorld;
using Verse;

namespace MSSFP.Comps;

public class BedUpgradeWorker
{
    public BedUpgradeDef def;

    public BedUpgradeWorker() { }

    public virtual void Initialize(BedUpgradeDef parentDef)
    {
        def = parentDef;
    }

    public virtual bool CanUpgrade(CompUpgradableBed bed)
    {
        return !def.oneshot || !bed.AppliedOneshotUpgrades.Contains(def);
    }

    public virtual string ButtonText(CompUpgradableBed bed)
    {
        if (def.stat == null)
            return "+";
        if (def.multiplier.HasValue)
        {
            return "x" + def.multiplier.Value.ToStringPercent();
        }

        if (def.offset.HasValue)
        {
            return "+" + def.offset.Value.ToStringPercent();
        }

        return "+";
    }

    public virtual bool DoUpgrade(CompUpgradableBed bed)
    {
        if (bed.Levels < 1)
            return false;

        if (def.stat == null)
        {
            return true;
        }

        if (def.multiplier.HasValue)
        {
            if (!bed.StatMultipliers.TryGetValue(def.stat, out float mult))
            {
                mult = 1f;
            }

            bed.StatMultipliers[def.stat] = mult * def.multiplier.Value;
        }

        if (def.offset.HasValue)
        {
            if (!bed.StatOffsets.TryGetValue(def.stat, out float offset))
            {
                offset = 0f;
            }
            bed.StatOffsets[def.stat] = offset + def.offset.Value;
        }

        bed.Levels--;
        return true;
    }
}

public class BedUpgradeDef : Def
{
    public StatDef stat;
    public float? multiplier;
    public float? offset;
    public bool oneshot = false;
    public Type workerClass;
    public bool appliesDirectToBed = false;

    protected BedUpgradeWorker workerInt;

    public BedUpgradeWorker Worker
    {
        get
        {
            if (workerInt != null)
            {
                return workerInt;
            }

            workerInt = (BedUpgradeWorker)Activator.CreateInstance(workerClass ?? typeof(BedUpgradeWorker));
            workerInt.Initialize(this);

            return workerInt;
        }
    }
}
