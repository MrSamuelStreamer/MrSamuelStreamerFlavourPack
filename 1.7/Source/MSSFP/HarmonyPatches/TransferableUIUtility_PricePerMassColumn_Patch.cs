using System;
using System.Collections.Generic;
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

// Adds a weight/price column to caravan transfer screens.
//
// NOTE ON APPROACH: DoRow leaves no spare rect for an extra column, and its layout
// state (drawMass, drawMarketValue, stoppingPoints, etc.) lives in private fields,
// so a non-destructive postfix/transpiler can't easily insert a column without
// re-deriving that state anyway. This prefix still fully replaces DoRow, but the
// replacement below uses Harmony Traverse to read the widget's private flags and
// invoke its private per-column draw methods, so every vanilla column/flag is
// reproduced faithfully: mass stopping points, readOnly/playerPawnsReadOnly
// enforcement, drawMarketValue/drawMass gating, daysUntilRot/itemNutrition/
// foragedFoodPerDay(+grazeability)/nutritionEatenPerDay/mech-energy columns,
// ideo/xenotype/slave icons (+ slave name color), equipped-weapon icon, and
// TransferableUIUtility.DoExtraIcons. Any other mod's Harmony patch aimed at the
// original DoRow body itself will still not run — only a true postfix/transpiler
// on the original would preserve that; that is not attempted here.
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

    // Faithful re-implementation of TransferableOneWayWidget.DoRow (see class-level
    // comment) with the weight/price column inserted after the mass column.
    static void DoRowWithWeightPriceColumn(
        TransferableOneWayWidget widget,
        Rect rect,
        TransferableOneWay trad,
        int index,
        float availableMass
    )
    {
        Traverse w = Traverse.Create(widget);

        if (index % 2 == 1)
            Widgets.DrawLightHighlight(rect);

        Text.Font = GameFont.Small;
        Widgets.BeginGroup(rect);

        float width = rect.width;
        int maxCount = trad.MaxCount;

        Rect adjustRect = new Rect(width - 240f, 0f, 240f, rect.height);
        List<TransferableCountToTransferStoppingPoint> stoppingPoints = new List<TransferableCountToTransferStoppingPoint>();
        Func<float> availableMassGetter = w.Field("availableMassGetter").GetValue<Func<float>>();
        bool includePawnsMassInMassUsage = w.Field("includePawnsMassInMassUsage").GetValue<bool>();
        if (availableMassGetter != null && (!(trad.AnyThing is Pawn) || includePawnsMassInMassUsage))
        {
            float mass = w.Method("GetMass", trad.AnyThing).GetValue<float>();
            float projected = availableMass + mass * trad.CountToTransfer;
            int threshold = projected <= 0f ? 0 : Mathf.FloorToInt(projected / mass);
            stoppingPoints.Add(new TransferableCountToTransferStoppingPoint(threshold, "M<", ">M"));
        }

        Pawn pawn = trad.AnyThing as Pawn;
        bool isEnforcedReadOnlyPawn = pawn != null && (pawn.IsColonist || pawn.IsPrisonerOfColony);
        bool playerPawnsReadOnly = w.Field("playerPawnsReadOnly").GetValue<bool>();
        TransferableUIUtility.DoCountAdjustInterface(
            adjustRect,
            trad,
            index,
            0,
            maxCount,
            false,
            stoppingPoints,
            (playerPawnsReadOnly && isEnforcedReadOnlyPawn) || widget.readOnly
        );
        width -= 240f;

        if (w.Field("drawMarketValue").GetValue<bool>())
        {
            Rect marketValueRect = new Rect(width - 100f, 0f, 100f, rect.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            w.Method("DrawMarketValue", marketValueRect, trad).GetValue();
            width -= 100f;
        }

        if (w.Field("drawMass").GetValue<bool>())
        {
            Rect massRect = new Rect(width - 100f, 0f, 100f, rect.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            w.Method("DrawMass", massRect, trad, availableMass).GetValue();
            width -= 100f;
        }

        // Weight/price column (our custom addition), inserted after the mass column.
        if (trad.HasAnyThing && !(trad.AnyThing is Pawn))
        {
            Rect weightPriceRect = new Rect(width - 85f, 0f, 85f, rect.height);
            DrawWeightPriceCell(weightPriceRect, trad);
            width -= 85f;
        }

        if (w.Field("drawDaysUntilRot").GetValue<bool>())
        {
            Rect r = new Rect(width - 75f, 0f, 75f, rect.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            w.Method("DrawDaysUntilRot", r, trad).GetValue();
            width -= 75f;
        }

        if (w.Field("drawItemNutrition").GetValue<bool>())
        {
            Rect r = new Rect(width - 75f, 0f, 75f, rect.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            w.Method("DrawItemNutrition", r, trad).GetValue();
            width -= 75f;
        }

        if (w.Field("drawForagedFoodPerDay").GetValue<bool>())
        {
            Rect r = new Rect(width - 75f, 0f, 75f, rect.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            bool grazed = w.Method("DrawGrazeability", r, trad).GetValue<bool>();
            if (!grazed)
                w.Method("DrawForagedFoodPerDay", r, trad).GetValue();
            width -= 75f;
        }

        bool drawNutritionEatenPerDay = w.Field("drawNutritionEatenPerDay").GetValue<bool>();
        bool drawMechEnergy = w.Field("drawMechEnergy").GetValue<bool>();
        if (drawNutritionEatenPerDay || drawMechEnergy)
        {
            bool handledNutrition = false;
            if (drawNutritionEatenPerDay)
            {
                Rect r = new Rect(width - 75f, 0f, 75f, rect.height);
                Text.Anchor = TextAnchor.MiddleLeft;
                handledNutrition = w.Method("DrawNutritionEatenPerDay", r, trad).GetValue<bool>();
            }
            if (ModsConfig.BiotechActive && drawMechEnergy && !handledNutrition)
            {
                Rect r = new Rect(width - 75f, 0f, 75f, rect.height);
                w.Method("DrawMechEnergy", r, trad).GetValue();
            }
            width -= 75f;
        }

        bool shouldShowCount = w.Method("ShouldShowCount", trad).GetValue<bool>();
        if (shouldShowCount)
        {
            Rect countRect = new Rect(width - 75f, 0f, 75f, rect.height);
            Widgets.DrawHighlightIfMouseover(countRect);
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect labelRect = countRect;
            labelRect.xMin += 5f;
            labelRect.xMax -= 5f;
            Widgets.Label(labelRect, maxCount.ToStringCached());
            string sourceCountDesc = w.Field("sourceCountDesc").GetValue<string>();
            TooltipHandler.TipRegion(countRect, sourceCountDesc);
        }
        width -= 75f;

        if (widget.drawIdeo)
        {
            if (pawn != null && pawn.Ideo != null)
            {
                Rect r = new Rect(width - 30f, 0f, 30f, rect.height);
                Widgets.DrawHighlightIfMouseover(r);
                pawn.Ideo.DrawIcon(r);
                TooltipHandler.TipRegion(r, pawn.Ideo.name);
            }
            width -= 30f;
        }

        if (widget.drawXenotype && pawn != null && pawn.genes?.Xenotype != null)
        {
            Rect r = new Rect(width - 30f, 0f, 30f, rect.height);
            Widgets.DrawHighlightIfMouseover(r);
            GUI.color = XenotypeDef.IconColor;
            GUI.DrawTexture(r, pawn.genes.XenotypeIcon);
            GUI.color = Color.white;
            TooltipHandler.TipRegion(r, pawn.genes.XenotypeLabelCap);
        }

        if (pawn != null && pawn.IsSlave)
        {
            Rect r = new Rect(width - 30f, 0f, 30f, rect.height);
            Widgets.DrawHighlightIfMouseover(r);
            GUI.DrawTexture(r, pawn.guest.GetIcon());
            TooltipHandler.TipRegion(r, pawn.guest.GetLabel());
            width -= 30f;
        }

        if (w.Field("drawEquippedWeapon").GetValue<bool>())
        {
            Rect r = new Rect(width - 30f, 0f, 30f, rect.height);
            Rect iconRect = new Rect(width - 30f, (rect.height - 30f) / 2f, 30f, 30f);
            w.Method("DrawEquippedWeapon", r, iconRect, trad).GetValue();
            width -= 30f;
        }

        TransferableUIUtility.DoExtraIcons(trad, rect, ref width);

        Rect idRect = new Rect(0f, 0f, width, rect.height);
        Color labelColor = (pawn != null && pawn.IsSlave) ? PawnNameColorUtility.PawnNameColorOf(pawn) : Color.white;
        TransferableUIUtility.DrawTransferableInfo(trad, idRect, labelColor);

        GenUI.ResetLabelAlign();
        Widgets.EndGroup();
    }
}
