using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Counter-patch for the Intimacy - Socio Butterfly (LovelyDovey.Recreation.WithEuterpe)
/// × Big&amp;Small sapient-animal incompatibility.
///
/// Cascade: Socio Butterfly Harmony-postfixes <see cref="RimWorld.JobGiver_GetFood.GetPriority"/>
/// via <c>RecreationalSexWithEuterpe.JobGiver_GetFood_GetPriority_Patch.Postfix</c>. The
/// postfix gates on <c>pawn.RaceProps.Humanlike &amp;&amp; pawn.IsColonistPlayerControlled</c>
/// and then derefs <c>Comp_SocioButterflyTracker</c> obtained via
/// <see cref="ThingCompUtility.TryGetComp{T}(ThingWithComps)"/> without null-checking. The
/// mod only injects the comp into vanilla <c>Defs/ThingDef[defName="Human"]</c> (see its
/// <c>Human_patches.xml</c>) so any non-Human humanlike — Big&amp;Small sapient animals
/// (including MSSFP's sapient raven creepjoiner), HAR races, Androids, custom xenos with
/// their own ThingDef, etc. — passes the guard but has no comp, throwing
/// <see cref="NullReferenceException"/> on every
/// <see cref="Verse.AI.ThinkNode_PrioritySorter"/> evaluation of food priority. Vanilla
/// catches the throw and writes a stack trace to the log each time, producing thousands of
/// log writes per minute on any save with a sapient-converted pawn — confirmed in a player
/// log where the Ref FCEA1FEA dedup marker repeats continuously.
///
/// Fix:
/// 1. <see cref="Prefix"/> — when the pawn has no SocioButterfly tracker comp, short-circuit
///    the postfix entirely. Saves the per-tick NRE throw + stack walk cost on every modded
///    humanlike pawn. Returns false (skip original). The comp type is resolved by reflection
///    so MSSFP does not take a compile-time reference on SocioButterfly's assembly.
/// 2. <see cref="Finalizer"/> — belt-and-braces. Converts a thrown
///    <see cref="NullReferenceException"/> into a no-op leaving <c>__result</c> at its
///    pre-postfix priority value. Logs once via <see cref="Log.WarningOnce(string,int)"/>
///    so MSSFP doesn't itself become the source of log spam.
///
/// Registered manually from <see cref="MSSFPMod"/> via <see cref="TryRegister"/>. If
/// SocioButterfly isn't loaded, registration silently no-ops. If its internals drift
/// (renamed type, signature change), <see cref="TryRegister"/> surfaces a single warning and
/// skips the patch — never breaks MSSFP load.
///
/// Correct semantic of "no comp == skip": SocioButterfly's postfix is purely a per-pawn
/// meal-timetable preference layer. A pawn without the tracker has no meal timetable, so
/// the postfix would not change priority even if the comp existed — skipping is
/// observationally identical to running with a fresh default-state comp.
/// </summary>
public static class SocioButterfly_GetFood_NullGuard
{
    private const string PatchTypeName =
        "RecreationalSexWithEuterpe.JobGiver_GetFood_GetPriority_Patch";
    private const string PatchMethodName = "Postfix";
    private const string CompTypeName =
        "RecreationalSexWithEuterpe.Comp_SocioButterflyTracker";

    /// <summary>
    /// Cached resolution of <c>Comp_SocioButterflyTracker</c>. Resolved lazily on first
    /// <see cref="Prefix"/> call so SocioButterfly's assembly is fully loaded.
    /// </summary>
    private static Type socioCompType;
    private static bool socioCompResolved;

    /// <summary>
    /// Install the prefix + finalizer on SocioButterfly's GetFood postfix. Silent no-op
    /// when SocioButterfly is absent.
    /// </summary>
    public static void TryRegister(Harmony harmony)
    {
        if (harmony == null) return;
        try
        {
            Type patchType = AccessTools.TypeByName(PatchTypeName);
            if (patchType == null)
            {
                // SocioButterfly not loaded — absent optional mod is not a fault condition.
                return;
            }

            MethodInfo target = AccessTools.Method(
                patchType,
                PatchMethodName,
                [typeof(Pawn), typeof(float).MakeByRefType()]
            );
            if (target == null)
            {
                Log.Warning(
                    $"[MSSFP] SocioButterfly compat: could not resolve {PatchTypeName}.{PatchMethodName}(Pawn, ref float) — "
                    + "SocioButterfly internals may have changed; missing-comp NRE guard NOT installed."
                );
                return;
            }

            MethodInfo prefix = AccessTools.Method(
                typeof(SocioButterfly_GetFood_NullGuard),
                nameof(Prefix)
            );
            MethodInfo finalizer = AccessTools.Method(
                typeof(SocioButterfly_GetFood_NullGuard),
                nameof(Finalizer)
            );

            harmony.Patch(
                target,
                prefix: new HarmonyMethod(prefix),
                finalizer: new HarmonyMethod(finalizer)
            );
            Log.Message(
                $"[MSSFP] SocioButterfly compat: installed missing-comp NRE guard on {PatchTypeName}.{PatchMethodName}."
            );
        }
        catch (Exception e)
        {
            Log.Warning(
                $"[MSSFP] SocioButterfly compat: registration threw; skipping. Detail: {e}"
            );
        }
    }

    /// <summary>
    /// Skip SocioButterfly's postfix when the pawn has no
    /// <c>Comp_SocioButterflyTracker</c>. Returning false skips the original —
    /// semantically identical to running it with a fresh default-state comp, because the
    /// postfix's only mutation requires non-default <c>breakfastHour</c> or
    /// <c>dinnerHour</c> values.
    /// </summary>
    public static bool Prefix(Pawn pawn)
    {
        if (pawn == null) return true; // postfix's own null guard handles
        Type compType = ResolveCompType();
        if (compType == null) return true; // can't resolve — let original run
        var comps = pawn.AllComps;
        if (comps == null) return false;
        for (int i = 0; i < comps.Count; i++)
        {
            if (compType.IsInstanceOfType(comps[i])) return true; // has comp — run original
        }
        return false; // no comp — skip
    }

    /// <summary>
    /// Convert any <see cref="NullReferenceException"/> escaping the SocioButterfly postfix
    /// into a no-op. Leaves <c>__result</c> at whatever vanilla
    /// <see cref="RimWorld.JobGiver_GetFood.GetPriority"/> already produced. Logs once.
    /// Non-NRE exceptions are re-thrown unchanged so we never mask real SocioButterfly bugs
    /// that aren't this specific cascade.
    /// </summary>
    public static Exception Finalizer(Exception __exception)
    {
        if (__exception is NullReferenceException)
        {
            Log.WarningOnce(
                "[MSSFP] SocioButterfly compat: swallowed NRE from Intimacy - Socio Butterfly "
                + "JobGiver_GetFood postfix on a humanlike pawn lacking Comp_SocioButterflyTracker "
                + "(e.g. Big&Small sapient animals, HAR races, Androids). Root cause is in "
                + "RecreationalSexWithEuterpe.JobGiver_GetFood_GetPriority_Patch.Postfix which "
                + "calls TryGetComp without null-checking the result. This warning is logged once "
                + "per session.",
                0x53424E47 // "SBNG" — SocioButterfly Null Guard
            );
            return null;
        }
        return __exception;
    }

    private static Type ResolveCompType()
    {
        if (!socioCompResolved)
        {
            socioCompType = AccessTools.TypeByName(CompTypeName);
            socioCompResolved = true;
        }
        return socioCompType;
    }
}
