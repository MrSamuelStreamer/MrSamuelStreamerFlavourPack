using System.Collections.Generic;
using MSSFP.Comps;
using MSSFP.Defs;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.Holo;

/// <summary>
/// Projector-side comp. Owns the holo Pawn (stored in <see cref="stored"/> when recalled,
/// spawned on map while projected) and the tint state. Projection is map-bound to the
/// projector's map; vanilla AI picks jobs anywhere on that map (no area-leash gating).
///
/// SCRIBE POLICY (revised — never world-pawn):
///   - Holo pawn lives in <see cref="stored"/> (a <see cref="ThingOwner{Pawn}"/>) which is
///     deep-scribed via <see cref="Scribe_Deep.Look"/>. ThingOwner is the deep scribe
///     authority — pawn is NEVER passed to <see cref="Find.WorldPawns"/>.
///   - Rationale: <c>WorldPawns.PassToWorld</c> scrambles the faction of a player-faction
///     pawn (player-colonist is invalid as a world pawn), wiping <c>IsFreeColonist</c> and
///     the colonist need set. Keeping the pawn comp-owned preserves Player faction + needs.
///   - <see cref="projected"/> is scribed by reference (the spawned-on-map pawn is owned
///     by the map's thingOwner at save time).
///   - Save migration (PostLoadInit): legacy saves had pawn in WorldPawns w/ scrambled
///     faction; on load, remove from WorldPawns, re-assign Player faction, recalc needs.
///     Legacy `area` / `radius` scribe nodes are silently ignored by RimWorld on load
///     (Scribe tolerates unknown XML); no migration shim needed.
///
/// HEDIFF + COMP BACK-REF: applied at projection time inside
/// <see cref="SpawnProjection"/> (NEVER from PostExposeData — P0 decision).
/// </summary>
public class CompHoloProjector : ThingComp, IThingHolder
{
    public CompProperties_HoloProjector Props => (CompProperties_HoloProjector) props;

    // ------- Persisted state -------

    /// <summary>Holo pawn when not projected. Empty while projected.</summary>
    public ThingOwner<Pawn> stored;

    /// <summary>Currently-projected pawn, or null. By reference (world pawn).</summary>
    public Pawn projected;

    /// <summary>
    /// Tint applied to the projection. Resolved per-read from the active persona
    /// (<see cref="AIPersonalityDef.HoloTintOrTextColor"/>) so each AI keeps its own color
    /// identity without a separate scribed field. Falls back to <see cref="CompProperties_HoloProjector.defaultTint"/>
    /// when no persona is bound.
    /// </summary>
    public Color Tint => Persona?.HoloTintOrTextColor ?? Props.defaultTint;

    /// <summary>
    /// Scribed one-shot — true once the persona-derived name has been applied to the holo
    /// pawn. Cleared by <see cref="OnPersonaChanged"/> so a SetPersonality swap re-applies
    /// the rename on the next projection. Prevents clobbering player renames on every recall.
    /// </summary>
    public bool personaNameApplied;

    // ------- Runtime caches (NOT scribed) -------

    /// <summary>
    /// Cached list of CompHoloProjector siblings on the same cell. Built at PostSpawnSetup,
    /// invalidated via <see cref="NotifyProjectorChanged"/> on adjacent spawn/despawn.
    /// </summary>
    private List<CompHoloProjector> siblingsCache;

    /// <summary>Cached power comp (null = no power requirement). Populated in PostSpawnSetup unconditionally.</summary>
    private CompPowerTrader powerComp;

    /// <summary>Cached sibling AI core comp for cross-comp reads (inspect-string state line).</summary>
    private CompTrueAICore aiCoreComp;

    /// <summary>
    /// Consecutive rare-tick count of unpowered state. Debounces single-tick power blips
    /// (EMP, conduit damage, breakdown toggle) so a brief outage doesn't permanently recall
    /// the projection. Reset on any powered tick.
    /// </summary>
    private int unpoweredTicks;

    /// <summary>
    /// Post-load PowerNet settle window. CompPowerTrader.PowerOn reads from PowerNet which
    /// rebuilds AFTER PostLoadInit; first rare ticks can report transient !PowerOn on a
    /// perfectly-fine save. Skip power logic for this many ticks after spawn.
    /// </summary>
    private int loadWarmupTicks;

    /// <summary>
    /// Edge-trigger latch for power-return auto-respawn. False -> true transition while
    /// projection is recalled-and-stored triggers automatic re-projection. Mirrors vanilla
    /// cryptosleep/biosculpter resume idiom.
    /// </summary>
    private bool hadPowerLastTick;

    /// <summary>
    /// Scribed one-shot — true once the very first projection has been attempted for this
    /// projector instance. Used to drive fresh-placement auto-project (waits for PowerNet
    /// settle then spawns automatically) without re-firing on every load or after a player
    /// deliberately recalls. Set true in <see cref="CompTickRare"/> after the first attempt
    /// AND forced true in PostExposeData PostLoadInit so loading an existing save never
    /// triggers a "first" projection regardless of recall state.
    /// </summary>
    public bool firstProjectionDone;

    /// <summary>
    /// Ticks remaining in an in-progress Defrag cycle. Zero = idle. Decremented in
    /// <see cref="CompTickRare"/> by <see cref="GenTicks.TickRareInterval"/>. While &gt; 0
    /// the projection is held recalled, power draw is doubled, and auto-respawn is
    /// suppressed. On hit-zero, chemical hediffs + injuries are stripped and auto-respawn
    /// is re-armed by clearing <see cref="hadPowerLastTick"/>.
    /// </summary>
    public int defragTicksRemaining;

    /// <summary>Total defrag duration — 2 in-game days (60,000 ticks/day).</summary>
    public const int DefragDurationTicks = 120000;

    /// <summary>Power-consumption multiplier applied while <see cref="IsDefragging"/>.</summary>
    public const float DefragPowerMultiplier = 2f;

    /// <summary>True while a Defrag cycle is in progress.</summary>
    public bool IsDefragging => defragTicksRemaining > 0;

    /// <summary>True when the projector has power, or has no CompPowerTrader at all (powerless variant defs).</summary>
    public bool HasPower => powerComp == null || powerComp.PowerOn;

    // ------- Init -------

    public override void Initialize(CompProperties propsArg)
    {
        base.Initialize(propsArg);
        stored ??= new ThingOwner<Pawn>(this);
    }

    public override void PostPostMake()
    {
        base.PostPostMake();
        stored ??= new ThingOwner<Pawn>(this);
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);

        // Build sibling cache + notify same-cell neighbours so their caches include us.
        RebuildSiblingCache();
        if (parent?.Map != null)
            MSSFPHoloUtil.NotifyProjectorChanged(parent.Map, parent.Position);

        // Cache comps unconditionally (must populate on both fresh-place AND respawningAfterLoad).
        // Without unconditional caching, a saved unpowered-but-projecting state would never
        // despawn after load because powerComp would be null and HasPower would fall through
        // to the null-true branch.
        powerComp = parent?.TryGetComp<CompPowerTrader>();
        aiCoreComp = parent?.TryGetComp<CompTrueAICore>();
        if (powerComp == null && parent?.def != null && parent.def.HasComp(typeof(CompPowerTrader)))
        {
            Log.WarningOnce(
                $"[MSSFP] CompHoloProjector on {parent.def.defName}: def lists CompPowerTrader but TryGetComp returned null. Power-gating disabled for this instance.",
                parent.def.GetHashCode() ^ 0x504F5752
            );
        }

        // Tick-local state init. Warmup skips power logic until PowerNet settles after load.
        loadWarmupTicks = 2;
        unpoweredTicks = 0;
        hadPowerLastTick = HasPower;
    }

    // ------- IThingHolder -------

    public void GetChildHolders(List<IThingHolder> outChildren) =>
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());

    public ThingOwner GetDirectlyHeldThings() => stored;

    // ------- Persona link (reads sibling AI core comp) -------

    /// <summary>Resolves persona from the projector's <see cref="CompTrueAICore"/>, if any.</summary>
    public AIPersonalityDef Persona => parent?.TryGetComp<CompTrueAICore>()?.activePersonality;

    // ------- Persona rename one-shot (Concern 1 + 2) -------

    /// <summary>
    /// Apply the persona's sanitised label as a NameTriple to the holo pawn. Idempotent:
    /// guarded by <see cref="personaNameApplied"/> so it runs exactly once per persona
    /// assignment (cleared by <see cref="OnPersonaChanged"/>).
    /// </summary>
    private void ApplyPersonaName(Pawn p)
    {
        if (p == null) return;
        AIPersonalityDef def = Persona;
        if (def == null) return;
        string clean = MSSFPHoloUtil.SanitizePersonaLabel(def.LabelShortOrLabel);
        p.Name = new NameTriple(clean, clean, string.Empty);
        personaNameApplied = true;
    }

    /// <summary>
    /// Invoked by <see cref="CompTrueAICore.SetPersonality"/> when the player explicitly
    /// swaps personality. Clears the rename one-shot AND, if a projection is live, renames
    /// the active pawn immediately so the persona swap is reflected without waiting for a
    /// recall+respawn cycle. NOT called during default-roll on PostSpawnSetup.
    /// </summary>
    public void OnPersonaChanged()
    {
        personaNameApplied = false;
        if (projected != null && Persona != null)
            ApplyPersonaName(projected);
    }

    // ------- Sibling-projector cache (Concern 4 + 6) -------

    /// <summary>
    /// Rebuild the same-cell sibling cache. Cheap (GetThingList is local). Called on
    /// PostSpawnSetup + when a neighbour notifies us via <see cref="MSSFPHoloUtil.NotifyProjectorChanged"/>.
    /// </summary>
    private void RebuildSiblingCache()
    {
        siblingsCache = new List<CompHoloProjector>();
        if (parent?.Map == null) return;
        foreach (Thing t in parent.Position.GetThingList(parent.Map))
        {
            if (t == parent) continue;
            CompHoloProjector c = t.TryGetComp<CompHoloProjector>();
            if (c != null) siblingsCache.Add(c);
        }
    }

    /// <summary>Public re-entry point for <see cref="MSSFPHoloUtil.NotifyProjectorChanged"/>.</summary>
    public void RebuildSiblingCacheExternal() => RebuildSiblingCache();

    /// <summary>
    /// True iff any cached sibling has a live, spawned, same-map projection right now.
    /// Lazy-prunes destroyed siblings from the cache. Used by chatter to suppress duplicate
    /// emissions when multiple projectors share a cell.
    /// </summary>
    public bool HasLiveActiveSibling()
    {
        if (siblingsCache == null) return false;
        for (int i = siblingsCache.Count - 1; i >= 0; i--)
        {
            CompHoloProjector s = siblingsCache[i];
            if (s?.parent == null || s.parent.Destroyed)
            {
                siblingsCache.RemoveAt(i);
                continue;
            }
            if (s.projected != null
                && s.projected.Spawned
                && s.projected.Map == parent?.Map)
                return true;
        }
        return false;
    }

    // ------- Lifecycle -------

    /// <summary>
    /// Generate the holo pawn lazily (first projection). Pass it straight to WorldPawns
    /// KeepForever so the world is its deep-scribe authority.
    /// </summary>
    public Pawn EnsureHoloPawn()
    {
        if (stored.Count > 0)
            return stored[0];
        if (projected != null)
            return projected;

        PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(Props.pawnKindDefName);
        if (kind == null)
        {
            Log.Error($"[MSSFP] CompHoloProjector: PawnKindDef '{Props.pawnKindDefName}' not found.");
            return null;
        }

        PawnGenerationRequest req = new(
            kind: kind,
            faction: Faction.OfPlayer,
            context: PawnGenerationContext.NonPlayer,
            tile: parent.Tile,
            forceGenerateNewPawn: true,
            allowDead: false,
            allowDowned: false,
            canGeneratePawnRelations: false,
            mustBeCapableOfViolence: false,
            colonistRelationChanceFactor: 0f,
            forceAddFreeWarmLayerIfNeeded: false,
            allowGay: true,
            allowFood: false,
            allowAddictions: false,
            inhabitant: false,
            certainlyBeenInCryptosleep: false,
            forceRedressWorldPawnIfFormerColonist: false,
            biocodeWeaponChance: 0f,
            biocodeApparelChance: 0f,
            extraPawnForExtraRelationChance: null,
            relationWithExtraPawnChanceFactor: 0f,
            validatorPreGear: null,
            validatorPostGear: null,
            forcedTraits: null,
            prohibitedTraits: null
        );

        // Persona-driven body overrides — only the projector's current persona at
        // first-projection time wins. Subsequent persona swaps do NOT regenerate the
        // already-stored pawn (avoids wiping accrued pawn state). See AIPersonalityDef
        // doc-comments for full caveat.
        AIPersonalityDef personaForGen = Persona;
        if (personaForGen != null)
        {
            if (personaForGen.forcedXenotype != null)
                req.ForcedXenotype = personaForGen.forcedXenotype;
            if (personaForGen.fixedGender.HasValue)
                req.FixedGender = personaForGen.fixedGender.Value;
        }

        Pawn p = PawnGenerator.GeneratePawn(req);
        // Hard-strip any apparel / equipment / inventory that slipped through PawnApparelGenerator.
        // Holos are illusory bodies — they MUST spawn nude. PawnKindDef apparelMoney=0~0 is not
        // enough on its own: backstory-required, biome-required, or faction-required apparel can
        // still land here. Strip after generation, before the pawn enters `stored`.
        // Drop instead of destroy: vanilla `DestroyAll` paths fire equipment/apparel notifications
        // we don't need here, but ThingOwner.ClearAndDestroyContents semantics are well-defined
        // and there are no listeners on a freshly-generated, never-spawned pawn.
        p.apparel?.DestroyAll(DestroyMode.Vanish);
        p.equipment?.DestroyAllEquipment(DestroyMode.Vanish);
        p.inventory?.DestroyAll(DestroyMode.Vanish);
        // Deliberately NOT calling WorldPawns.PassToWorld — PassToWorld scrambles the
        // faction of a player-colonist pawn. Pawn is deep-scribed via the `stored`
        // ThingOwner instead.
        stored.TryAdd(p, canMergeWithExistingStacks: false);
        return p;
    }

    /// <summary>
    /// Move pawn from <see cref="stored"/> onto the map adjacent to the projector, attach
    /// the projected-back-ref comp + hediff.
    /// </summary>
    public bool SpawnProjection()
    {
        if (projected != null)
        { Log.Warning("[MSSFP] Already projected."); return false; }
        if (parent?.Map == null)
        { Log.Warning("[MSSFP] Projector not on a map."); return false; }
        if (!HasPower)
        {
            Messages.Message(
                "AI core lacks power.",
                parent,
                MessageTypeDefOf.RejectInput,
                historical: false
            );
            return false;
        }

        Pawn p = EnsureHoloPawn();
        if (p == null)
            return false;

        // Take pawn out of stored (does NOT discard the world-pawn ref).
        stored.Remove(p);

        IntVec3 cell = CellFinder.StandableCellNear(parent.Position, parent.Map, 3)
            .IsValid ? CellFinder.StandableCellNear(parent.Position, parent.Map, 3) : parent.Position;

        GenSpawn.Spawn(p, cell, parent.Map);
        projected = p;

        MSSFPHoloUtil.AddHoloComp(p, parent);
        MSSFPHoloUtil.AddOrRefreshHologramHediff(p);

        // Scrub any latched drafted state from a prior projection or pre-fix save. The
        // Drafted setter prefix blocks draft=true on holos but always allows undraft, so
        // this call goes through and clears the field. Without this, a holo restored with
        // drafted=true (or drafted via a side path before the comp was attached) would be
        // stuck-drafted permanently — the draft gizmo is hidden, so there's no UI exit.
        if (p.drafter != null && p.drafter.Drafted)
            p.drafter.Drafted = false;

        // Persona-flavour fixed hediffs (e.g. Hollee → Dementia). Idempotent: re-projection
        // doesn't stack. Must run AFTER AddHoloComp so the hediff-filter patch can resolve
        // the persona; allowedHediffs/fixedHediffs entries bypass the filter.
        HoloHediffPolicy.ApplyFixedHediffs(p, Persona);

        // Mark this pawn as an AI projection. Idempotent + caps Artistic at 4
        // via SkillRecord_Patch — see HoloHediffPolicy.ApplyHoloTrait.
        HoloHediffPolicy.ApplyHoloTrait(p);

        // Persona rename one-shot. Skips after first apply unless OnPersonaChanged() fired.
        if (!personaNameApplied && Persona != null)
            ApplyPersonaName(p);

        // Hediff carries <disablesNeeds>Food, Rest</disablesNeeds>. NeedsTracker only
        // re-evaluates on its own ticker, so force a recalc now to drop the needs the
        // moment the hediff lands rather than seeing one frame of Food/Rest at default.
        p.needs?.AddOrRemoveNeedsAsAppropriate();

        return true;
    }

    /// <summary>Remove projection from map, return to <see cref="stored"/>.</summary>
    /// <remarks>
    /// After DeSpawn the pawn is in limbo (no map, no holder). ThingOwner.TryAdd takes
    /// ownership — that becomes the scribe authority. No world-pawn round-trip.
    /// </remarks>
    public bool OnDespawnProjection()
    {
        if (projected == null)
            return false;

        Pawn p = projected;
        if (p.Spawned)
            p.DeSpawn(DestroyMode.Vanish);

        stored.TryAdd(p, canMergeWithExistingStacks: false);
        projected = null;
        return true;
    }

    /// <summary>
    /// Routes projection collapse to recall + player letter. Called from two paths:
    ///   1. <see cref="CompHoloProjected.PostPreApplyDamage"/> when absorbed damage exceeds threshold (DamageInfo present, Hediff null)
    ///   2. <see cref="MSSFP.HarmonyPatches.Pawn_Kill_HoloIntercept_Patch"/> when external code calls Pawn.Kill (DamageInfo nullable, Hediff carries plague/asphyxia/etc.)
    /// </summary>
    public void OnProjectionCollapsed(DamageInfo? dinfo, Hediff exactCulprit = null)
    {
        Pawn p = projected;
        string pawnLabel = p?.LabelShort ?? "Holo";
        string source =
            dinfo?.Instigator?.LabelShort
            ?? exactCulprit?.LabelCap.ToString()
            ?? "unknown source";
        OnDespawnProjection();
        Find.LetterStack.ReceiveLetter(
            "Holo projection collapsed",
            $"{pawnLabel}'s projection collapsed from {source}. The projector has recalled the pawn.",
            LetterDefOf.NegativeEvent,
            parent
        );
    }

    /// <summary>
    /// Projector-side power gating. Drives recall on power loss + auto-respawn on power return.
    /// Independent of <see cref="CompHoloProjected.CompTickRare"/> which runs persona chatter
    /// on the pawn side — that path keeps firing while drafted; this one bails out cleanly so
    /// a power blip mid-combat doesn't yank a drafted holo away from the player.
    ///
    /// Order of operations per tick:
    ///   0. External-destroy cleanup: detect when something outside MSSFP destroyed our
    ///      projected pawn (no Pawn.Destroy intercept patch — see Vehicle Framework compat
    ///      note in <see cref="MSSFP.HarmonyPatches.Pawn_Kill_HoloIntercept_Patch"/>).
    ///   1. Load-warmup decrement (skip power logic for ~2 rare ticks after load while PowerNet rebuilds).
    ///   2. Debounced power-loss recall: 2 consecutive !PowerOn rare ticks → silent OnDespawnProjection.
    ///      Skipped when projected is drafted (player control wins).
    ///   3. Edge-trigger auto-respawn: power off→on transition with stored pawn → SpawnProjection.
    /// </summary>
    public override void CompTickRare()
    {
        base.CompTickRare();
        if (parent?.Map == null) return;

        // Catch externally-destroyed projections. Pawn.Kill is intercepted, but the
        // companion Pawn.Destroy prefix is intentionally absent (VEF parallel-renderer
        // bug — bool prefixes on Pawn.Destroy break VEF). So a direct pawn.Destroy()
        // call from dev gizmos or third-party code bypasses our recall path. Detect
        // here, clear the ref, fire the standard collapse letter.
        if (projected != null && projected.Destroyed)
        {
            string label = projected.LabelShortCap;
            projected = null;
            Find.LetterStack.ReceiveLetter(
                "Holo projection collapsed",
                $"{label}'s projection collapsed (pawn destroyed externally).",
                LetterDefOf.NegativeEvent,
                parent
            );
            hadPowerLastTick = HasPower;
            return;
        }

        if (loadWarmupTicks > 0)
        {
            loadWarmupTicks--;
            hadPowerLastTick = HasPower;
            return;
        }

        // ----- Defrag pre-empts normal power-gating / auto-respawn -----
        // Override PowerOutput every rare-tick (not just on edges) — vanilla PowerNet may
        // reset it during net rebuilds, and our override is the only signal that lifts the
        // draw to 2x. Decrement timer; on hit-zero, FinishDefrag clears hediffs and clears
        // hadPowerLastTick so the auto-respawn branch below re-fires next rare tick.
        if (IsDefragging)
        {
            if (powerComp != null)
                powerComp.PowerOutput = -powerComp.Props.PowerConsumption * DefragPowerMultiplier;
            defragTicksRemaining -= GenTicks.TickRareInterval;
            if (defragTicksRemaining <= 0)
                FinishDefrag();
            else
            {
                hadPowerLastTick = HasPower;
                return;
            }
        }

        bool nowPowered = HasPower;

        // Debounced power-loss recall. Drafted projection keeps player control —
        // unless they undraft while still unpowered, debounce resumes counting next tick.
        if (projected != null && !nowPowered && !projected.Drafted)
        {
            unpoweredTicks++;
            if (unpoweredTicks >= 2)
            {
                OnDespawnProjection();
                Messages.Message(
                    "AI core lost power — projection recalled.",
                    parent,
                    MessageTypeDefOf.NeutralEvent,
                    historical: false
                );
                unpoweredTicks = 0;
            }
        }
        else
        {
            unpoweredTicks = 0;
        }

        // Edge-trigger auto-respawn on power return. Mirrors vanilla cryptosleep/biosculpter
        // resume idiom. Symmetric with power-loss recall: player flips breaker, holo returns.
        if (projected == null && stored.Count > 0 && nowPowered && !hadPowerLastTick)
        {
            if (SpawnProjection())
            {
                Messages.Message(
                    "AI core powered — projection restored.",
                    parent,
                    MessageTypeDefOf.PositiveEvent,
                    historical: false
                );
            }
        }

        // Fresh-placement auto-project. Fires exactly once per projector lifetime, after
        // PowerNet has settled (warmup elapsed) and power is available. Stored pawn is created
        // lazily by SpawnProjection -> EnsureHoloPawn, so we don't need a separate stored.Count
        // check. The persistence flag is forced true in PostExposeData PostLoadInit so existing
        // saves never re-trigger first-projection regardless of the player's last recall state.
        if (!firstProjectionDone && projected == null && nowPowered)
        {
            firstProjectionDone = true;
            SpawnProjection();
        }

        hadPowerLastTick = nowPowered;
    }

    // ------- Despawn cleanup -------

    public override void PostDeSpawn(Map map, DestroyMode mode)
    {
        base.PostDeSpawn(map, mode);
        // If the projector itself is being despawned (e.g. minified) while a projection is
        // active, recall first so we don't leak a holo pawn on the map.
        if (projected != null)
            OnDespawnProjection();
        // Notify same-cell neighbours so their sibling caches drop our (now-destroyed) ref.
        if (map != null && parent != null)
            MSSFPHoloUtil.NotifyProjectorChanged(map, parent.Position);
        siblingsCache = null;
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        // Building destroyed for good — recall projection first, then dispose stored pawn.
        if (projected != null)
            OnDespawnProjection();
        // Safety net: legacy saves may still have stored pawn registered in WorldPawns.
        // Remove from world before destroying so GC isn't blocked.
        foreach (Pawn p in new List<Pawn>(stored))
        {
            if (p == null)
                continue;
            if (p.IsWorldPawn())
                Find.WorldPawns.RemovePawn(p);
            // Bypass Pawn.Kill/Destroy intercept — projector is gone, we genuinely want the
            // holo pawn destroyed (no recall to route to). ClearAndDestroyContents would
            // hit the intercept and silently no-op, leaking pawns.
            MSSFPHoloUtil.DestroyHoloForReal(p, DestroyMode.Vanish);
        }
        stored.Clear();
    }

    // ------- Scribe (ThingOwner is deep authority — never world-pawn) -------

    public override void PostExposeData()
    {
        base.PostExposeData();

        Scribe_Deep.Look(ref stored, "stored", this);
        Scribe_References.Look(ref projected, "projected");
        Scribe_Values.Look(ref personaNameApplied, "personaNameApplied", false);
        Scribe_Values.Look(ref firstProjectionDone, "firstProjectionDone", false);
        Scribe_Values.Look(ref defragTicksRemaining, "defragTicksRemaining", 0);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            stored ??= new ThingOwner<Pawn>(this);

            // Existing saves never auto-projected, so the player's recall state at save time
            // is authoritative. Force this true regardless of the scribed value so a load can
            // never re-fire the fresh-placement auto-project against an intentionally-recalled
            // projector. Scribed field stays primarily as a future-proof reset point.
            firstProjectionDone = true;

            // Legacy-save migration: prior versions PassToWorld'd the pawn, which
            // scrambled its faction to a random outlander. On load, pull it back out
            // of WorldPawns and re-anchor it on the player faction with the right needs.
            foreach (Pawn p in stored)
            {
                if (p == null)
                    continue;
                if (p.IsWorldPawn())
                    Find.WorldPawns.RemovePawn(p);
                if (p.Faction != Faction.OfPlayer)
                    p.SetFaction(Faction.OfPlayer);
                p.needs?.AddOrRemoveNeedsAsAppropriate();
            }

            // Legacy-save persona name migration: if pawn still has its vanilla-generated
            // NameTriple (no nick) and a persona is present, apply the persona name now so
            // pre-feature saves pick up the wiring on first load. New saves persist the
            // applied flag and skip this branch.
            if (!personaNameApplied && Persona != null && stored != null)
            {
                foreach (Pawn p in stored)
                {
                    if (p == null) continue;
                    if (p.Name is NameTriple nt && string.IsNullOrEmpty(nt.Nick))
                    {
                        ApplyPersonaName(p);
                        break;
                    }
                }
            }
        }
    }

    // ------- Dev gizmos (P1-A only — replaced by full UI in P1-B) -------

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo g in base.CompGetGizmosExtra())
            yield return g;

        // Defrag — production gizmo. Requires a live projection (the action recalls it).
        // Disabled while already defragging.
        yield return new Command_Action
        {
            defaultLabel = "Defrag",
            defaultDesc = $"Take the projection offline for 2 in-game days. Power draw doubles for the duration. On completion, all chemical hediffs and injuries are cleared from the holo pawn.",
            icon = ContentFinder<Texture2D>.Get("UI/MSSFP_Defrag", reportFailure: false),
            action = () => StartDefrag(),
            Disabled = projected == null || IsDefragging,
            disabledReason = IsDefragging
                ? "Defrag in progress."
                : "Projection must be live to defrag.",
        };

        // Production tint is now persona-driven via AIPersonalityDef.HoloTintOrTextColor —
        // no runtime picker. To recolor a persona, edit its XML def and reload defs.
        // Dev gizmos require active godMode (not just DevMode prefs) — matches the
        // "Switch personality" gate on CompTrueAICore so all dev-only knobs are
        // consistently gated by godMode.
        if (!DebugSettings.godMode)
            yield break;

        yield return new Command_Action
        {
            defaultLabel = "DEV: Project test holo",
            defaultDesc = "Spawn the stored holo pawn adjacent to this projector.",
            action = () => SpawnProjection(),
            Disabled = projected != null,
            disabledReason = "Already projected.",
        };

        yield return new Command_Action
        {
            defaultLabel = "DEV: Recall holo",
            defaultDesc = "Despawn the projection and return it to storage.",
            action = () => OnDespawnProjection(),
            Disabled = projected == null,
            disabledReason = "No projection active.",
        };

        yield return new Command_Action
        {
            defaultLabel = "DEV: Skip defrag",
            defaultDesc = "Finish the current Defrag cycle immediately.",
            action = () => { defragTicksRemaining = 1; },
            Disabled = !IsDefragging,
            disabledReason = "No defrag in progress.",
        };
    }

    // ------- Defrag -------

    /// <summary>
    /// Begin a Defrag cycle. Recalls the live projection, arms the countdown timer, and
    /// returns true. Returns false (no-op) if a cycle is already running or no projection
    /// is live to recall.
    /// </summary>
    public bool StartDefrag()
    {
        if (IsDefragging) return false;
        if (projected == null) return false;

        OnDespawnProjection();
        defragTicksRemaining = DefragDurationTicks;

        Find.LetterStack.ReceiveLetter(
            "Defrag started",
            $"{(Persona?.LabelShortOrLabel ?? "Holo")} is defragmenting. Projection offline for 2 days; power draw doubled.",
            LetterDefOf.NeutralEvent,
            parent
        );
        return true;
    }

    /// <summary>
    /// End-of-defrag cleanup. Wipes chemical hediffs + injuries from the stored pawn,
    /// restores baseline power output, and clears the auto-respawn edge-trigger latch so
    /// the next <see cref="CompTickRare"/> re-projects.
    /// </summary>
    private void FinishDefrag()
    {
        defragTicksRemaining = 0;

        if (stored.Count > 0)
        {
            Pawn p = stored[0];
            List<Hediff> removable = HoloHediffPolicy.CollectDefragRemovable(p);
            foreach (Hediff h in removable)
                p.health.RemoveHediff(h);
        }

        // Restore baseline power consumption (PowerOutput was held at -2x during the cycle).
        if (powerComp != null)
            powerComp.PowerOutput = -powerComp.Props.PowerConsumption;

        // Re-arm the auto-respawn edge trigger: forcing hadPowerLastTick = false makes the
        // next rare tick (with power still on) see a false→true transition and re-project.
        hadPowerLastTick = false;

        Find.LetterStack.ReceiveLetter(
            "Defrag complete",
            $"{(Persona?.LabelShortOrLabel ?? "Holo")} defragmented. Memory clean.",
            LetterDefOf.PositiveEvent,
            parent
        );
    }

    public override string CompInspectStringExtra()
    {
        string baseStr = base.CompInspectStringExtra();
        if (!IsDefragging) return baseStr;

        int days = defragTicksRemaining / 60000;
        int hours = (defragTicksRemaining % 60000) / 2500;
        string defragLine = $"Defragging: {days}d {hours}h remaining";
        return string.IsNullOrEmpty(baseStr) ? defragLine : $"{baseStr}\n{defragLine}";
    }
}
