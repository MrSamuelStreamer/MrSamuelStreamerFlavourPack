using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace MSSFP.Tunnels;

public class ITab_ContentsTunnel : ITab_ContentsBase
{
    private List<Thing> tmpContainer = new List<Thing>();
    private Vector2 scrollLeftToLoad;
    private Vector2 scrollLoaded;
    private float lastHeightLeftToLoad;
    private float lastHeightLoaded;

    public override IList<Thing> container => tmpContainer;

    public override bool UseDiscardMessage => false;

    public Building_Tunnel Tunnel => SelThing as Building_Tunnel;

    public override bool IsVisible => Tunnel != null && (Tunnel.LoadInProgress || (Tunnel.innerContainer?.Any ?? false));

    public override IntVec3 DropOffset => IntVec3.Zero;

    public ITab_ContentsTunnel()
    {
        labelKey = "TabMapPortalContents";
        containedItemsKey = "";
    }

    protected override void DoItemsLists(Rect inRect, ref float curY)
    {
        Text.Font = GameFont.Small;

        bool forbidden = false;
        if (Tunnel.leftToLoad != null)
        {
            foreach (TransferableOneWay t in Tunnel.leftToLoad.Where(t => t.CountToTransfer > 0 && t.HasAnyThing))
            {
                for (int i = 0; i < t.things.Count; i++)
                {
                    if (t.things[i].IsForbidden(Faction.OfPlayer))
                    {
                        forbidden = true;
                        break;
                    }
                }
                if (forbidden)
                    break;
            }
        }

        if (forbidden)
        {
            Rect warningRect = new Rect(inRect.x, inRect.y, inRect.width, 40f);
            GUI.color = Color.yellow;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(warningRect, "ForbiddenItemsInLoadoutWarning".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            inRect.yMin += 45f;
        }

        float halfHeight = inRect.height / 2f;
        Rect leftToLoadRect = new Rect(inRect.x, inRect.y, inRect.width, halfHeight - 2f);
        Rect loadedRect = new Rect(inRect.x, inRect.y + halfHeight + 2f, inRect.width, halfHeight - 2f);

        // Left to Load Section
        DoLeftToLoadList(leftToLoadRect);

        // Loaded Section
        DoLoadedList(loadedRect);
    }

    private void DoLeftToLoadList(Rect rect)
    {
        Widgets.BeginGroup(rect);
        Rect viewRect = new Rect(0f, 0f, rect.width, lastHeightLeftToLoad);
        if (lastHeightLeftToLoad > rect.height)
            viewRect.width -= 16f;
        float curY = 0.0f;
        Widgets.BeginScrollView(rect.AtZero(), ref scrollLeftToLoad, viewRect);
        Widgets.ListSeparator(ref curY, viewRect.width, "ItemsToLoad".Translate());
        bool anyItems = false;
        if (Tunnel.leftToLoad != null)
        {
            foreach (TransferableOneWay t in Tunnel.leftToLoad.Where(t => t.CountToTransfer > 0 && t.HasAnyThing))
            {
                anyItems = true;
                TransferableOneWay t1 = t;
                DoThingRow(t.ThingDef, t.CountToTransfer, t.things, viewRect.width, ref curY, x => OnDropToLoadThing(t1, x));
            }
        }
        if (!anyItems)
            Widgets.NoneLabel(ref curY, viewRect.width);
        lastHeightLeftToLoad = curY;
        Widgets.EndScrollView();
        Widgets.EndGroup();
    }

    private void DoLoadedList(Rect rect)
    {
        Widgets.BeginGroup(rect);
        Rect viewRect = new Rect(0f, 0f, rect.width, lastHeightLoaded);
        if (lastHeightLoaded > rect.height)
            viewRect.width -= 16f;
        float curY = 0.0f;
        Widgets.BeginScrollView(rect.AtZero(), ref scrollLoaded, viewRect);

        // Header for Loaded section
        Rect headerRect = new Rect(0f, curY, viewRect.width, 24f);
        Widgets.ListSeparator(ref curY, viewRect.width, "LoadedItems".Translate());

        // Remove All button
        Rect removeAllRect = new Rect(viewRect.width - 100f, headerRect.y, 100f, 24f);
        if (Widgets.ButtonText(removeAllRect, "RemoveAll".Translate()))
        {
            List<Thing> toDrop = Tunnel.innerContainer.ToList();
            foreach (Thing t in toDrop)
            {
                if (Tunnel.innerContainer.TryDrop(t, Tunnel.Position, Tunnel.Map, ThingPlaceMode.Near, t.stackCount, out Thing droppedThing))
                {
                    if (droppedThing is Pawn pawn)
                    {
                        RemovePawnFromLoadLord(pawn);
                    }
                }
            }
        }

        bool anyLoaded = false;
        var loadedThings = Tunnel.innerContainer;
        if (loadedThings.Any)
        {
            anyLoaded = true;
            // Grouping loaded things to show them as rows, similar to how leftToLoad does it
            // Since it's a ThingOwner, we might have many stacks of the same ThingDef
            var grouped = loadedThings.GroupBy(x => x.def);
            foreach (var group in grouped.OrderBy(g => g.Key.label))
            {
                List<Thing> things = group.ToList();
                int count = things.Sum(x => x.stackCount);
                DoThingRow(group.Key, count, things, viewRect.width, ref curY, x => OnDropLoadedThing(group.Key, x));
            }
        }
        if (!anyLoaded)
            Widgets.NoneLabel(ref curY, viewRect.width);
        lastHeightLoaded = curY;
        Widgets.EndScrollView();
        Widgets.EndGroup();
    }

    private void OnDropLoadedThing(ThingDef def, int count)
    {
        var loadedThings = Tunnel.innerContainer;
        int remainingToRemove = count;
        for (int i = loadedThings.Count - 1; i >= 0 && remainingToRemove > 0; i--)
        {
            Thing t = loadedThings[i];
            if (t.def == def)
            {
                int toRemove = Mathf.Min(t.stackCount, remainingToRemove);
                if (loadedThings.TryDrop(t, Tunnel.Position, Tunnel.Map, ThingPlaceMode.Near, toRemove, out Thing droppedThing))
                {
                    if (droppedThing is Pawn pawn)
                    {
                        RemovePawnFromLoadLord(pawn);
                    }
                }
                remainingToRemove -= toRemove;
            }
        }
    }

    protected override void OnDropThing(Thing t, int count)
    {
        base.OnDropThing(t, count);
        if (!(t is Pawn pawn))
            return;
        RemovePawnFromLoadLord(pawn);
    }

    private void RemovePawnFromLoadLord(Pawn pawn)
    {
        Lord lord = pawn.GetLord();
        if (lord is not { LordJob: LordJob_LoadAndEnterTunnel })
            return;
        lord.Notify_PawnLost(pawn, PawnLostCondition.LeftVoluntarily);
    }

    private void OnDropToLoadThing(TransferableOneWay t, int count)
    {
        t.ForceTo(t.CountToTransfer - count);
        EndJobForEveryoneHauling(t);
        foreach (Thing thing in t.things)
        {
            if (thing is Pawn pawn)
                RemovePawnFromLoadLord(pawn);
        }
    }

    private void EndJobForEveryoneHauling(TransferableOneWay t)
    {
        IReadOnlyList<Pawn> allPawnsSpawned = SelThing.Map.mapPawns.AllPawnsSpawned;
        foreach (Pawn t1 in allPawnsSpawned)
        {
            if (t1.CurJobDef == MSSFPDefOf.MSSFP_HaulToTunnel)
            {
                JobDriver_HaulToTunnel curDriver = (JobDriver_HaulToTunnel)t1.jobs.curDriver;
                if (curDriver.Tunnel == Tunnel && curDriver.ThingToCarry != null && curDriver.ThingToCarry.def == t.ThingDef)
                    t1.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }
    }
}
