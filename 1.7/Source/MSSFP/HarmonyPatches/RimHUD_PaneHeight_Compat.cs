using System;
using System.Reflection;
using HarmonyLib;
using MSSFP.Fonts;
using MSSFP.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Keeps RimHUD's docked inspect pane readable when the UI font has taller lines than vanilla.
///
/// RimHUD gives its docked pane a fixed height (its InspectPaneHeight setting, 232 px by default)
/// and sizes every row from the font's line height. A taller font, such as Cadman, makes the rows
/// no longer fit, so lines overlap. This compat grows the pane by the font's line-height ratio
/// (<see cref="UiFont.LineHeightScale"/>) in the three places RimHUD applies its height:
/// <c>InspectPaneUtility.PaneSizeFor</c>, <c>MainTabWindow_Inspect.PaneTopY</c> and the
/// <c>RecentHeight</c> it sets in <c>InspectPanePlus.DrawPane</c>. RimHUD's own setting is never
/// touched, so nothing is saved or exported with the scaled value.
///
/// Each postfix runs only while RimHUD is modifying the pane (<c>State.ModifyPane</c>), and does
/// nothing at scale 1 (vanilla font, or one with lines no taller than vanilla).
///
/// Registered manually from <see cref="MSSFPMod"/> via <see cref="TryRegister"/>. If RimHUD is not
/// loaded, registration silently no-ops. If its internals drift (renamed type or method),
/// <see cref="TryRegister"/> logs one warning and skips the patch, never breaking MSSFP load.
/// </summary>
public static class RimHUD_PaneHeight_Compat
{
    private const string StateTypeName = "RimHUD.Engine.State";
    private const string PanePlusTypeName = "RimHUD.Interface.Screen.InspectPanePlus";

    // RimHUD's docked pane keeps this much clear below it (see its PaneTopY patch).
    private const float PaneBottomMargin = 35f;

    private static Func<bool> modifyPane;

    public static void TryRegister(Harmony harmony)
    {
        if (harmony == null)
            return;
        try
        {
            Type stateType = AccessTools.TypeByName(StateTypeName);
            Type panePlusType = AccessTools.TypeByName(PanePlusTypeName);
            if (stateType == null && panePlusType == null)
                return; // RimHUD is not loaded: an absent optional mod is not a fault.

            MethodInfo modifyPaneGetter = stateType == null ? null : AccessTools.PropertyGetter(stateType, "ModifyPane");
            MethodInfo drawPane = panePlusType == null
                ? null
                : AccessTools.Method(panePlusType, "DrawPane", [typeof(Rect), typeof(IInspectPane)]);
            MethodInfo paneSizeFor = AccessTools.Method(typeof(InspectPaneUtility), nameof(InspectPaneUtility.PaneSizeFor));
            MethodInfo paneTopY = AccessTools.PropertyGetter(typeof(MainTabWindow_Inspect), nameof(MainTabWindow_Inspect.PaneTopY));
            if (modifyPaneGetter == null || drawPane == null || paneSizeFor == null || paneTopY == null)
            {
                ModLog.Warn(
                    "[MSSFP] RimHUD compat: could not resolve RimHUD's pane internals "
                        + "(State.ModifyPane / InspectPanePlus.DrawPane) or the vanilla pane methods; "
                        + "RimHUD may have changed. Pane height is NOT adjusted for tall UI fonts."
                );
                return;
            }

            modifyPane = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), modifyPaneGetter);

            // Last, so each postfix sees the height RimHUD's own patches have already set.
            harmony.Patch(paneSizeFor, postfix: Postfix(nameof(PaneSizeForPostfix)));
            harmony.Patch(paneTopY, postfix: Postfix(nameof(PaneTopYPostfix)));
            harmony.Patch(drawPane, postfix: Postfix(nameof(DrawPanePostfix)));
            ModLog.Log("[MSSFP] RimHUD compat: docked pane height follows the UI font's line height.");
        }
        catch (Exception e)
        {
            ModLog.Warn($"[MSSFP] RimHUD compat: registration threw; skipping. Detail: {e}");
        }
    }

    private static HarmonyMethod Postfix(string name) =>
        new(typeof(RimHUD_PaneHeight_Compat), name) { priority = Priority.Last };

    /// <summary>The factor to grow the pane by, or 1 when RimHUD is not modifying the pane.</summary>
    private static float ActiveScale() => modifyPane != null && modifyPane() ? UiFont.LineHeightScale : 1f;

    public static void PaneSizeForPostfix(ref Vector2 __result)
    {
        float scale = ActiveScale();
        if (scale > 1f)
            __result.y *= scale;
    }

    public static void PaneTopYPostfix(ref float __result)
    {
        float scale = ActiveScale();
        if (scale <= 1f)
            return;
        float paneHeight = UI.screenHeight - PaneBottomMargin - __result;
        __result = UI.screenHeight - PaneBottomMargin - paneHeight * scale;
    }

    public static void DrawPanePostfix(IInspectPane pane)
    {
        float scale = ActiveScale();
        if (scale > 1f)
            pane.RecentHeight *= scale;
    }
}
