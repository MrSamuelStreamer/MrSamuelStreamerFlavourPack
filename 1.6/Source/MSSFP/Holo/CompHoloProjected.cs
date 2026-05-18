using System;
using MSSFP.AICore;
using MSSFP.Comps;
using MSSFP.Defs;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Holo;

/// <summary>
/// Marker comp on the projected pawn carrying a back-reference to the source projector building.
/// Render/persona patches read this comp to resolve tint + persona without scanning maps.
///
/// LIFETIME: attached at projection time via <see cref="MSSFPHoloUtil.AddHoloComp"/>. Never
/// added through XML on a race def because the holo pawn race is built from vanilla Human;
/// pawn-level dynamic comp addition is required.
/// </summary>
public class CompHoloProjected : ThingComp
{
    /// <summary>Source projector building. Scribed by reference.</summary>
    public Thing sourceProjector;

    /// <summary>
    /// Lookup helper — resolves the holo-projector comp on the source building, or null if
    /// the building has been destroyed mid-frame.
    /// </summary>
    public CompHoloProjector ProjectorComp => sourceProjector?.TryGetComp<CompHoloProjector>();

    /// <summary>Tint mirrored from the projector. White if projector gone.</summary>
    public Color Tint => ProjectorComp?.Tint ?? Color.white;

    /// <summary>Persona ref (read from the projector's AI core sibling comp). Nullable.</summary>
    public AIPersonalityDef Persona => ProjectorComp?.Persona;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref sourceProjector, "sourceProjector");
    }

    /// <summary>
    /// Holo absorbs ALL damage (illusory body). Triggers projection collapse when incoming
    /// damage would down or kill the pawn (>=50% of current summed HP).
    /// </summary>
    /// <remarks>
    /// Vanilla parity: <c>CompShield.PostPreApplyDamage</c> sets <c>absorbed=true</c> on absorb.
    /// We always absorb when a projector back-ref is present; collapse routing is a side effect.
    /// </remarks>
    public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
    {
        absorbed = false;
        if (sourceProjector == null)
            return;
        absorbed = true;
        if (dinfo.Amount <= 0f)
            return;
        Pawn p = parent as Pawn;
        if (p == null)
            return;
        float curHP = p.health.summaryHealth.SummaryHealthPercent * p.MaxHitPoints;
        if (dinfo.Amount >= curHP * 0.5f)
            ProjectorComp?.OnProjectionCollapsed(dinfo);
    }

    /// <summary>
    /// Rare-tick (~250t / ~4s). Persona chatter + cross-map sanity. Area-leash gating
    /// was removed — holo is map-bound only (same map as projector); vanilla AI picks
    /// jobs anywhere on that map. Power-loss recall + projector despawn still recall.
    /// </summary>
    public override void CompTickRare()
    {
        base.CompTickRare();

        Pawn p = parent as Pawn;
        if (p == null || !p.Spawned)
            return;

        CompHoloProjector proj = ProjectorComp;
        if (proj == null)
            return;
        Thing projThing = proj.parent;
        if (projThing?.Map == null || p.Map != projThing.Map)
            return;

        // Persona chatter — runs regardless of drafted state.
        TryRollPersonaChatter(p, proj);
        TryRollPersonaAddress(p, proj);
    }

    /// <summary>
    /// Roll for a persona ambient chatter line and throw it as a Mote over the holo.
    /// Snapshots refs at entry to dodge mid-tick despawn races. Filters grammar-resolver
    /// failure literals and strips rich-text injection from the result.
    /// </summary>
    private static void TryRollPersonaChatter(Pawn p, CompHoloProjector proj)
    {
        // Snapshot — re-reads after this point could see stale state if a recall fires mid-method.
        Map m = proj.parent?.Map;
        AIPersonalityDef def = proj.Persona;
        if (m == null || def == null) return;
        if (!p.Spawned || p.Map != m) return;

        // Sibling suppression: only one same-cell projector emits chatter per tick-rare.
        // Self is allowed if our projection IS the live one (HasLiveActiveSibling looks for OTHER live siblings).
        if (proj.HasLiveActiveSibling() && !ReferenceEquals(proj.projected, p))
            return;

        // MTB: ~1 line / 2 in-game hours per holo. 250t = CompTickRare delta.
        if (!Rand.MTBEventOccurs(2f, 60000f, 250f))
            return;

        CompTrueAICore core = proj.parent.TryGetComp<CompTrueAICore>();
        if (core == null) return;

        AIPersonalityWorker worker = MSSFPHoloUtil.ResolveWorker(def);
        if (worker == null) return;

        string line;
        try
        {
            line = worker.RollChatter(core);
        }
        catch (Exception ex)
        {
            Log.WarningOnce(
                $"[MSSFP] Persona {def.defName} RollChatter threw: {ex.Message}",
                def.GetHashCode() ^ 0x43484154
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(line)) return;
        if (line.StartsWith("Cannot resolve") || line.Contains("[unresolved")) return;

        line = MSSFPHoloUtil.StripRichText(line);
        if (string.IsNullOrWhiteSpace(line)) return;

        MoteMaker.ThrowText(p.DrawPos + new Vector3(0f, 0f, 0.6f), p.Map, line, 3.65f);
    }

    /// <summary>
    /// Roll for a persona pawn-address line directed at a nearby colonist. Same snapshot +
    /// sibling-suppression discipline as <see cref="TryRollPersonaChatter"/>. Lower MTB than
    /// chatter (~1 line / 4 in-game hours) since addresses are more attention-grabbing.
    /// </summary>
    private static void TryRollPersonaAddress(Pawn p, CompHoloProjector proj)
    {
        Map m = proj.parent?.Map;
        AIPersonalityDef def = proj.Persona;
        if (m == null || def == null) return;
        if (!p.Spawned || p.Map != m) return;

        if (proj.HasLiveActiveSibling() && !ReferenceEquals(proj.projected, p))
            return;

        if (!Rand.MTBEventOccurs(4f, 60000f, 250f))
            return;

        Pawn target = FindNearestAddressableColonist(p);
        if (target == null) return;

        CompTrueAICore core = proj.parent.TryGetComp<CompTrueAICore>();
        if (core == null) return;

        AIPersonalityWorker worker = MSSFPHoloUtil.ResolveWorker(def);
        if (worker == null) return;

        string line;
        try
        {
            line = worker.RollPawnAddress(core, target);
        }
        catch (Exception ex)
        {
            Log.WarningOnce(
                $"[MSSFP] Persona {def.defName} RollPawnAddress threw: {ex.Message}",
                def.GetHashCode() ^ 0x41444452
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(line)) return;
        if (line.StartsWith("Cannot resolve") || line.Contains("[unresolved")) return;

        line = MSSFPHoloUtil.StripRichText(line);
        if (string.IsNullOrWhiteSpace(line)) return;

        MoteMaker.ThrowText(p.DrawPos + new Vector3(0f, 0f, 0.6f), p.Map, line, 3.65f);
    }

    /// <summary>
    /// Find nearest conscious colonist/slave within 8 cells of the holo. Skips other holos,
    /// downed/dead, mental-state pawns. Pure linear scan over FreeColonistsAndPrisonersSpawned
    /// — colony pop is small, no spatial index needed.
    /// </summary>
    private static Pawn FindNearestAddressableColonist(Pawn holo)
    {
        Pawn best = null;
        int bestDistSq = 8 * 8 + 1;
        foreach (Pawn c in holo.Map.mapPawns.FreeColonistsAndPrisonersSpawned)
        {
            if (c == holo) continue;
            if (MSSFPHoloUtil.IsHolo(c)) continue;
            if (c.Dead || c.Downed) continue;
            if (c.InMentalState) continue;
            if (!c.IsColonist && !c.IsSlaveOfColony) continue;
            int d = (c.Position - holo.Position).LengthHorizontalSquared;
            if (d < bestDistSq)
            {
                bestDistSq = d;
                best = c;
            }
        }
        return best;
    }

}
