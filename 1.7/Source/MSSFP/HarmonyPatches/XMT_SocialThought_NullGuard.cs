using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Counter-patch for the Xenomorphtype (XMT) × Big&amp;Small sapient-animal incompatibility.
///
/// Cascade: MSSFP's <c>MSSFP_RavenCreepJoinerJoin</c> incident routes the spawned raven
/// through <c>BigAndSmall.RaceMorpher.SwapAnimalToSapientVersion</c>, producing a
/// humanlike-driven pawn that still lacks several humanlike trackers (story / relations /
/// xeno-specific trackers). XMT registers <c>XMT_ObsessionSocial</c> and
/// <c>XMT_TraumaSocial</c> as situational social thoughts — fired by
/// <see cref="SituationalThoughtHandler.TryCreateSocialThought"/> for every pawn-pair on
/// every social-thought recalc. XMT's <c>ThoughtWorker_Obsession.CurrentSocialStateInternal</c>
/// and <c>ThoughtWorker_TraumatizedBy.CurrentSocialStateInternal</c> do not null-guard one
/// of those missing trackers and throw <see cref="NullReferenceException"/> on the
/// sapient-converted raven (and other Big&amp;Small sapient animals). The vanilla
/// recalculator catches the throw and writes a stack trace to the log each time, producing
/// thousands of log writes per minute on a busy colony — confirmed in a player log where
/// the lines "preceding 4 lines repeated 95 times" appear repeatedly, cratering TPS.
///
/// Fix:
/// 1. <see cref="Prefix"/> — short-circuit when either subject is the MSSFP sapient raven
///    (carries the <c>MSSFP_RavenSavantAuraSource</c> marker hediff). Returns
///    <see cref="ThoughtState.Inactive"/> directly without invoking XMT. Targeted fast-path
///    that costs one hediff lookup per call for the known-bad case.
/// 2. <see cref="Finalizer"/> — belt-and-braces for the broader case where any
///    sapient-animal-converted pawn (other mods, other ravens-of-the-future) trips the same
///    XMT null path. Converts a thrown <see cref="NullReferenceException"/> into
///    <see cref="ThoughtState.Inactive"/> and clears the exception. Logs once via
///    <see cref="Log.WarningOnce(string,int)"/> so MSSFP doesn't itself become the source
///    of log spam.
///
/// Registered manually from <see cref="MSSFPMod"/> via <see cref="TryRegister"/> rather
/// than auto-discovered, so MSSFP does NOT take a compile-time reference on XMT. If XMT is
/// not loaded the registration silently no-ops. If XMT's internals drift (renamed type,
/// renamed method, signature change), <see cref="TryPatch"/> surfaces a single warning and
/// skips the patch — never breaks MSSFP load.
///
/// The correct semantic of "return Inactive" here: a sapient-animal-converted raven
/// cannot meaningfully be the subject of XMT's xeno-specific Obsession or Trauma social
/// states. Those thoughts are scoped to xenomorph-style pawns by their author. Returning
/// Inactive is equivalent to "this thought does not apply to this pawn type", which is
/// what XMT would return if it had the null-guard.
/// </summary>
public static class XMT_SocialThought_NullGuard
{
    private const string XmtObsessionTypeName = "Xenomorphtype.ThoughtWorker_Obsession";
    private const string XmtTraumaTypeName = "Xenomorphtype.ThoughtWorker_TraumatizedBy";
    private const string XmtMethodName = "CurrentSocialStateInternal";
    private const string RavenAuraSourceHediffName = "MSSFP_RavenSavantAuraSource";

    /// <summary>
    /// Cached resolution of the marker hediff that identifies the MSSFP sapient raven.
    /// Resolved lazily on first <see cref="IsMssfpSapientRaven"/> call — DefDatabase is not
    /// fully populated at Mod ctor time so eager resolution would no-op.
    /// </summary>
    private static HediffDef ravenAuraSourceDef;
    private static bool ravenAuraResolved;

    /// <summary>
    /// Install the prefix + finalizer on both XMT social ThoughtWorkers. No-ops when XMT is
    /// absent. Each target is patched independently — a resolve failure on one does not
    /// block the other.
    /// </summary>
    public static void TryRegister(Harmony harmony)
    {
        if (harmony == null) return;
        TryPatch(harmony, XmtObsessionTypeName);
        TryPatch(harmony, XmtTraumaTypeName);
    }

    private static void TryPatch(Harmony harmony, string typeName)
    {
        try
        {
            Type workerType = AccessTools.TypeByName(typeName);
            if (workerType == null)
            {
                // XMT not loaded, or this specific worker was removed by the author.
                // Silent — absent optional mod is not a fault condition.
                return;
            }

            MethodInfo target = AccessTools.Method(
                workerType,
                XmtMethodName,
                [typeof(Pawn), typeof(Pawn)]
            );
            if (target == null)
            {
                Log.Warning(
                    $"[MSSFP] XMT compat: could not resolve {typeName}.{XmtMethodName}(Pawn, Pawn) — "
                    + "XMT internals may have changed; sapient-animal NRE guard NOT installed for this type."
                );
                return;
            }

            MethodInfo prefix = AccessTools.Method(
                typeof(XMT_SocialThought_NullGuard),
                nameof(Prefix)
            );
            MethodInfo finalizer = AccessTools.Method(
                typeof(XMT_SocialThought_NullGuard),
                nameof(Finalizer)
            );

            harmony.Patch(
                target,
                prefix: new HarmonyMethod(prefix),
                finalizer: new HarmonyMethod(finalizer)
            );
            Log.Message(
                $"[MSSFP] XMT compat: installed sapient-animal NRE guard on {typeName}.{XmtMethodName}."
            );
        }
        catch (Exception e)
        {
            Log.Warning(
                $"[MSSFP] XMT compat: registration threw on {typeName}; skipping. Detail: {e}"
            );
        }
    }

    /// <summary>
    /// Short-circuit XMT social workers when the subject pawn is the MSSFP sapient raven.
    /// Cheap path: one hediff lookup, no XMT body executed. Returns false to skip original.
    /// </summary>
    public static bool Prefix(Pawn p, Pawn other, ref ThoughtState __result)
    {
        if (IsMssfpSapientRaven(p) || IsMssfpSapientRaven(other))
        {
            __result = ThoughtState.Inactive;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Convert any <see cref="NullReferenceException"/> escaping the XMT worker into
    /// <see cref="ThoughtState.Inactive"/> and clear the exception by returning null.
    /// Catches the broad case where any sapient-animal-converted pawn (not just MSSFP's
    /// raven) trips XMT's missing null guard. Non-NRE exceptions are re-thrown unchanged so
    /// we never mask real XMT bugs that aren't this specific cascade.
    /// </summary>
    public static Exception Finalizer(Exception __exception, ref ThoughtState __result)
    {
        if (__exception is NullReferenceException)
        {
            __result = ThoughtState.Inactive;
            Log.WarningOnce(
                "[MSSFP] XMT compat: swallowed NRE from XMT social ThoughtWorker on a sapient-converted "
                + "pawn (returning ThoughtState.Inactive). Root cause is in XMT's "
                + "CurrentSocialStateInternal which does not handle pawns lacking full humanlike trackers "
                + "(e.g. Big&Small sapient animals). This warning is logged once per session.",
                0x4D58544E
            );
            return null;
        }
        return __exception;
    }

    private static bool IsMssfpSapientRaven(Pawn p)
    {
        if (p?.health?.hediffSet == null) return false;
        if (!ravenAuraResolved)
        {
            ravenAuraSourceDef = DefDatabase<HediffDef>.GetNamedSilentFail(RavenAuraSourceHediffName);
            ravenAuraResolved = true;
        }
        return ravenAuraSourceDef != null && p.health.hediffSet.HasHediff(ravenAuraSourceDef);
    }
}
