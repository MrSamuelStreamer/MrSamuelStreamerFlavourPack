using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace MSSFP.Tunnels;

[StaticConstructorOnStartup]
public class Building_Tunnel : Building, IThingHolder, INotifyHauledTo
{
    public ThingOwner innerContainer;

    private static readonly Texture2D ViewPocketMapTex = ContentFinder<Texture2D>.Get("UI/Commands/ViewCave");
    private static readonly Texture2D CancelEnterTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");
    private static readonly Texture2D DefaultEnterTex = ContentFinder<Texture2D>.Get("UI/Commands/EnterCave");

    public SurfaceTile target;

    public List<TransferableOneWay> leftToLoad;

    public bool notifiedCantLoadMore;
    private int lastLoadPossibleTick = -1;

    private bool cachedAnyPawnCanLoad;
    private int lastAnyPawnCanLoadCheckTick = -1;

    public PlanetTile origin = PlanetTile.Invalid;
    public PlanetTile destination = PlanetTile.Invalid;

    protected virtual Texture2D EnterTex => DefaultEnterTex;

    public virtual string EnterString => "EnterPortal".Translate((NamedArgument)Label);

    public virtual string CancelEnterString => "CommandCancelEnterPortal".Translate();

    public virtual string EnteringString
    {
        get => "EnteringPortal".Translate((NamedArgument)Label);
    }

    public bool LoadInProgress => leftToLoad is { Count: > 0 };

    public bool AnyPawnCanLoadAnythingNow
    {
        get
        {
            if (lastAnyPawnCanLoadCheckTick == Find.TickManager.TicksGame)
                return cachedAnyPawnCanLoad;
            cachedAnyPawnCanLoad = TunnelUtilities.AnyPawnCanLoadAnythingNow(this);
            lastAnyPawnCanLoadCheckTick = Find.TickManager.TicksGame;
            return cachedAnyPawnCanLoad;
        }
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
        base.Destroy(mode);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref leftToLoad, "leftToLoad", LookMode.Deep);
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        Scribe_Values.Look(ref notifiedCantLoadMore, "notifiedCantLoadMore", false);
        Scribe_Values.Look(ref lastLoadPossibleTick, "lastLoadPossibleTick", -1);
        Scribe_Values.Look(ref destination, "destination", PlanetTile.Invalid);
        Scribe_Values.Look(ref origin, "origin", PlanetTile.Invalid);
        if (Scribe.mode != LoadSaveMode.PostLoadInit)
            return;
        leftToLoad?.RemoveAll(x => x?.AnyThing == null);
        innerContainer ??= new ThingOwner<Thing>(this);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (!TunnelUtilities.IsEnabled())
            return;

        WorldGenStep_Tunnels tunnels = new WorldGenStep_Tunnels();
        if (!tunnels.RegenerateNeeded(Tile))
            return;

        LongEventHandler.QueueLongEvent(
            () =>
            {
                tunnels.Regenerate(Tile.Layer);
                Find.World.renderer.AllDrawLayers.First(layer => layer is WorldDrawLayer_Tunnels).SetDirty();
            },
            "MSSFP_TunnelRegen",
            false,
            exception =>
            {
                ModLog.Error(exception.ToString());
            }
        );
    }

    protected override void Tick()
    {
        base.Tick();

        if (this.IsHashIntervalTick(60))
        {
            if (ShouldSendCaravanNow())
            {
                TunnelGenData.Instance.SendCaravan(this);
                return;
            }
        }

        if (!Spawned || !LoadInProgress)
        {
            lastLoadPossibleTick = -1;
            return;
        }

        if (AnyPawnCanLoadAnythingNow)
        {
            lastLoadPossibleTick = Find.TickManager.TicksGame;
            notifiedCantLoadMore = false; // Reset so if it gets stuck later, it can warn again
            return;
        }

        // New check: If there's a Lord, check if any owned pawns are still outside the tunnel.
        // This prevents the "can't load more" message if we're just waiting for the last pawn to walk in.
        Lord lord = Map.lordManager.lords.FirstOrDefault(l => l.LordJob is LordJob_LoadAndEnterTunnel lordJob && lordJob.tunnel == this);
        if (lord != null)
        {
            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (pawn.Spawned && pawn.Map == Map)
                {
                    lastLoadPossibleTick = Find.TickManager.TicksGame;
                    notifiedCantLoadMore = false;
                    return;
                }
            }
        }

        if (!this.IsHashIntervalTick(60))
            return;

        // If we've never been able to load, or it's been more than 10 seconds (600 ticks)
        if (lastLoadPossibleTick != -1 && (Find.TickManager.TicksGame - lastLoadPossibleTick) < 600)
            return;

        if (leftToLoad == null || leftToLoad.Count <= 0 || leftToLoad[0] == null || leftToLoad[0].AnyThing == null)
            return;

        if (notifiedCantLoadMore)
            return;

        notifiedCantLoadMore = true;
        Messages.Message("MessageCantLoadMoreIntoPortal".Translate((NamedArgument)Label, (NamedArgument)Faction.OfPlayer.def.pawnsPlural, (NamedArgument)leftToLoad[0].AnyThing), (Thing)this, MessageTypeDefOf.CautionInput);
    }

    public bool ShouldSendCaravanNow()
    {
        if (!innerContainer.OfType<Pawn>().Any())
            return false;

        if (!LoadInProgress)
        {
            // If no items left to load, check if all pawns from the Lord are inside.
            Lord lord = Map.lordManager.lords.FirstOrDefault(l => l.LordJob is LordJob_LoadAndEnterTunnel lordJob && lordJob.tunnel == this);
            if (lord == null)
            {
                return innerContainer.Any;
            }

            // If there is a lord, check if all its owned pawns are in the container.
            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (!innerContainer.Contains(pawn))
                    return false;
            }

            return true;
        }

        return false;
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, innerContainer);
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        innerContainer ??= new ThingOwner<Thing>(this);
        return innerContainer;
    }

    public virtual bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
    {
        if (!innerContainer.CanAcceptAnyOf(thing))
        {
            return false;
        }

        int originalCount = thing.stackCount;

        if (innerContainer.TryAddOrTransfer(thing))
        {
            SubtractFromToLoadList(thing, originalCount);
            return true;
        }
        return false;
    }

    public void AddToTheToLoadList(TransferableOneWay t, int count)
    {
        if (!t.HasAnyThing || count <= 0)
            return;
        if (leftToLoad == null)
            leftToLoad = new List<TransferableOneWay>();
        TransferableOneWay transferableOneWay1 = TransferableUtility.TransferableMatching(t.AnyThing, leftToLoad, TransferAsOneMode.PodsOrCaravanPacking);
        if (transferableOneWay1 != null)
        {
            for (int index = 0; index < t.things.Count; ++index)
            {
                if (!transferableOneWay1.things.Contains(t.things[index]))
                    transferableOneWay1.things.Add(t.things[index]);
            }
            if (!transferableOneWay1.CanAdjustBy(count).Accepted)
                return;
            transferableOneWay1.AdjustBy(count);
        }
        else
        {
            TransferableOneWay transferableOneWay2 = new TransferableOneWay();
            leftToLoad.Add(transferableOneWay2);
            transferableOneWay2.things.AddRange(t.things);
            transferableOneWay2.AdjustTo(count);
        }
    }

    public int SubtractFromToLoadList(Thing t, int count)
    {
        if (leftToLoad == null)
        {
            return 0;
        }

        // Try matching by instance first, then by def
        TransferableOneWay transferableOneWay = leftToLoad.FirstOrDefault(tow => tow.things.Contains(t) && tow.CountToTransfer > 0);
        if (transferableOneWay == null)
        {
            transferableOneWay = leftToLoad.FirstOrDefault(tow => tow.ThingDef == t.def && tow.CountToTransfer > 0);
        }
        if (transferableOneWay == null)
        {
            // One last try: anything matching the def even if CountToTransfer is 0 (shouldn't really happen if it's in leftToLoad)
            transferableOneWay = leftToLoad.FirstOrDefault(tow => tow.ThingDef == t.def);
        }

        if (transferableOneWay == null)
        {
            return 0;
        }

        int loadList = Mathf.Min(count, transferableOneWay.CountToTransfer);
        string label = "Unknown";
        try
        {
            label = transferableOneWay.Label;
        }
        catch (Exception)
        {
            // If Label fails (e.g., because AnyThing is null), we use def name
            if (transferableOneWay.ThingDef != null)
                label = transferableOneWay.ThingDef.label;
        }

        if (loadList > 0)
        {
            transferableOneWay.AdjustBy(-loadList);
        }

        // Always ensure the thing is removed from ALL transferables in leftToLoad of this type,
        // to prevent it from being "available" for other transferables if it's already loaded.
        foreach (var tow in leftToLoad)
        {
            if (tow.ThingDef == t.def && tow.things.Contains(t))
            {
                tow.things.Remove(t);
            }
        }

        if (transferableOneWay.CountToTransfer <= 0)
        {
            leftToLoad.Remove(transferableOneWay);
        }

        return loadList;
    }

    public void CancelLoad()
    {
        Lord oldLord = Map.lordManager.lords.FirstOrDefault(l => l.LordJob is LordJob_LoadAndEnterTunnel lordJob && lordJob.tunnel == this);
        if (oldLord != null)
            Map.lordManager.RemoveLord(oldLord);
        if (leftToLoad == null)
            return;
        leftToLoad.Clear();
        innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
    }

    public void ClearCaravan()
    {
        Lord oldLord = Map.lordManager.lords.FirstOrDefault(l => l.LordJob is LordJob_LoadAndEnterTunnel lordJob && lordJob.tunnel == this);
        if (oldLord != null)
            Map.lordManager.RemoveLord(oldLord);

        if (innerContainer != null)
        {
            innerContainer.ClearAndDestroyContents();
        }

        if (leftToLoad != null)
            leftToLoad.Clear();
        notifiedCantLoadMore = false;
        lastLoadPossibleTick = -1;
        destination = PlanetTile.Invalid;
    }

    public override string GetInspectString()
    {
        var loaded = innerContainer.ToList();

        if (loaded.Count == 0)
            return base.GetInspectString();

        string result = base.GetInspectString();
        if (!result.NullOrEmpty())
            result += "Contains:\n";

        result += string.Join("\n", loaded.Select(thing => " - " + thing.LabelCap));

        return result;
    }

    public virtual bool IsEnterable(out string reason)
    {
        foreach (ThingComp allComp in AllComps)
        {
            AcceptanceReport acceptanceReport = allComp.CanEnterPortal();
            if (!acceptanceReport.Accepted)
            {
                reason = acceptanceReport.Reason;
                return false;
            }
        }
        reason = "";
        return true;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos())
            yield return gizmo;
        yield return new Command_Action
        {
            action = delegate
            {
                HashSet<int> reachableTiles = TunnelGenData.Instance.GetReachableTilesFrom(Tile);
                List<WorldObject> wos = TunnelGenData.WorldObjectsWithTunnelEntrances()
                    .Where(wo => wo != null && wo.Tile != Map.Tile && reachableTiles.Contains(wo.Tile))
                    .ToList();

                wos.Remove(Find.WorldObjects.SiteAt(Map.Tile));

                FloatMenuUtility.MakeMenu(
                    wos,
                    entrance =>
                    {
                        var distance = Find.WorldGrid.ApproxDistanceInTiles(Tile, entrance.Tile);
                        return entrance.LabelCap + " [" + distance.ToStringDecimalIfSmall() + " tiles]";
                    },
                    entrance =>
                    {
                        return () =>
                        {
                            Find.WindowStack.Add(new Dialog_EnterTunnel(this, entrance));
                        };
                    }
                );
            },
            icon = EnterTex,
            defaultLabel = EnterString + "...",
            defaultDesc = "CommandEnterPortalDesc".Translate(Label),
            Disabled = !IsEnterable(out string text),
            disabledReason = text,
        };

        if (Prefs.DevMode)
        {
            yield return new Command_Action
            {
                defaultLabel = "DEV: Regenerate Tunnels",
                action = delegate
                {
                    TunnelGenData.RegenerateTunnels();
                },
            };
        }

        if (LoadInProgress || innerContainer.Any())
        {
            Command_Action gizmo2 = new Command_Action();
            gizmo2.action = CancelLoad;
            gizmo2.icon = CancelEnterTex;
            gizmo2.defaultLabel = CancelEnterString;
            gizmo2.defaultDesc = "CommandCancelEnterPortalDesc".Translate();
            yield return gizmo2;
        }
    }

    public void Notify_HauledTo(Pawn hauler, Thing thing, int count)
    {
        SubtractFromToLoadList(thing, count);
    }
}
