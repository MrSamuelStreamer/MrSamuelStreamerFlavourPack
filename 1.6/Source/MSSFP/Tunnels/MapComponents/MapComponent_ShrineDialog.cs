using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Tunnels.MapComponents;

/// <summary>
/// Added to the encounter map by IncidentWorker_TunnelCaravanUndergroundShrine
/// in PostSetupEncounterMap. Fires Dialog_NodeTree on the first map tick when
/// the player is viewing the encounter map.
///
/// Prayer outcomes (RollShrineOutcome):
///   40% — MSSFP_Tunnels_Incidents_ShrineBlessed thought applied to all free colonists on the map.
///   40% — Neutral: nothing happens (message only).
///   20% — MSSFP_Tunnels_Incidents_ShrineCursed thought applied to all free colonists + one random
///          colonist takes minor blunt damage (3–8).
///
/// _active is false by default (parameterless constructor). This prevents the
/// component from doing anything on non-shrine maps where it is auto-instantiated
/// by RimWorld's MapComponent discovery system.
/// </summary>
public class MapComponent_ShrineDialog : MapComponent
{
    private bool _dialogShown;
    private bool _active;

    // Parameterless constructor required by RimWorld's reflection-based
    // MapComponent auto-instantiation (Map.FillComponents). _active stays false
    // so the component is inert on any map that didn't get the active constructor.
    public MapComponent_ShrineDialog(Map map) : base(map) { }

    public MapComponent_ShrineDialog(Map map, bool active) : base(map)
    {
        _active = active;
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
        DiaNode root = new DiaNode("MSSFP_Tunnels_Incidents_ShrineDialogText".Translate());

        // Option A: Approach and pray.
        DiaOption prayOption = new DiaOption("MSSFP_Tunnels_Incidents_ShrinePrayOption".Translate());
        prayOption.action = RollShrineOutcome;
        prayOption.resolveTree = true;
        root.options.Add(prayOption);

        // Option B: Leave undisturbed. No action; no outcome.
        DiaOption leaveOption = new DiaOption("MSSFP_Tunnels_Incidents_ShrineLeaveOption".Translate());
        leaveOption.resolveTree = true;
        root.options.Add(leaveOption);

        return new Dialog_NodeTree(root, delayInteractivity: true);
    }

    private void RollShrineOutcome()
    {
        float roll = Rand.Value;

        if (roll < 0.40f)
        {
            ApplyThoughtToAllColonists("MSSFP_TunnelShrineBlessed");
            Messages.Message("MSSFP_Tunnels_Incidents_ShrineBlessedMsg".Translate(),
                MessageTypeDefOf.NeutralEvent, historical: false);
        }
        else if (roll < 0.80f)
        {
            Messages.Message("MSSFP_Tunnels_Incidents_ShrineNeutralMsg".Translate(),
                MessageTypeDefOf.NeutralEvent, historical: false);
        }
        else
        {
            ApplyThoughtToAllColonists("MSSFP_TunnelShrineCursed");
            InflictCurseInjury();
            Messages.Message("MSSFP_Tunnels_Incidents_ShrineCursedMsg".Translate(),
                MessageTypeDefOf.NeutralEvent, historical: false);
        }
    }

    private void ApplyThoughtToAllColonists(string thoughtDefName)
    {
        ThoughtDef thoughtDef = ThoughtDef.Named(thoughtDefName);
        if (thoughtDef == null)
        {
            Log.Error("[MSSFP] UndergroundShrine: ThoughtDef '" + thoughtDefName + "' not found.");
            return;
        }

        foreach (Pawn pawn in map.mapPawns.FreeColonists)
            pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(thoughtDef);
    }

    private void InflictCurseInjury()
    {
        List<Pawn> colonists = map.mapPawns.FreeColonists.ToList();
        if (colonists.Count == 0) return;

        Pawn victim = colonists.RandomElement();
        if (victim.Dead) return;

        victim.TakeDamage(new DamageInfo(DamageDefOf.Blunt, Rand.Range(3f, 8f)));
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _dialogShown, "dialogShown", false);
        Scribe_Values.Look(ref _active, "active", false);
    }
}
