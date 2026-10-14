using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using MSSFP.Tunnels.Lords;

namespace MSSFP.Tunnels.MapComponents;

/// <summary>
/// Added to the encounter map in PostProcessGeneratedPawnsAfterSpawning.
/// Fires Dialog_NodeTree on the first map tick when the player is viewing
/// the encounter map. Handles silver counting/consumption.
///
/// Payment path: DeSpawn() the toll collector directly. LordToil_ExitMap is
/// NOT used because tunnel encounter maps are enclosed caves with no map edges
/// to pathfind to — the pawn would wander indefinitely. DeSpawn() removes the
/// pawn immediately; the lord dissolves when it has no more active pawns.
/// MapComponent_SuppressBattleWon prevents the spurious "Caravan victorious!"
/// letter that would otherwise fire once the map is clear of hostile threats.
/// </summary>
public class MapComponent_TollCollectorDialog : MapComponent
{
    private const int TollAmount = 150;

    private Pawn _tollCollector;
    private bool _dialogShown;
    private bool _active;

    // Parameterless constructor required by RimWorld's reflection-based
    // MapComponent auto-instantiation. _active = false → tick does nothing.
    public MapComponent_TollCollectorDialog(Map map) : base(map) { }

    /// <summary>
    /// Marks this component as live. Call exactly once from
    /// PostProcessGeneratedPawnsAfterSpawning on the auto-instantiated component
    /// fetched via map.GetComponent&lt;T&gt;() — never construct a second instance.
    /// </summary>
    public void Activate(Pawn tollCollector)
    {
        _tollCollector = tollCollector;
        _active = true;
    }

    public override void MapComponentTick()
    {
        if (!_active) return;
        if (_dialogShown) return;
        if (Find.CurrentMap != map) return;
        if (Find.TickManager.TicksGame % 60 != 0) return;

        _dialogShown = true;
        Find.WindowStack.Add(BuildDialog());
    }

    private Dialog_NodeTree BuildDialog()
    {
        DiaNode root = new DiaNode("MSSFP_Tunnels_Incidents_TollCollectorDialogText".Translate());

        // Option A: Pay
        int available = CountSilver();
        DiaOption payOption = new DiaOption("MSSFP_Tunnels_Incidents_TollCollectorPayOption".Translate(TollAmount));
        if (available < TollAmount)
        {
            payOption.Disable("MSSFP_Tunnels_Incidents_TollCollectorNotEnoughSilver".Translate(available));
        }
        else
        {
            payOption.action = () =>
            {
                ConsumeSilver(TollAmount);
                // DeSpawn directly — LordToil_ExitMap cannot be used on enclosed cave
                // maps (no accessible map edges). Lord dissolves when pawn is lost.
                if (_tollCollector?.Spawned == true)
                    _tollCollector.DeSpawn();
                Messages.Message("MSSFP_Tunnels_Incidents_TollPaidMsg".Translate(), MessageTypeDefOf.NeutralEvent, historical: false);
            };
        }
        payOption.resolveTree = true;
        root.options.Add(payOption);

        // Option B: Refuse
        DiaOption refuseOption = new DiaOption("MSSFP_Tunnels_Incidents_TollCollectorRefuseOption".Translate());
        refuseOption.action = () =>
        {
            _tollCollector?.lord?.ReceiveMemo(LordJob_TollCollector.MemoTollRefused);
        };
        refuseOption.resolveTree = true;
        root.options.Add(refuseOption);

        return new Dialog_NodeTree(root, delayInteractivity: true);
    }

    private int CountSilver()
    {
        return map.mapPawns.FreeColonists
            .Sum(p => p.inventory.innerContainer.TotalStackCountOfDef(ThingDefOf.Silver));
    }

    private void ConsumeSilver(int amount)
    {
        int remaining = amount;
        foreach (Pawn pawn in map.mapPawns.FreeColonists.ToList())
        {
            if (remaining <= 0) break;
            foreach (Thing thing in pawn.inventory.innerContainer.ToList())
            {
                if (remaining <= 0) break;
                if (thing.def != ThingDefOf.Silver) continue;

                int take = Mathf.Min(remaining, thing.stackCount);
                remaining -= take;
                thing.stackCount -= take;
                if (thing.stackCount <= 0)
                    thing.Destroy();
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref _tollCollector, "tollCollector");
        Scribe_Values.Look(ref _dialogShown, "dialogShown", false);
        Scribe_Values.Look(ref _active, "active", false);
    }
}
