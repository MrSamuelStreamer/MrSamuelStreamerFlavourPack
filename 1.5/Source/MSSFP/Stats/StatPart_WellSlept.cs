using System.Text;
using MSSFP.Comps;
using MSSFP.Hediffs;
using RimWorld;
using Verse;

namespace MSSFP.Stats;

public class StatPart_WellSlept : StatPart
{
    public static CompUpgradableBed CompForPawn(Pawn pawn)
    {
        if (!pawn.health.hediffSet.HasHediff(MSSFPDefOf.MSS_FP_WellSlept))
            return null;

        Hediff wellSlept = pawn.health.hediffSet.GetFirstHediffOfDef(MSSFPDefOf.MSS_FP_WellSlept);
        HediffCompBedUpgrade hediffCompBedUpgrade = wellSlept.TryGetComp<HediffCompBedUpgrade>();

        CompUpgradableBed comp = hediffCompBedUpgrade?.CompUpgradableBed;

        return comp;
    }

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (req.Thing is not Pawn pawn)
            return;

        CompUpgradableBed comp = CompForPawn(pawn);

        if (comp is null)
            return;

        if (comp.StatOffsets.TryGetValue(parentStat, out float offset))
        {
            val += offset;
        }
        if (comp.StatMultipliers.TryGetValue(parentStat, out float multiplier))
        {
            val *= multiplier;
        }
    }

    public override string ExplanationPart(StatRequest req)
    {
        StringBuilder sb = new();
        if (req.Thing is not Pawn pawn)
            return sb.ToString();

        CompUpgradableBed comp = CompForPawn(pawn);

        if (comp is null)
            return sb.ToString();

        if (comp.StatOffsets.TryGetValue(parentStat, out float offset))
        {
            sb.AppendLine("Well Slept: +" + offset.ToStringPercent());
        }
        if (comp.StatMultipliers.TryGetValue(parentStat, out float multiplier))
        {
            sb.AppendLine("Well Slept: x" + offset.ToStringPercent());
        }

        return sb.ToString();
    }
}
