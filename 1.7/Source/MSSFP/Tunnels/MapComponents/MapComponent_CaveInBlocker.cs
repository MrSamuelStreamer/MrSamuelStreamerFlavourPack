using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Tunnels.MapComponents;

/// <summary>
/// Monitors a Cave-In encounter map. Polls every 60 ticks; once every mobile player
/// pawn has an unobstructed walking path to the map edge (debris mined clear), fires
/// a "path cleared" letter and sets IsCleared = true so the reform-blocking patch
/// stops blocking.
///
/// Activate() must be called from PostSetupEncounterMap after the component is added
/// to map.components. This guards against RimWorld's reflection-based auto-instantiation
/// of MapComponents on every map — without the _active flag a freshly constructed
/// (never-activated) component would fire "path cleared" on its very first tick
/// because there are no uncleared things to block it.
///
/// Only mobile (alive, not downed) player pawns are checked; downed pawns can be
/// carried by the others and are excluded so a single injured colonist doesn't
/// permanently block reform.
///
/// Persisted across save/load via ExposeData.
/// </summary>
public class MapComponent_CaveInBlocker : MapComponent
{
    private bool _cleared;
    private bool _active;

    public MapComponent_CaveInBlocker(Map map) : base(map) { }

    /// <summary>
    /// True when reform is allowed: either no cave-in is active on this map,
    /// or it was active and the path to the edge has been cleared.
    /// The (commit 4) reform-blocking patch checks this property.
    /// </summary>
    public bool IsCleared => !_active || _cleared;

    /// <summary>
    /// Marks this component as live. Call exactly once from PostSetupEncounterMap.
    /// </summary>
    public void Activate() => _active = true;

    public override void MapComponentTick()
    {
        if (!_active || _cleared) return;
        if (Find.TickManager.TicksGame % 60 != 0) return;

        List<Pawn> mobilePawns = map.mapPawns.AllPawnsSpawned
            .Where(p => p.Faction != null
                        && p.Faction == Faction.OfPlayer
                        && !p.Dead
                        && !p.Downed)
            .ToList();

        // No mobile pawns means everyone is dead or incapacitated — don't fire.
        if (mobilePawns.Count == 0) return;

        bool allCanEscape = mobilePawns.All(p =>
            map.reachability.CanReachMapEdge(p.Position, TraverseParms.For(p)));

        if (allCanEscape)
            OnPathCleared();
    }

    private void OnPathCleared()
    {
        _cleared = true;
        Find.LetterStack.ReceiveLetter(
            "MSSFP_Tunnels_Incidents_CaveInClearedLabel".Translate(),
            "MSSFP_Tunnels_Incidents_CaveInClearedText".Translate(),
            LetterDefOf.NeutralEvent);
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref _cleared, "cleared", false);
        Scribe_Values.Look(ref _active, "active", false);
    }
}
