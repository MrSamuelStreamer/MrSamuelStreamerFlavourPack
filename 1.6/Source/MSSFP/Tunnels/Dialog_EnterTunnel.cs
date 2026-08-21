using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MSSFP.Tunnels;

public class Dialog_EnterTunnel : Window
{
    private const float TitleRectHeight = 35f;
    private const float BottomAreaHeight = 55f;
    private readonly Vector2 BottomButtonSize = new Vector2(160f, 40f);
    private Building_Tunnel tunnel;
    private WorldObject destination;
    private List<Pawn> preSelectedPawns;
    private List<TransferableOneWay> transferables;
    private TransferableOneWayWidget pawnsTransfer;
    private TransferableOneWayWidget itemsTransfer;
    private Tab tab;
    private static List<TabRecord> tabsList = new List<TabRecord>();

    public override Vector2 InitialSize => new Vector2(1024f, UI.screenHeight);

    protected override float Margin => 0.0f;

    public Dialog_EnterTunnel(Building_Tunnel tunnel, WorldObject destination, List<Pawn> preSelectedPawns = null)
    {
        this.tunnel = tunnel;
        this.destination = destination;
        this.preSelectedPawns = preSelectedPawns;
        forcePause = true;
        absorbInputAroundWindow = true;
    }

    public override void PostOpen()
    {
        base.PostOpen();
        CalculateAndRecacheTransferables();
    }

    public override void DoWindowContents(Rect inRect)
    {
        Rect rect1 = new Rect(0.0f, 0.0f, inRect.width, 35f);
        using (new TextBlock(GameFont.Medium, TextAnchor.MiddleCenter))
            Widgets.Label(rect1, tunnel.EnterString);
        tabsList.Clear();
        tabsList.Add(new TabRecord("PawnsTab".Translate(), () => tab = Tab.Pawns, tab == Tab.Pawns));
        tabsList.Add(new TabRecord("ItemsTab".Translate(), () => tab = Tab.Items, tab == Tab.Items));
        inRect.yMin += 67f;
        Widgets.DrawMenuSection(inRect);
        TabDrawer.DrawTabs(inRect, tabsList);
        inRect = inRect.ContractedBy(17f);
        Widgets.BeginGroup(inRect);
        Rect rect2 = inRect.AtZero();
        DoBottomButtons(rect2);
        Rect inRect1 = rect2;
        inRect1.yMax -= 76f;
        bool anythingChanged = false;
        switch (tab)
        {
            case Tab.Pawns:
                pawnsTransfer.OnGUI(inRect1, out anythingChanged);
                break;
            case Tab.Items:
                itemsTransfer.OnGUI(inRect1, out anythingChanged);
                break;
        }
        Widgets.EndGroup();
    }

    private void DoBottomButtons(Rect rect)
    {
        if (Widgets.ButtonText(new Rect((float)(rect.width / 2.0 - BottomButtonSize.x / 2.0), (float)(rect.height - 55.0 - 17.0), BottomButtonSize.x, BottomButtonSize.y), "ResetButton".Translate()))
        {
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            CalculateAndRecacheTransferables();
        }
        if (Widgets.ButtonText(new Rect(0.0f, (float)(rect.height - 55.0 - 17.0), BottomButtonSize.x, BottomButtonSize.y), "CancelButton".Translate()))
            Close();
        if (!Widgets.ButtonText(new Rect(rect.width - BottomButtonSize.x, (float)(rect.height - 55.0 - 17.0), BottomButtonSize.x, BottomButtonSize.y), "AcceptButton".Translate()) || !TryAccept())
            return;
        SoundDefOf.Tick_High.PlayOneShotOnCamera();
        Close(false);
    }

    private bool TryAccept()
    {
        List<Pawn> fromTransferables = TransferableUtility.GetPawnsFromTransferables(transferables);
        if (!fromTransferables.Any())
        {
            Messages.Message("MessagePortalMustHavePawn".Translate(), MessageTypeDefOf.RejectInput);
            return false;
        }
        tunnel.leftToLoad = new List<TransferableOneWay>();
        foreach (TransferableOneWay transferable in transferables)
            tunnel.AddToTheToLoadList(transferable, transferable.CountToTransfer);
        TunnelUtilities.MakeLordsAsAppropriate(fromTransferables, tunnel);
        tunnel.destination = destination.Tile;
        tunnel.origin = tunnel.Map.Tile;
        return true;
    }

    private void CalculateAndRecacheTransferables()
    {
        transferables = new List<TransferableOneWay>();
        if (tunnel.LoadInProgress)
        {
            foreach (TransferableOneWay transferableOneWay in tunnel.leftToLoad)
                transferables.Add(transferableOneWay);
        }
        AddPawnsToTransferables();
        AddItemsToTransferables();

        if (preSelectedPawns != null)
        {
            foreach (Pawn pawn in preSelectedPawns)
            {
                TransferableOneWay transferable = TransferableUtility.TransferableMatching(pawn, transferables, TransferAsOneMode.PodsOrCaravanPacking);
                if (transferable != null)
                {
                    transferable.ForceTo(1);
                }
            }
        }

        foreach (Thing t in TunnelUtilities.ThingsBeingHauledTo(tunnel))
            AddToTransferables(t);
        pawnsTransfer = new TransferableOneWayWidget(null, null, null, "TransferMapPortalColonyThingCountTip".Translate(), true, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, true, (Func<float>)(() => float.MaxValue), tile: tunnel.Map.Tile, drawEquippedWeapon: true);
        CaravanUIUtility.AddPawnsSections(pawnsTransfer, transferables);
        itemsTransfer = new TransferableOneWayWidget(transferables.Where(x => x.ThingDef.category != ThingCategory.Pawn), null, null, "TransferMapPortalColonyThingCountTip".Translate(), true, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, true, (Func<float>)(() => float.MaxValue), tile: tunnel.Map.Tile);
    }

    private void AddToTransferables(Thing t)
    {
        if (tunnel.LoadInProgress && tunnel.leftToLoad.Any(trans => trans.things.Contains(t)))
            return;
        TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatchingDesperate(t, transferables, TransferAsOneMode.PodsOrCaravanPacking);
        if (transferableOneWay == null)
        {
            transferableOneWay = new TransferableOneWay();
            transferables.Add(transferableOneWay);
        }
        if (transferableOneWay.things.Contains(t))
            Log.Error("Tried to add the same thing twice to TransferableOneWay: " + t);
        else
            transferableOneWay.things.Add(t);
    }

    private void AddPawnsToTransferables()
    {
        foreach (Thing allSendablePawn in CaravanFormingUtility.AllSendablePawns(tunnel.Map, true, allowLodgers: true))
            AddToTransferables(allSendablePawn);
    }

    private void AddItemsToTransferables()
    {
        foreach (Thing thing in tunnel.Map.listerThings.AllThings)
        {
            if (thing.def.category == ThingCategory.Item && thing.Spawned && !thing.IsForbidden(Faction.OfPlayer))
            {
                AddToTransferables(thing);
            }
        }
    }

    public override void OnAcceptKeyPressed()
    {
        if (!TryAccept())
            return;
        SoundDefOf.Tick_High.PlayOneShotOnCamera();
        Close(false);
    }

    private enum Tab
    {
        Pawns,
        Items,
    }
}
