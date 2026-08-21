using Verse;

namespace MSSFP.Tunnels.MapComponents;

/// <summary>
/// DEPRECATED — kept only for save-game compatibility.
///
/// Non-combat tunnel incidents no longer use a ghost pawn. They extend
/// IncidentWorker_TunnelCaravanNonCombat, which bypasses IncidentWorker_Ambush.DoExecute
/// entirely, sets up the map with an empty enemy list, and sends the letter with
/// map.Parent as the look target. No ghost is required.
///
/// This class is retained in the assembly so that saves created before the refactor
/// (which serialised this component on encounter maps) can still be loaded without error.
/// On the first MapComponentTick the _ghost field is null, so the component does nothing
/// except remove itself from the map.
/// </summary>
public class MapComponent_DespawnGhostNextTick : MapComponent
{
    private Pawn _ghost;

    // Parameterless (Map-only) constructor required by RimWorld's reflection-based
    // MapComponent auto-instantiation (Map.FillComponents). _ghost stays null and
    // MapComponentTick does nothing until a real instance is added manually.
    public MapComponent_DespawnGhostNextTick(Map map) : base(map) { }

    public MapComponent_DespawnGhostNextTick(Map map, Pawn ghost) : base(map)
    {
        _ghost = ghost;
    }

    public override void MapComponentTick()
    {
        if (_ghost?.Spawned == true)
            _ghost.DeSpawn();

        map.components.Remove(this);
        _ghost = null;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref _ghost, "ghost");
    }
}
