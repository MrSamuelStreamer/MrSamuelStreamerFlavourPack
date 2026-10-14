using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Counter-patch for Expanded Incidents (Continued) (Mlie.ExpandedIncidents) crashing the
/// tick loop.
///
/// Confirmed from a player CTD log (2026-09-12): every tick of
/// <see cref="RimWorld.Pawn_InteractionsTracker.TryInteractWith"/> for certain pawns threw
/// through <c>ExpandedIncidents.Harmony.InteractionWorker_Interacted.Prefix(Pawn, Pawn)</c> —
/// a <see cref="NullReferenceException"/> inside its own clique-lookup lambda
/// (<c>Alert_CliqueMembers+&lt;&gt;c.&lt;get_CliqueLeaders&gt;b__3_0</c>), most likely a stale
/// pawn reference left in a clique/faction list on this save. Vanilla's own per-thing
/// try/catch in <see cref="Verse.TickList.Tick"/> keeps the game alive per exception, but the
/// same pawns re-throw it every tick forever — hundreds of full stack traces logged per
/// second, which is what actually took the process down (log-write volume, not the exception
/// itself). The same root cause also throws from <c>Alert_CliqueMembers.GetReport()</c> on
/// every alert recalculation, already caught by <see cref="RimWorld.AlertsReadout"/> but
/// equally noisy.
///
/// Fix: attach a <see cref="HarmonyFinalizer"/> directly to each of EI's own broken methods.
/// A finalizer patch can target ANY method, including one that is itself installed as
/// someone else's Harmony patch — Harmony has no concept of "this MethodInfo is already a
/// patch", it is just IL to rewrite. Swallowing the NRE there converts "crash every tick"
/// into "this mod's clique feature silently no-ops for the affected pawn", which is the
/// correct outcome: MSSFP does not own Expanded Incidents' clique data and cannot repair it.
///
/// Resolved entirely by reflection — MSSFP does not reference ExpandedIncidents.dll.
/// Registered manually from <see cref="MSSFPMod"/> via <see cref="TryRegister"/>. Absent mod,
/// renamed type/method, or a Harmony failure all just skip silently (one warning), same
/// contract as the other cross-mod NullGuard patches in this folder.
/// </summary>
public static class ExpandedIncidents_NullGuard
{
    private const string InteractedPatchTypeName = "ExpandedIncidents.Harmony.InteractionWorker_Interacted";
    private const string InteractedPatchMethodName = "Prefix";

    private const string CliqueAlertTypeName = "ExpandedIncidents.Alert_CliqueMembers";
    private const string CliqueAlertMethodName = "GetReport";

    public static void TryRegister(Harmony harmony)
    {
        if (harmony == null) return;
        TryRegisterInteractedGuard(harmony);
        TryRegisterCliqueAlertGuard(harmony);
    }

    /// <summary>
    /// Guards the tick-loop crash: EI's own Prefix on TryInteractWith. On NRE, sets
    /// <c>__result = true</c> so the interaction proceeds as if EI's patch were absent,
    /// rather than leaving the throw to unwind through the tick loop every tick.
    /// </summary>
    private static void TryRegisterInteractedGuard(Harmony harmony)
    {
        try
        {
            Type patchType = AccessTools.TypeByName(InteractedPatchTypeName);
            if (patchType == null) return; // Expanded Incidents not loaded.

            MethodInfo target = AccessTools.Method(
                patchType,
                InteractedPatchMethodName,
                [typeof(Pawn), typeof(Pawn)]
            );
            if (target == null)
            {
                Log.Warning(
                    $"[MSSFP] ExpandedIncidents compat: could not resolve {InteractedPatchTypeName}.{InteractedPatchMethodName}(Pawn, Pawn) — "
                    + "EI internals may have changed; tick-loop NRE guard NOT installed."
                );
                return;
            }

            MethodInfo finalizer = AccessTools.Method(
                typeof(ExpandedIncidents_NullGuard),
                nameof(InteractedFinalizer)
            );

            harmony.Patch(target, finalizer: new HarmonyMethod(finalizer));
            Log.Message(
                $"[MSSFP] ExpandedIncidents compat: installed tick-loop NRE guard on {InteractedPatchTypeName}.{InteractedPatchMethodName}."
            );
        }
        catch (Exception e)
        {
            Log.Warning($"[MSSFP] ExpandedIncidents compat: registration threw; skipping. Detail: {e}");
        }
    }

    /// <summary>
    /// Guards the same root cause surfacing through the clique alert's GetReport. Lower
    /// stakes than the tick-loop guard (vanilla already catches this one), added for
    /// completeness since it shares the same broken clique lookup.
    /// </summary>
    private static void TryRegisterCliqueAlertGuard(Harmony harmony)
    {
        try
        {
            Type alertType = AccessTools.TypeByName(CliqueAlertTypeName);
            if (alertType == null) return;

            MethodInfo target = AccessTools.Method(alertType, CliqueAlertMethodName, Type.EmptyTypes);
            if (target == null)
            {
                Log.Warning(
                    $"[MSSFP] ExpandedIncidents compat: could not resolve {CliqueAlertTypeName}.{CliqueAlertMethodName}() — "
                    + "EI internals may have changed; alert NRE guard NOT installed."
                );
                return;
            }

            MethodInfo finalizer = AccessTools.Method(
                typeof(ExpandedIncidents_NullGuard),
                nameof(CliqueAlertFinalizer)
            );

            harmony.Patch(target, finalizer: new HarmonyMethod(finalizer));
            Log.Message(
                $"[MSSFP] ExpandedIncidents compat: installed alert NRE guard on {CliqueAlertTypeName}.{CliqueAlertMethodName}."
            );
        }
        catch (Exception e)
        {
            Log.Warning($"[MSSFP] ExpandedIncidents compat: registration threw; skipping. Detail: {e}");
        }
    }

    public static Exception InteractedFinalizer(ref bool __result, Exception __exception)
    {
        if (__exception is NullReferenceException)
        {
            Log.WarningOnce(
                "[MSSFP] ExpandedIncidents compat: swallowed NRE from InteractionWorker_Interacted.Prefix "
                + "(broken clique lookup — see Alert_CliqueMembers). Interaction proceeds as if EI's patch "
                + "were absent. Logged once per session.",
                0x45494E47 // "EING" — Expanded Incidents Null Guard
            );
            __result = true;
            return null;
        }
        return __exception;
    }

    public static Exception CliqueAlertFinalizer(ref AlertReport __result, Exception __exception)
    {
        if (__exception is NullReferenceException)
        {
            Log.WarningOnce(
                "[MSSFP] ExpandedIncidents compat: swallowed NRE from Alert_CliqueMembers.GetReport "
                + "(broken clique lookup). Alert reports inactive this cycle. Logged once per session.",
                0x45494E48 // "EINH"
            );
            __result = AlertReport.Inactive;
            return null;
        }
        return __exception;
    }
}
