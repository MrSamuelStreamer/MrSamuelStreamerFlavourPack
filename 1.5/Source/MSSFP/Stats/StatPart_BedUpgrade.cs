using System.Text;
using MSSFP.Comps;
using MSSFP.Hediffs;
using RimWorld;
using Verse;

namespace MSSFP.Stats;

public class StatPart_BedUpgrade : StatPart
{
    public override void TransformValue(StatRequest req, ref float val)
    {
        if (req.Thing is not Building_Bed bed || !bed.TryGetComp<CompUpgradableBed>(out CompUpgradableBed comp))
            return;

        if (comp.StatMultipliers.TryGetValue(parentStat, out float multiplier))
        {
            val *= multiplier;
        }
    }

    public override string ExplanationPart(StatRequest req)
    {
        StringBuilder sb = new();
        if (req.Thing is not Building_Bed bed || !bed.TryGetComp<CompUpgradableBed>(out CompUpgradableBed comp))
            return sb.ToString();

        if (comp is null)
            return sb.ToString();

        if (comp.StatMultipliers.TryGetValue(parentStat, out float multiplier))
        {
            sb.AppendLine("Well Slept: x" + multiplier.ToStringPercent());
        }

        return sb.ToString();
    }
}
