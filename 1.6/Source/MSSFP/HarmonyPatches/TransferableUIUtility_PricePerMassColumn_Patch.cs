using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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
        if (format == null)
            format = f >= 10f || f == 0f ? "F0" : "F2";

        return "MSSFP_WeightPriceFormat".Translate(f.ToString(format));
    }
}

// DISABLED: Trade menu weight/price column - only keeping caravan functionality
/*
// Transpiler + postfix for TradeUI.DrawTradeableRow to modify column widths and add our column
[HarmonyPatch(typeof(TradeUI), "DrawTradeableRow")]
public static class MSSFP_TradeUI_DrawTradeableRow_WeightPrice
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        ModLog.Log("[MSSFP WeightPrice] Applying TradeUI transpiler to modify column widths...");

        var codes = new List<CodeInstruction>(instructions);
        bool patched = false;

        for (int i = 0; i < codes.Count; i++)
        {
            // Target the same constants that are used in TradeUI
            if (codes[i].LoadsConstant(75f)) // Count column width
            {
                codes[i].operand = 55f; // Reduce by 20px (was 60f, now 55f)
                patched = true;
                ModLog.Log("[MSSFP WeightPrice] Modified TradeUI count column: 75f -> 55f");
            }
            else if (codes[i].LoadsConstant(100f)) // Market value column width
            {
                codes[i].operand = 80f; // Reduce by 20px (was 85f, now 80f)
                patched = true;
                ModLog.Log("[MSSFP WeightPrice] Modified TradeUI market value column: 100f -> 80f");
            }
            else if (codes[i].LoadsConstant(175f)) // Column spacing/positioning constants from the decompiled method
            {
                codes[i].operand = 150f; // Reduce by 25px to give more space
                patched = true;
                ModLog.Log("[MSSFP WeightPrice] Modified TradeUI spacing: 175f -> 150f");
            }
            else if (codes[i].LoadsConstant(240f)) // Another positioning constant
            {
                codes[i].operand = 200f; // Reduce by 40px
                patched = true;
                ModLog.Log("[MSSFP WeightPrice] Modified TradeUI positioning: 240f -> 200f");
            }
        }

        ModLog.Log($"[MSSFP WeightPrice] TradeUI transpiler completed, patched: {patched}");
        return codes;
    }

    static void Postfix(Rect rect, Tradeable trad, int index)
    {
        try
        {
            ModLog.Log($"[MSSFP WeightPrice] TradeUI postfix called for {trad?.Label ?? "null"}");

            if (trad == null || trad.IsCurrency || !trad.TraderWillTrade || !trad.HasAnyThing || trad.AnyThing is Pawn)
                return;

            // Position our column with fine-tuned spacing
            float x = rect.xMax - 260f; // Move slightly right from previous position
            Rect weightPriceRect = new Rect(x, rect.y, 100f, rect.height); // Wider column for better visibility

            DrawWeightPriceCell(weightPriceRect, trad);
            ModLog.Log($"[MSSFP WeightPrice] Drew TradeUI cell at x={x}");
        }
        catch (System.Exception ex)
        {
            ModLog.Error($"[MSSFP WeightPrice] Error in TradeUI postfix: {ex}");
        }
    }

    private static void DrawWeightPriceCell(Rect rect, Tradeable trad)
    {
        rect = GenUI.Rounded(rect);

        if (Mouse.IsOver(rect))
        {
            Widgets.DrawHighlight(rect);
            TooltipHandler.TipRegion(rect, GetWeightPriceTooltip(trad));
        }

        Thing thing = trad.AnyThing;
        float mass = thing.GetStatValue(StatDefOf.Mass, true);
        float price = thing.MarketValue; // Use base market value for now

        // Apply color coding based on price efficiency
        Color originalColor = GUI.color;
        if (mass > 0.0001f)
        {
            float pricePerMass = price / mass;
            // Simple color coding: green for efficient, red for inefficient
            if (pricePerMass > 50f)
                GUI.color = new Color(0.5f, 1f, 0.5f); // Light green
            else if (pricePerMass < 5f)
                GUI.color = new Color(1f, 0.5f, 0.5f); // Light red
            else
                GUI.color = Color.white;
        }

        string text;
        if (mass <= 0.0001f)
        {
            text = "∞";
        }
        else
        {
            float pricePerMass = price / mass;
            text = pricePerMass.ToStringWeightPrice();
        }

        Rect labelRect = new Rect(rect.x + 5f, rect.y, rect.width - 10f, rect.height);
        TextAnchor oldAnchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(labelRect, text);
        Text.Anchor = oldAnchor;
        GUI.color = originalColor;
    }

    private static string GetWeightPriceTooltip(Tradeable trad)
    {
            Thing thing = trad.AnyThing;
                float mass = thing.GetStatValue(StatDefOf.Mass, true);
                float price = thing.MarketValue;

        string priceText = "Market Value: " + price.ToStringMoney();
        string massText = "Mass: " + mass.ToStringMass();

        string ratioText;
        if (mass <= 0.0001f)
        {
            ratioText = "Value per mass: ∞";
        }
        else
        {
            float ratio = price / mass;
            ratioText = "Value per mass: " + ratio.ToStringWeightPrice();
        }

        return $"{priceText}\n{massText}\n\n{ratioText}";
    }
}
*/

// Adds weight/price column to caravan transfer screens
[HarmonyPatch(typeof(TransferableOneWayWidget), "DoRow")]
public static class MSSFP_TransferableOneWayWidget_DoRow_WeightPrice
{
    static bool Prefix(
        TransferableOneWayWidget __instance,
        Rect rect,
        TransferableOneWay trad,
        int index,
        float availableMass
    )
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
    static void DoRowWithWeightPriceColumn(
        TransferableOneWayWidget widget,
        Rect rect,
        TransferableOneWay trad,
        int index,
        float availableMass
    )
    {
        if (index % 2 == 1)
            Widgets.DrawLightHighlight(rect);

        Text.Font = GameFont.Small;
        Widgets.BeginGroup(rect);

        float currentX = rect.width;
        int maxCount = trad.MaxCount;

        // Transfer controls (buttons)
        Rect adjustRect = new Rect(currentX - 240f, 0f, 240f, rect.height);
        TransferableUIUtility.DoCountAdjustInterface(adjustRect, trad, index, 0, maxCount, false, null, false);
        currentX -= 240f;

        // Market value column
        Rect marketValueRect = new Rect(currentX - 100f, 0f, 100f, rect.height);
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
        Rect massRect = new Rect(currentX - 100f, 0f, 100f, rect.height);
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
            Rect weightPriceRect = new Rect(currentX - 85f, 0f, 85f, rect.height);
            DrawWeightPriceCell(weightPriceRect, trad);
        }
        currentX -= 85f;

        // Count column
        if (trad.HasAnyThing && maxCount > 1)
        {
            Rect countRect = new Rect(currentX - 65f, 0f, 65f, rect.height);
            Widgets.DrawHighlightIfMouseover(countRect);
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect labelRect = new Rect(countRect.x + 5f, countRect.y, countRect.width - 10f, countRect.height);
            Widgets.Label(labelRect, maxCount.ToStringCached());
            if (Mouse.IsOver(countRect))
                TooltipHandler.TipRegion(countRect, "Number available to send with the caravan.");
        }
        currentX -= 65f;

        // Item name/info (gets remaining space)
        Rect idRect = new Rect(0f, 0f, currentX, rect.height);
        TransferableUIUtility.DrawTransferableInfo(trad, idRect, Color.white);

        GenUI.ResetLabelAlign();
        Widgets.EndGroup();
    }
}
