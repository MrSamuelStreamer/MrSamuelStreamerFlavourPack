using System;
using System.Text;
using System.Text.RegularExpressions;
using MSSFP.AICore;
using MSSFP.Defs;
using RimWorld;
using Verse;

namespace MSSFP.Holo;

/// <summary>
/// Helpers for holo-projection lifecycle. The CompHoloProjected back-ref comp is registered
/// on <c>MSSFP_HoloPawnRace</c> at def-load (XML); these helpers just populate its fields and
/// attach the runtime hediff at projection time.
///
/// HEDIFF ADD POLICY (P0 finding #5): hediff additions happen ONLY at projection time,
/// never inside PostExposeData. Restored saves rely on the hediff already being on the
/// pawn (deep-scribed by world-pawn authority).
///
/// DESTROY POLICY: holo pawns intercept Pawn.Kill / Pawn.Destroy via
/// <see cref="MSSFP.HarmonyPatches.Pawn_Kill_HoloIntercept_Patch"/> and route to collapse
/// instead of dying. To actually destroy a holo pawn (projector cleanup, save shutdown),
/// ALWAYS use <see cref="DestroyHoloForReal"/> — never call <c>pawn.Destroy()</c> directly.
/// </summary>
public static class MSSFPHoloUtil
{
    [ThreadStatic] private static int holoDestroyBypass;

    /// <summary>
    /// True while a <see cref="DestroyHoloForReal"/> call is in flight on this thread.
    /// Death-intercept patches consult this to decide whether to forward to vanilla.
    /// </summary>
    public static bool IsHoloDestroyBypassed => holoDestroyBypass > 0;

    /// <summary>
    /// Funnel for ALL legitimate holo-pawn destruction. Increments the thread-local bypass
    /// guard so Pawn.Kill/Destroy intercept patches allow this call through to vanilla.
    /// Use this anywhere MSSFP code needs to actually destroy a stored holo pawn.
    /// </summary>
    public static void DestroyHoloForReal(Pawn p, DestroyMode mode = DestroyMode.Vanish)
    {
        if (p == null || p.Destroyed) return;
        holoDestroyBypass++;
        try
        {
            p.Destroy(mode);
        }
        finally
        {
            holoDestroyBypass--;
        }
    }

    /// <summary>
    /// True iff the pawn is an active projection (has <see cref="CompHoloProjected"/> with a
    /// live <c>sourceProjector</c>). Null-safe. Used by all P1-B gating patches.
    /// </summary>
    public static bool IsHolo(Pawn pawn)
    {
        if (pawn == null)
            return false;
        CompHoloProjected comp = pawn.TryGetComp<CompHoloProjected>();
        return comp?.sourceProjector != null;
    }

    /// <summary>Set the back-ref on the existing CompHoloProjected (XML-registered).</summary>
    public static void AddHoloComp(Pawn pawn, Thing source)
    {
        if (pawn == null)
            return;
        CompHoloProjected comp = pawn.TryGetComp<CompHoloProjected>();
        if (comp == null)
        {
            Log.Error($"[MSSFP] AddHoloComp: pawn {pawn} has no CompHoloProjected. Check race def registration.");
            return;
        }
        comp.sourceProjector = source;
    }

    /// <summary>Attach the hologram hediff at full severity (P0: PostSpawnSetup-shaped path).</summary>
    public static void AddOrRefreshHologramHediff(Pawn pawn)
    {
        if (pawn?.health?.hediffSet == null)
            return;
        HediffDef def = MSSFPDefOf.MSSFP_Hediff_Hologram;
        if (def == null)
            return;

        Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(def);
        if (existing != null)
            return;

        Hediff h = HediffMaker.MakeHediff(def, pawn);
        h.Severity = 1f;
        pawn.health.AddHediff(h);
    }

    // ----- Persona-wiring helpers (Concerns 2 + 5 + 7) -----

    private const int PersonaLabelMaxLen = 24;
    private const int ChatterLineMaxLen = 120;
    private static readonly Regex TagRx = new Regex("<[^>]+>", RegexOptions.Compiled);

    /// <summary>
    /// Sanitize a free-form persona label for use as a NameTriple slot. Strips XML-unsafe
    /// chars and control codes, caps length, falls back to "Holo" on empty/whitespace.
    /// </summary>
    public static string SanitizePersonaLabel(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "Holo";
        StringBuilder sb = new StringBuilder(raw.Length);
        foreach (char c in raw)
        {
            if (char.IsControl(c))
                continue;
            if (c == '<' || c == '>' || c == '&')
                continue;
            sb.Append(c);
        }
        string s = sb.ToString().Trim();
        if (s.Length > PersonaLabelMaxLen)
            s = s.Substring(0, PersonaLabelMaxLen);
        return string.IsNullOrWhiteSpace(s) ? "Holo" : s;
    }

    /// <summary>
    /// Strip rich-text tags and cap length on persona chatter output before display.
    /// Defends against hostile RulePack injection of <c>&lt;color&gt;</c>, <c>&lt;size&gt;</c>, etc.
    /// </summary>
    public static string StripRichText(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return raw;
        string s = TagRx.Replace(raw, string.Empty);
        if (s.Length > ChatterLineMaxLen)
            s = s.Substring(0, ChatterLineMaxLen);
        return s;
    }

    /// <summary>
    /// Validate + lazily resolve the worker for a persona def. Returns null on bad type or
    /// activation failure. Logs once per def to avoid log spam from broken third-party packs.
    /// </summary>
    public static AIPersonalityWorker ResolveWorker(AIPersonalityDef def)
    {
        if (def == null || def.workerClass == null)
            return null;
        if (!typeof(AIPersonalityWorker).IsAssignableFrom(def.workerClass))
        {
            Log.ErrorOnce(
                $"[MSSFP] Persona {def.defName} has invalid workerClass {def.workerClass} (must derive from AIPersonalityWorker)",
                def.GetHashCode() ^ 0x484F4C4F
            );
            return null;
        }
        try
        {
            return def.Worker;
        }
        catch (Exception ex)
        {
            Log.ErrorOnce(
                $"[MSSFP] Persona {def.defName} worker ctor threw: {ex.Message}",
                def.GetHashCode() ^ 0x57524B45
            );
            return null;
        }
    }

    /// <summary>
    /// Notify all CompHoloProjector instances on a cell that the local projector cluster
    /// has changed (spawn / despawn). Each rebuilds its sibling cache.
    /// </summary>
    public static void NotifyProjectorChanged(Map map, IntVec3 cell)
    {
        if (map == null || !cell.InBounds(map))
            return;
        foreach (Thing t in cell.GetThingList(map))
        {
            t.TryGetComp<CompHoloProjector>()?.RebuildSiblingCacheExternal();
        }
    }
}
