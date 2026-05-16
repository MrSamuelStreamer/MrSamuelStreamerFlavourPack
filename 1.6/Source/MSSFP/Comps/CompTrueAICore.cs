using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MSSFP.AICore;
using MSSFP.Defs;
using MSSFP.Holo;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Comps;

/// <summary>
/// Reusable "True AI core" attached to any building. Holds an active <see cref="AIPersonalityDef"/>,
/// hauled art inputs (via <see cref="IThingHolder"/>), and the bookkeeping the chatter / art completion
/// logic reads in later impl steps.
///
/// Step 2 scaffold: state + IThingHolder + gizmos only. No chatter tick, no art completion yet —
/// those land in steps 7 and 8.
/// </summary>
public class CompTrueAICore : ThingComp, IThingHolder
{
    public CompProperties_TrueAICore Props => (CompProperties_TrueAICore)props;

    /// <summary>Currently-active personality. Set on spawn (or via gizmo). Never null after PostSpawnSetup.</summary>
    public AIPersonalityDef activePersonality;

    /// <summary>Per-core scratch for personality workers (which are stateless singletons).</summary>
    public Dictionary<string, string> personalityScratch = new Dictionary<string, string>();

    /// <summary>Last tick chatter fired. -1 = never.</summary>
    public int lastChatterTick = -1;

    /// <summary>Letters fired today (rolling, reset at <see cref="dayTickStart"/> + 60000 ticks).</summary>
    public int lettersToday;

    /// <summary>Tick the current "letter day" started.</summary>
    public int dayTickStart = -1;

    /// <summary>Hauled art inputs. Vanilla JobDriver_HaulToContainer writes into this.</summary>
    protected ThingOwner innerContainer;

    /// <summary>Player has clicked "Create AI art". WorkGiver scans for haul jobs while true.</summary>
    public bool artRequested;

    /// <summary>
    /// One-shot scribed flag — true after the spawn-announce Message has fired for this core.
    /// Suppresses repeat fires on minify→reinstall, rotate, save reload, quest delivery.
    /// </summary>
    public bool spawnAnnounced;

    /// <summary>Cached sibling holo projector comp (null if def has no projector). For inspect-string state line.</summary>
    private CompHoloProjector holoComp;

    /// <summary>Cached sibling power-trader comp (null on powerless variants). Powers the chatter / announce / art-completion tick gates.</summary>
    private CompPowerTrader powerComp;

    /// <summary>True when the core has power, or has no <see cref="CompPowerTrader"/> at all (powerless variant defs). Mirrors <see cref="CompHoloProjector.HasPower"/>.</summary>
    private bool IsPowered => powerComp == null || powerComp.PowerOn;

    public override void PostPostMake()
    {
        base.PostPostMake();
        innerContainer = new ThingOwner<Thing>(this);
    }

    public override void Initialize(CompProperties propsArg)
    {
        base.Initialize(propsArg);
        innerContainer ??= new ThingOwner<Thing>(this);
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (activePersonality == null)
        {
            activePersonality = Props.defaultPersonality ?? RollRandomPersonality();
        }
        if (dayTickStart < 0)
            dayTickStart = Find.TickManager.TicksGame;

        // Cache sibling comps for inspect-string state line + tick gates.
        holoComp = parent?.TryGetComp<CompHoloProjector>();
        powerComp = parent?.TryGetComp<CompPowerTrader>();

        // Spawn-announce is no longer fired here. Power-up timing on fresh placement is
        // unreliable (PowerNet not yet settled) and the announce must anchor to the holo
        // pawn when a projector sibling is present — which doesn't exist on PostSpawnSetup.
        // The announce is retried each rare tick (see CompTickRare) until it actually
        // emits, gated by IsPowered + AICoreSpeech.AnchorFor.
    }

    /// <summary>
    /// Chatter tick. Driven by parent ThingDef tickerType ≥ Rare (250-tick interval). The MTB roll
    /// here uses that 250-tick delta against <see cref="CompProperties_TrueAICore.chatterMtbHours"/>.
    ///
    /// Letter throttle: <see cref="CompProperties_TrueAICore.letterChance"/> rolled on each chatter
    /// hit, gated by <see cref="CompProperties_TrueAICore.lettersPerDay"/> + map StoryDanger=None.
    /// Without the danger gate a personality would fire flavour letters during raids — guaranteed
    /// player-aggro complaint.
    /// </summary>
    public override void CompTickRare()
    {
        base.CompTickRare();
        if (parent == null || !parent.Spawned) return;
        if (parent.Faction != Faction.OfPlayer) return;
        if (activePersonality == null) return;
        if (!IsPowered) return;

        // Deferred spawn-announce: retries each tick until it actually emits (powered AND, if a
        // holo projector sibling exists, projection live). spawnAnnounced is scribed, so the
        // retry survives save/load. Returning here on success avoids double-talk this tick.
        if (!spawnAnnounced)
        {
            if (AICoreSpeech.EmitMessage(this, $"AI core online — {activePersonality.LabelShortOrLabel}."))
            {
                spawnAnnounced = true;
                return;
            }
        }

        // Poll-driven art completion. ~250-tick worst-case latency between last haul and spawn —
        // acceptable for v1; cleaner than overriding ThingOwner.Notify_ItemAdded.
        if (artRequested && RemainingNeed == 0 && InputsHeld > 0)
        {
            TryCompleteArt();
            return;
        }

        float mtbHours = Props.chatterMtbHours;
        switch (Props.verbosity)
        {
            case AIVerbosity.Loud:
                mtbHours *= 0.5f;
                break;
            case AIVerbosity.Quiet:
                mtbHours *= 2.0f;
                break;
        }
        if (mtbHours <= 0f) return;

        if (!Rand.MTBEventOccurs(mtbHours, GenDate.TicksPerHour, 250f)) return;

        int now = Find.TickManager.TicksGame;
        if (lastChatterTick >= 0 && now - lastChatterTick < Props.chatterCooldownTicks) return;

        AIPersonalityWorker worker = activePersonality.Worker;
        if (worker == null) return;

        string line = worker.RollChatter(this);
        if (string.IsNullOrEmpty(line)) return;

        EnsureDayWindow(now);

        bool dangerOk =
            parent.Map?.dangerWatcher == null
            || parent.Map.dangerWatcher.DangerRating == StoryDanger.None;
        bool capOk = lettersToday < Props.lettersPerDay;
        bool fireLetter = dangerOk && capOk && Rand.Value < Props.letterChance;

        bool emitted;
        if (fireLetter)
        {
            emitted = AICoreSpeech.EmitLetter(this, activePersonality.LabelShortOrLabel, line, MSSFPDefOf.MSSFP_AICoreAlert);
            if (emitted) lettersToday++;
        }
        else
        {
            emitted = AICoreSpeech.EmitChatter(this, line);
        }

        // Only burn the cooldown window when an emit actually went through. A silent-suppress
        // (holo recalled, etc.) leaves the cooldown intact so the next powered/projected tick
        // can still talk.
        if (emitted) lastChatterTick = now;
    }

    /// <summary>
    /// Cached reflection handle for <c>CompArt.authorNameInt</c> — vanilla has no public setter
    /// that takes a non-Pawn name.
    /// </summary>
    private static FieldInfo authorNameIntField;

    /// <summary>
    /// Spawn an Awful sculpture authored by the active personality. Locked operation order
    /// (per plan §Decisions): MakeThing → SetQuality(Awful, Outsider) → InitializeArt →
    /// Title override → reflection-set <c>authorNameInt</c> → GenSpawn → HitPoints=Max.
    /// Reordering breaks the title override (vanilla short-circuits if titleInt already set
    /// when InitializeArt runs) or the quality (CompQuality post-spawn isn't safe in 1.6).
    ///
    /// Bails — leaving materials intact — if no spawn cell is reachable. Player can free a cell
    /// and re-tick will retry.
    /// </summary>
    public void TryCompleteArt()
    {
        if (!artRequested) return;
        if (parent == null || !parent.Spawned) return;
        if (Props.artOutputDef == null) return;
        if (RemainingNeed > 0) return;

        Verse.Map map = parent.Map;
        AIPersonalityDef pers = activePersonality;

        // Pick a spawn cell BEFORE making the thing — abort cheap if none.
        IntVec3 spawnAt = IntVec3.Invalid;
        foreach (IntVec3 c in GenAdj.CellsAdjacentCardinal(parent))
        {
            if (c.InBounds(map) && c.Standable(map) && c.GetFirstItem(map) == null)
            {
                spawnAt = c;
                break;
            }
        }
        if (!spawnAt.IsValid)
        {
            IntVec3 fallback = CellFinder.RandomClosewalkCellNear(
                parent.Position,
                map,
                3,
                c => c.Standable(map) && c.GetFirstItem(map) == null && c != parent.Position
            );
            if (fallback.IsValid && fallback.InBounds(map) && fallback != parent.Position)
                spawnAt = fallback;
        }
        if (!spawnAt.IsValid)
        {
            // Tier 3: parent's own cell. Sculpture stacks on top of building — looks intentional, not broken.
            // GenSpawn handles the placement collision.
            if (parent.Position.InBounds(map))
                spawnAt = parent.Position;
        }
        if (!spawnAt.IsValid)
        {
            // Tier 4: genuine failure (parent destroyed mid-tick, map gone). Surface to player so they
            // can move/deconstruct + retry. Leave materials, leave artRequested=true.
            AICoreSpeech.EmitMessage(
                this,
                "MSSFP_AICore_NoSpawnRoom".Translate(pers?.LabelShortOrLabel ?? "AI")
            );
            return;
        }

        ThingDef stuffDef = PickStuff();
        Thing sculpture;
        try
        {
            sculpture = ThingMaker.MakeThing(Props.artOutputDef, stuffDef);
        }
        catch (Exception e)
        {
            Log.Error($"[MSSFP] AICore art MakeThing failed (out={Props.artOutputDef?.defName} stuff={stuffDef?.defName}): {e}");
            return;
        }

        if (sculpture.TryGetComp<CompQuality>() is CompQuality quality)
            quality.SetQuality(QualityCategory.Awful, ArtGenerationContext.Outsider);

        if (sculpture.TryGetComp<CompArt>() is CompArt artComp)
        {
            artComp.InitializeArt(ArtGenerationContext.Outsider);

            string customTitle = pers?.Worker?.GenerateTitle(this);
            if (!string.IsNullOrEmpty(customTitle))
                artComp.Title = customTitle;

            if (pers != null)
                SetAuthor(artComp, pers.LabelShortOrLabel);
        }

        // Personality-flavoured image description — written to the sidecar comp; the Harmony postfix on
        // CompArt.GenerateImageDescription reads it. Wrapped in try/catch so a worker bug can't block
        // sculpture spawn — vanilla taleRef text remains as fallback.
        if (sculpture.TryGetComp<CompTrueAICoreArt>() is CompTrueAICoreArt artSidecar && pers?.Worker != null)
        {
            try
            {
                string desc = pers.Worker.GenerateDescription(this);
                if (!string.IsNullOrEmpty(desc))
                    artSidecar.flavouredDescription = desc;
            }
            catch (Exception e)
            {
                Log.Error($"[MSSFP] AICore worker.GenerateDescription threw: {e}");
            }
        }

        GenSpawn.Spawn(sculpture, spawnAt, map);
        sculpture.HitPoints = sculpture.MaxHitPoints;

        // Return non-chosen stuff to the world before destroying the rest. Mixed-stuff hauls otherwise
        // silently vaporise the minority (e.g. 40 stone + 35 wood → wood wins, stone destroyed). Player
        // sees leftover materials reappear adjacent to the core.
        if (innerContainer != null && stuffDef != null && innerContainer.Count > 1)
        {
            List<Thing> toDrop = new List<Thing>();
            for (int i = 0; i < innerContainer.Count; i++)
            {
                Thing th = innerContainer[i];
                if (th?.def != null && th.def != stuffDef)
                    toDrop.Add(th);
            }
            foreach (Thing th in toDrop)
            {
                innerContainer.TryDrop(th, parent.Position, map, ThingPlaceMode.Near, out _, null, null, true);
            }
        }

        innerContainer?.ClearAndDestroyContents();
        artRequested = false;

        // Worker hook for personality-specific side effects (extra letters, mood, etc.).
        try
        {
            pers?.Worker?.OnArtCompleted(this, sculpture);
        }
        catch (Exception e)
        {
            Log.Error($"[MSSFP] AICore worker.OnArtCompleted threw: {e}");
        }

        AICoreSpeech.EmitMessage(
            this,
            "MSSFP_AICore_ArtCompletedMsg".Translate(
                pers?.LabelShortOrLabel ?? "AI",
                sculpture.LabelShort
            )
        );
    }

    /// <summary>Most-numerous IsStuff def in the container; null if nothing is stuff (sculpture spawns stuffless).</summary>
    private ThingDef PickStuff()
    {
        if (innerContainer == null || innerContainer.Count == 0) return null;
        Dictionary<ThingDef, int> counts = new Dictionary<ThingDef, int>();
        for (int i = 0; i < innerContainer.Count; i++)
        {
            Thing th = innerContainer[i];
            if (th?.def == null || !th.def.IsStuff) continue;
            counts.TryGetValue(th.def, out int n);
            counts[th.def] = n + th.stackCount;
        }
        if (counts.Count == 0) return null;
        return counts.OrderByDescending(kv => kv.Value).First().Key;
    }

    /// <summary>Reflection-set <c>CompArt.authorNameInt</c>. Null-safe — silent fail leaves vanilla "Unknown".</summary>
    private static void SetAuthor(CompArt art, string name)
    {
        if (art == null || string.IsNullOrEmpty(name)) return;
        if (authorNameIntField == null)
        {
            authorNameIntField = typeof(CompArt).GetField(
                "authorNameInt",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
        }
        if (authorNameIntField == null) return;
        try
        {
            authorNameIntField.SetValue(art, new TaggedString(name));
        }
        catch (Exception e)
        {
            Log.WarningOnce($"[MSSFP] CompArt.authorNameInt reflect-set failed: {e}", 0x1A2B3C4D);
        }
    }

    /// <summary>
    /// Rolls the per-core "letter day" forward when a full <see cref="GenDate.TicksPerDay"/> has elapsed
    /// since <see cref="dayTickStart"/>. Cheap; called only on chatter hit.
    /// </summary>
    private void EnsureDayWindow(int now)
    {
        if (dayTickStart < 0)
        {
            dayTickStart = now;
            return;
        }
        if (now - dayTickStart >= GenDate.TicksPerDay)
        {
            dayTickStart = now;
            lettersToday = 0;
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Defs.Look(ref activePersonality, "activePersonality");
        Scribe_Collections.Look(
            ref personalityScratch,
            "personalityScratch",
            LookMode.Value,
            LookMode.Value
        );
        Scribe_Values.Look(ref lastChatterTick, "lastChatterTick", -1);
        Scribe_Values.Look(ref lettersToday, "lettersToday", 0);
        Scribe_Values.Look(ref dayTickStart, "dayTickStart", -1);
        Scribe_Values.Look(ref artRequested, "artRequested", false);
        Scribe_Values.Look(ref spawnAnnounced, "spawnAnnounced", false);
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            personalityScratch ??= new Dictionary<string, string>();
            innerContainer ??= new ThingOwner<Thing>(this);
        }
    }

    public ThingOwner GetDirectlyHeldThings() => innerContainer;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, (IList<Thing>)GetDirectlyHeldThings());
    }

    /// <summary>Total stack count of accepted-input items currently held.</summary>
    public int InputsHeld
    {
        get
        {
            if (innerContainer == null) return 0;
            int n = 0;
            for (int i = 0; i < innerContainer.Count; i++)
                n += innerContainer[i].stackCount;
            return n;
        }
    }

    /// <summary>How many more units the comp still needs before art completes.</summary>
    public int RemainingNeed
    {
        get
        {
            int target = Props != null ? Props.artInputCount : 0;
            int held = InputsHeld;
            return target - held > 0 ? target - held : 0;
        }
    }

    /// <summary>WorkGiver gate — true when player requested art and we still need more inputs.</summary>
    public bool WantsHaul => artRequested && RemainingNeed > 0 && parent != null && parent.Spawned;

    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new StringBuilder();
        if (activePersonality != null)
            sb.Append("MSSFP_AICore_ActivePersonality".Translate(activePersonality.LabelShortOrLabel));
        if (innerContainer != null && innerContainer.Count > 0)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.Append("MSSFP_AICore_HeldInputs".Translate(innerContainer.ContentsString));
        }
        // Holo projector state line — single owner for AI-core inspect output (DA decision:
        // CompHoloProjector intentionally does NOT override CompInspectStringExtra to avoid
        // duplicating the persona line when both comps live on the same building). Power
        // status is reflected indirectly via "Inactive (no power)" rather than a separate
        // line, since vanilla CompPowerTrader already prints power consumption / on-off state.
        if (holoComp != null)
        {
            string state;
            if (!holoComp.HasPower)
                state = "Inactive (no power)";
            else if (holoComp.projected != null)
                state = "Projecting";
            else if (holoComp.stored != null && holoComp.stored.Count > 0)
                state = "Stored";
            else
                state = "Inactive";
            if (sb.Length > 0) sb.AppendLine();
            sb.Append($"State: {state}");
        }
        return sb.ToString();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo g in base.CompGetGizmosExtra())
            yield return g;

        if (parent.Faction != Faction.OfPlayer)
            yield break;

        if (Props.showPersonalitySelector)
        {
            yield return new Command_Action
            {
                defaultLabel = "MSSFP_AICore_SwitchPersonality".Translate(),
                defaultDesc = "MSSFP_AICore_SwitchPersonalityDesc".Translate(),
                icon = activePersonality != null && !string.IsNullOrEmpty(activePersonality.iconPath)
                    ? ContentFinder<Texture2D>.Get(activePersonality.iconPath, false)
                    : null,
                action = OpenPersonalityFloatMenu,
            };
        }

        // "Create AI art" toggle — only when art output config present.
        if (Props.artOutputDef != null && Props.artInputs != null && Props.artInputs.Count > 0)
        {
            yield return new Command_Toggle
            {
                defaultLabel = "MSSFP_AICore_CreateArt".Translate(),
                defaultDesc = "MSSFP_AICore_CreateArtDesc".Translate(
                    Props.artInputCount,
                    InputsHeld
                ),
                icon = TexCommand.Install,
                isActive = () => artRequested,
                toggleAction = () =>
                {
                    artRequested = !artRequested;
                    if (!artRequested)
                    {
                        // Drop any active reservations so colonists abandon enroute hauls cleanly.
                        if (parent.Spawned && parent.Map != null)
                            parent.Map.reservationManager.ReleaseAllForTarget(parent);
                    }
                },
            };
        }

        // Dev-mode gizmos — bypass MTB / cooldown / verbosity for manual testing.
        if (DebugSettings.godMode)
        {
            yield return new Command_Action
            {
                defaultLabel = "DEV: force chatter",
                defaultDesc = "Roll one chatter line from the active personality and emit it as a bubble. Bypasses MTB and cooldown.",
                action = () =>
                {
                    if (activePersonality?.Worker == null)
                    {
                        Messages.Message("AI core has no active personality / worker.", MessageTypeDefOf.RejectInput, historical: false);
                        return;
                    }
                    string line = activePersonality.Worker.RollChatter(this);
                    if (string.IsNullOrEmpty(line))
                    {
                        Messages.Message($"Personality '{activePersonality.LabelShortOrLabel}' rolled an empty chatter line — RulePack may be empty.", MessageTypeDefOf.RejectInput, historical: false);
                        return;
                    }
                    AICoreSpeech.EmitChatter(this, line);
                    lastChatterTick = Find.TickManager.TicksGame;
                },
            };
        }
    }

    private void OpenPersonalityFloatMenu()
    {
        List<FloatMenuOption> opts = new List<FloatMenuOption>();
        foreach (AIPersonalityDef p in DefDatabase<AIPersonalityDef>.AllDefsListForReading)
        {
            AIPersonalityDef captured = p;
            opts.Add(
                new FloatMenuOption(
                    captured.LabelShortOrLabel,
                    () => SetPersonality(captured),
                    iconTex: !string.IsNullOrEmpty(captured.iconPath)
                        ? ContentFinder<Texture2D>.Get(captured.iconPath, false)
                        : null,
                    iconColor: captured.textColor
                )
            );
        }
        if (opts.Count == 0)
            opts.Add(new FloatMenuOption("MSSFP_AICore_NoPersonalities".Translate(), null));
        Find.WindowStack.Add(new FloatMenu(opts));
    }

    public void SetPersonality(AIPersonalityDef next)
    {
        if (next == null || next == activePersonality) return;
        activePersonality = next;
        personalityScratch.Clear();
        // Notify the sibling holo-projector (if any) so its persona-name one-shot re-applies
        // on the next projection. Default-roll on init does NOT hit this path (callers that
        // do default-rolls assign activePersonality directly, not via SetPersonality).
        parent?.TryGetComp<CompHoloProjector>()?.OnPersonaChanged();
    }

    private AIPersonalityDef RollRandomPersonality()
    {
        List<AIPersonalityDef> all = DefDatabase<AIPersonalityDef>.AllDefsListForReading;
        if (all.Count == 0) return null;
        return all.RandomElementByWeight(d => d.weight);
    }
}
