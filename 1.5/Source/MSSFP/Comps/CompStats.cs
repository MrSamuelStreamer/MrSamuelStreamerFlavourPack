using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Comps;

public class CompStats : ThingComp
{
    public CompProperties_Stats Props => (CompProperties_Stats)props;

    public override float GetStatFactor(StatDef stat)
    {
        if (Props.factors.NullOrEmpty())
            return base.GetStatFactor(stat);
        StatModifier factor = Props.factors.FirstOrDefault(o => o.stat == stat);
        if (factor == null)
            return base.GetStatFactor(stat);
        return base.GetStatOffset(stat) * factor.value;
    }

    public override float GetStatOffset(StatDef stat)
    {
        if (Props.offsets.NullOrEmpty())
            return base.GetStatOffset(stat);
        StatModifier offset = Props.offsets.FirstOrDefault(o => o.stat == stat);
        if (offset == null)
            return base.GetStatOffset(stat);
        return base.GetStatOffset(stat) * offset.value;
    }

    public override void GetStatsExplanation(StatDef stat, StringBuilder sb)
    {
        float factor = GetStatFactor(stat);
        if (!Mathf.Approximately(factor, 1f))
        {
            sb.AppendLine($"{Props.explanation}: x{factor * 100f:0}%");
        }

        float offset = GetStatOffset(stat);
        if (!Mathf.Approximately(offset, 0f))
        {
            sb.AppendLine($"{Props.explanation}: {offset:0}%");
        }
    }
}
