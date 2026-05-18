using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Counter-patch for the HAR (Humanoid Alien Races) × NVA (No Vanilla Apparel) incompatibility.
///
/// Cascade: NVA calls <c>DefDatabase&lt;ThingDef&gt;.Remove(...)</c> on vanilla apparel defs but
/// those defs remain referenced as live object instances by scenario starts, faction loadouts,
/// race apparel rules, and any code holding the matching <see cref="ApparelProperties"/> by
/// reference. When vanilla pawn generation calls <see cref="ApparelProperties.PawnCanWear"/>
/// on one of these orphans, HAR's postfix routes through
/// <c>AlienRace.Utilities.CachedData.GetApparelFromApparelProps</c> which does
/// <c>DefDatabase&lt;ThingDef&gt;.AllDefsListForReading.First(td =&gt; td.apparel == props)</c>.
/// <see cref="System.Linq.Enumerable.First{TSource}(System.Collections.Generic.IEnumerable{TSource}, System.Func{TSource, bool})"/>
/// throws <see cref="InvalidOperationException"/> when no def matches — and the throw
/// propagates out of vanilla <c>PawnCanWear</c>, cascading through faction leader / starting
/// pawn / quest pawn generation.
///
/// Fix: replace <c>First</c> with <c>FirstOrDefault</c> and cache the null result in HAR's
/// own dict. <c>RaceRestrictionSettings.CanWear(null, race)</c> is null-safe and returns
/// <c>true</c>, so the postfix expression <c>__result &amp;= CanWear(null, ...)</c> becomes a
/// no-op — semantically equivalent to "orphan apparel has no race restriction", which is the
/// correct interpretation: a def that no longer exists in the DefDatabase cannot belong to
/// any race rule anyway.
///
/// Registered manually from <see cref="MSSFPMod"/> via <see cref="TryRegister"/> rather than
/// auto-discovered, so MSSFP does NOT take a compile-time reference on HAR. If HAR is not
/// loaded, the registration silently no-ops. If HAR's internals drift (renamed type/field),
/// the try/catch surfaces a single warning and skips the patch — never breaks MSSFP load.
/// </summary>
public static class HAR_GetApparelFromApparelProps_NullGuard
{
    // The installed HAR assembly exposes CachedData at the namespace root — NOT nested
    // inside Utilities as the upstream GitHub source suggests. Confirmed via runtime
    // reflection: AccessTools.TypeByName("AlienRace.Utilities+CachedData") => null,
    // AccessTools.TypeByName("AlienRace.CachedData") => valid. Either the public release
    // hoists the nested class or the upstream source was refactored after release. Use
    // the runtime-correct fully-qualified name.
    private const string HarCachedDataTypeName = "AlienRace.CachedData";
    private const string HarMethodName = "GetApparelFromApparelProps";
    private const string HarDictFieldName = "apparelPropsToApparelDict";

    /// <summary>
    /// Captured reflective handle to <c>AlienRace.Utilities.CachedData.apparelPropsToApparelDict</c>.
    /// Resolved once at <see cref="TryRegister"/> time and read on every prefix invocation —
    /// re-reflecting per call would add measurable overhead to <c>PawnCanWear</c>, which is
    /// hot during pawn generation.
    /// </summary>
    private static FieldInfo dictField;

    /// <summary>
    /// Attempt to install the prefix on HAR's <c>GetApparelFromApparelProps</c>. No-ops if
    /// HAR is not loaded. Logs a single warning and swallows any reflection error so HAR
    /// version drift cannot prevent MSSFP from finishing its Mod ctor.
    /// </summary>
    public static void TryRegister(Harmony harmony)
    {
        if (harmony == null) return;
        try
        {
            Type cachedDataType = AccessTools.TypeByName(HarCachedDataTypeName);
            if (cachedDataType == null)
            {
                // HAR not loaded — nothing to patch. Silent.
                return;
            }

            MethodInfo target = AccessTools.Method(cachedDataType, HarMethodName);
            if (target == null)
            {
                Log.Warning(
                    $"[MSSFP] HAR compat: could not resolve {HarCachedDataTypeName}.{HarMethodName} — "
                    + "HAR internals may have changed. NVA × HAR null-guard NOT installed."
                );
                return;
            }

            dictField = AccessTools.Field(cachedDataType, HarDictFieldName);
            if (dictField == null)
            {
                Log.Warning(
                    $"[MSSFP] HAR compat: could not resolve field {HarDictFieldName} on "
                    + $"{HarCachedDataTypeName} — NVA × HAR null-guard NOT installed."
                );
                return;
            }

            MethodInfo prefix = AccessTools.Method(
                typeof(HAR_GetApparelFromApparelProps_NullGuard),
                nameof(Prefix)
            );
            harmony.Patch(target, prefix: new HarmonyMethod(prefix));
            Log.Message(
                "[MSSFP] HAR compat: installed NVA × HAR null-guard on "
                + $"{HarCachedDataTypeName}.{HarMethodName}."
            );
        }
        catch (Exception e)
        {
            Log.Warning(
                "[MSSFP] HAR compat: NVA × HAR null-guard registration threw; skipping. "
                + $"Detail: {e}"
            );
        }
    }

    /// <summary>
    /// Harmony prefix replacing the original <c>GetApparelFromApparelProps</c> body. Returns
    /// <c>false</c> to skip the original (which would throw on orphan props).
    /// </summary>
    public static bool Prefix(ApparelProperties props, ref ThingDef __result)
    {
        if (dictField == null)
        {
            // Defensive: should be unreachable — TryRegister bails before patching when the
            // field can't be resolved. Falling back to the original keeps behaviour identical
            // to an unpatched HAR install rather than silently returning null.
            return true;
        }

        Dictionary<ApparelProperties, ThingDef> dict;
        try
        {
            dict = (Dictionary<ApparelProperties, ThingDef>)dictField.GetValue(null);
        }
        catch (Exception e)
        {
            Log.WarningOnce(
                $"[MSSFP] HAR compat: dict field read threw, falling through to original: {e}",
                0x4D5353F0
            );
            return true;
        }
        if (dict == null) return true;

        // Cached path — including the null-cache case. Caching null prevents repeated O(n)
        // DefDatabase scans for the same orphan props on every subsequent PawnCanWear call,
        // which is critical during faction leader generation where the same loadout cycles
        // through many pawns.
        if (dict.TryGetValue(props, out ThingDef cached))
        {
            __result = cached;
            return false;
        }

        ThingDef found = DefDatabase<ThingDef>.AllDefsListForReading
            .FirstOrDefault(td => td.apparel == props);
        dict[props] = found;
        __result = found;
        return false;
    }
}
