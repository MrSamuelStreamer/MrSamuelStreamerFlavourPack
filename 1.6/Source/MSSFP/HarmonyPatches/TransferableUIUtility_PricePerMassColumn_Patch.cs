using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

// Extension method for formatting weight/price ratios
public static class WeightPriceExtensions
{
    public static string ToStringWeightPrice(this float f, string format = null)
    {
        format ??= f is >= 10f or 0f ? "F0" : "F2";

        return "MSSFP_WeightPriceFormat".Translate(f.ToString(format));
    }
}

// Adds weight/price column to caravan transfer screens
[HarmonyPatch(typeof(TransferableOneWayWidget), "DoRow")]
public static class MSSFP_TransferableOneWayWidget_DoRow_WeightPrice
{
    static bool Prefix(TransferableOneWayWidget __instance, Rect rect, TransferableOneWay trad, int index, float availableMass)
    {
        DoRowWithWeightPriceColumn(__instance, rect, trad, index, availableMass);
        return false; // Skip original method
    }

    // Draws the weight/price ratio cell
    private static void DrawWeightPriceCell(Rect rect, TransferableOneWay trad)
    {
        if (!trad.HasAnyThing)
            return;

        Widgets.DrawHighlightIfMouseover(rect);

        Thing thing = trad.AnyThing;
        float mass = thing.GetStatValue(StatDefOf.Mass, true);
        float price = thing.MarketValue;

        float pricePerMass = mass > 0.0001f ? price / mass : 0f;
        string text = pricePerMass.ToStringWeightPrice();

        Widgets.Label(rect, text);
        TooltipHandler.TipRegion(rect, "Weight/Price");
    }

    // Custom row implementation with weight/price column inserted between mass and count
    static void DoRowWithWeightPriceColumn(TransferableOneWayWidget widget, Rect rect, TransferableOneWay trad, int index, float availableMass)
    {
        if (index % 2 == 1)
            Widgets.DrawLightHighlight(rect);

        Text.Font = GameFont.Small;
        Widgets.BeginGroup(rect);

        float currentX = rect.width;
        int maxCount = trad.MaxCount;

        // Transfer controls (buttons)
        Rect adjustRect = new(currentX - 240f, 0f, 240f, rect.height);
        TransferableUIUtility.DoCountAdjustInterface(adjustRect, trad, index, 0, maxCount, false, null, false);
        currentX -= 240f;

        // Market value column
        Rect marketValueRect = new(currentX - 100f, 0f, 100f, rect.height);
        Text.Anchor = TextAnchor.MiddleLeft;
        if (trad.HasAnyThing && trad.ThingDef.tradeability != Tradeability.None)
        {
            Widgets.DrawHighlightIfMouseover(marketValueRect);
            Widgets.Label(marketValueRect, trad.AnyThing.MarketValue.ToStringMoney());
            if (Mouse.IsOver(marketValueRect))
                TooltipHandler.TipRegion(marketValueRect, "MarketValueTip".Translate());
        }

        currentX -= 100f;

        // Mass column
        Rect massRect = new(currentX - 100f, 0f, 100f, rect.height);
        Text.Anchor = TextAnchor.MiddleLeft;
        if (trad.HasAnyThing)
        {
            Widgets.DrawHighlightIfMouseover(massRect);
            float mass = trad.AnyThing.GetStatValue(StatDefOf.Mass, true);
            GUI.color = TransferableOneWayWidget.ItemMassColor;
            Widgets.Label(massRect, mass.ToStringMass());
            GUI.color = Color.white;
            if (Mouse.IsOver(massRect))
                TooltipHandler.TipRegion(massRect, "Weight of this item.");
        }

        currentX -= 100f;

        // Weight/price column (our custom addition)
        if (trad.HasAnyThing && !(trad.AnyThing is Pawn))
        {
            Rect weightPriceRect = new(currentX - 85f, 0f, 85f, rect.height);
            DrawWeightPriceCell(weightPriceRect, trad);
        }

        currentX -= 85f;

        // Count column
        if (trad.HasAnyThing && maxCount > 1)
        {
            Rect countRect = new(currentX - 65f, 0f, 65f, rect.height);
            Widgets.DrawHighlightIfMouseover(countRect);
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect labelRect = new(countRect.x + 5f, countRect.y, countRect.width - 10f, countRect.height);
            Widgets.Label(labelRect, maxCount.ToStringCached());
            if (Mouse.IsOver(countRect))
                TooltipHandler.TipRegion(countRect, "Number available to send with the caravan.");
        }

        currentX -= 65f;

        // Item name/info (gets remaining space)
        Rect idRect = new(0f, 0f, currentX, rect.height);
        TransferableUIUtility.DrawTransferableInfo(trad, idRect, Color.white);

        GenUI.ResetLabelAlign();
        Widgets.EndGroup();
    }
}
