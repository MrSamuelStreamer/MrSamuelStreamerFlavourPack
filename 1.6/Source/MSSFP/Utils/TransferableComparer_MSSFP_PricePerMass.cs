using RimWorld;
using Verse;

namespace MSSFP.TransferableComparers;

/// Sort by price divided by mass, using the representative thing of the transferable.
public sealed class TransferableComparer_MSSFP_PricePerMass : TransferableComparer
{
    public override int Compare(Transferable left, Transferable right)
    {
        float leftValue = GetValue(left);
        float rightValue = GetValue(right);
        int c = leftValue.CompareTo(rightValue);
        if (c != 0)
        {
            return c;
        }

        // Tie-breaker to keep sort stable and deterministic
        Thing lt = left?.AnyThing;
        Thing rt = right?.AnyThing;
        string lLabel = lt?.LabelNoCount ?? string.Empty;
        string rLabel = rt?.LabelNoCount ?? string.Empty;
        return string.Compare(lLabel, rLabel);
    }

    private static float GetValue(Transferable t)
    {
        Thing thing = t?.AnyThing;
        if (thing == null)
        {
            return float.MinValue;
        }

        if (thing is Pawn)
        {
            // pawns are not worth anything
            return float.MinValue;
        }

        float massPerUnit = thing.GetStatValue(StatDefOf.Mass, true);
        float pricePerUnit = thing.MarketValue;

        if (massPerUnit <= 0.0001f)
        {
            // Treat as effectively infinite value density
            return float.MaxValue;
        }

        return pricePerUnit / massPerUnit;
    }
}
