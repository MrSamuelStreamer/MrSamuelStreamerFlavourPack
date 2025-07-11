using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.BS;

public class StatPart_BodyOffset : StatPart
{
    public float GetMultiplier(Thing t)
    {
        if (t is not Pawn pawn)
            return 1f;

        return !pawn.health.hediffSet.TryGetHediff(out Hediff_Bigger hediff) ? 1f : hediff.BodySizeMultiplier;
    }

    public override void TransformValue(StatRequest req, ref float val)
    {
        val *= GetMultiplier(req.Thing);
    }

    public override string ExplanationPart(StatRequest req)
    {
        float multiplier = GetMultiplier(req.Thing);

        return Mathf.Approximately(multiplier, 1f) ? null : "MSSFP_BS_BodySizeFactor".Translate(multiplier.ToString("0.00" + "%"));
    }
}
