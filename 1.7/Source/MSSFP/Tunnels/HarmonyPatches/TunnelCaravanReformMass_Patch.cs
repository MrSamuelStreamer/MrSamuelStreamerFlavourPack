using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MSSFP.Tunnels.MapComponents;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.Tunnels.HarmonyPatches;

/// <summary>
/// Replaces the misleading mass bar in Dialog_FormCaravan when the player
/// reforms a caravan from a tunnel encounter map.
///
/// Tunnel caravans travel through underground passages and have no weight limit.
/// The standard reform dialog would show a capacity number and potentially a
/// red bar — both incorrect for this context.
///
/// Three patches cooperate:
///
///   DialogFormCaravan_TunnelReformTag_Patch (constructor Postfix)
///     Tags Dialog_FormCaravan instances opened during an active tunnel encounter
///     in a ConditionalWeakTable so dead dialog objects are collected automatically.
///     Guards on reform=true, HasActiveEncounter, and tile match to avoid affecting
///     unrelated caravan dialogs that happen to be open while an encounter is active.
///
///   DialogFormCaravan_TunnelMassCapacity_Patch (get_MassCapacity Postfix)
///     Returns float.MaxValue for tagged instances.  This ensures:
///       – GetMassColor always returns white (no red/orange bar).
///       – "remaining capacity" sliders in CreateCaravanTransferableWidgets have
///         an effectively unlimited budget (MassCapacity − MassUsage ≈ MaxValue).
///     Auto-supply is already disabled for reform dialogs, so the auto-select
///     code path that multiplies by MaxValue is never reached.
///
///   CaravanUIUtility_TunnelMassDisplay_Patch (DrawCaravanInfo Prefix)
///     When a tagged dialog is in the window stack, skips the original method
///     entirely and draws only a single "Mass: No weight limit" ExtraInfo entry
///     via TransferableUIUtility.DrawExtraInfo.  Because the encounter tile is
///     underground, isCaravan would already be false inside DrawCaravanInfo,
///     meaning vanilla would only show Mass and DaysWorthOfFood anyway — the
///     latter is irrelevant for the remaining tunnel leg and is omitted here.
/// </summary>

// ── Part 1: Tag tunnel reform dialog instances ──────────────────────────────

[HarmonyPatch]
public static class DialogFormCaravan_TunnelReformTag_Patch
{
    /// <summary>
    /// Presence in this table indicates the dialog instance is a tunnel reform.
    /// WeakReference semantics mean closed dialogs are reclaimed automatically.
    /// </summary>
    internal static readonly ConditionalWeakTable<Dialog_FormCaravan, object> Tagged
        = new ConditionalWeakTable<Dialog_FormCaravan, object>();

    private static readonly object _sentinel = new object();

    static MethodBase TargetMethod() =>
        AccessTools.Constructor(
            typeof(Dialog_FormCaravan),
            new[] { typeof(Map), typeof(bool), typeof(Action), typeof(bool), typeof(IntVec3?) });

    [HarmonyPostfix]
    static void TagTunnelReformDialog(Dialog_FormCaravan __instance, Map map, bool reform)
    {
        if (!TunnelUtilities.IsEnabled()) return;

        if (!reform) return;
        if (!TunnelEncounterSetup.HasActiveEncounter) return;
        if (map.Tile != TunnelEncounterSetup.ActiveEncounterTile) return;

        if (!Tagged.TryGetValue(__instance, out _))
            Tagged.Add(__instance, _sentinel);
    }
}

// ── Part 2: Return unlimited mass capacity for tagged instances ─────────────

[HarmonyPatch(typeof(Dialog_FormCaravan), "MassCapacity", MethodType.Getter)]
public static class DialogFormCaravan_TunnelMassCapacity_Patch
{
    [HarmonyPostfix]
    static void UnlimitedCapacity(Dialog_FormCaravan __instance, ref float __result)
    {
        if (!TunnelUtilities.IsEnabled()) return;

        if (DialogFormCaravan_TunnelReformTag_Patch.Tagged.TryGetValue(__instance, out _))
            __result = float.MaxValue;
    }
}

// ── Part 3: Replace the mass bar with a "No weight limit" label ─────────────

[HarmonyPatch(typeof(CaravanUIUtility), nameof(CaravanUIUtility.DrawCaravanInfo))]
public static class CaravanUIUtility_TunnelMassDisplay_Patch
{
    // Cached via lazy init — reflection lookup happens once.
    private static FieldInfo _tmpInfoField;
    private static FieldInfo TmpInfoField =>
        _tmpInfoField ??= AccessTools.Field(typeof(CaravanUIUtility), "tmpInfo");

    private static List<TransferableUIUtility.ExtraInfo> GetTmpInfo() =>
        TmpInfoField?.GetValue(null) as List<TransferableUIUtility.ExtraInfo>;

    [HarmonyPrefix]
    static bool ReplaceForTunnelReform(Rect rect)
    {
        if (!TunnelUtilities.IsEnabled()) return true;

        // Walk the window stack to find a tagged tunnel reform dialog.
        // This is O(n) over open windows (typically 1–2) — negligible cost.
        var windows = Find.WindowStack?.Windows;
        if (windows == null) return true;

        for (int i = 0; i < windows.Count; i++)
        {
            if (windows[i] is Dialog_FormCaravan dialog &&
                DialogFormCaravan_TunnelReformTag_Patch.Tagged.TryGetValue(dialog, out _))
            {
                return DrawTunnelReformInfo(rect);
            }
        }

        return true; // not a tunnel reform — run original DrawCaravanInfo
    }

    /// <summary>
    /// Draws a single "Mass: No weight limit" row in place of the standard
    /// mass bar and returns false to skip the original DrawCaravanInfo.
    /// </summary>
    private static bool DrawTunnelReformInfo(Rect rect)
    {
        var tmpInfo = GetTmpInfo();
        if (tmpInfo == null) return true; // reflection failure — fall back to vanilla

        tmpInfo.Clear();
        tmpInfo.Add(new TransferableUIUtility.ExtraInfo(
            "Mass".Translate(),
            "MSSFP_Tunnels_TunnelReform_NoWeightLimit".Translate(),
            Color.white,
            "MSSFP_Tunnels_TunnelReform_NoWeightLimitTip".Translate()));

        TransferableUIUtility.DrawExtraInfo(tmpInfo, rect);
        return false; // skip original
    }
}
