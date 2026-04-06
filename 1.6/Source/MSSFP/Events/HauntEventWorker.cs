using MSSFP.Defs;
using Verse;

namespace MSSFP.Events;

/// <summary>
/// Base class for poltergeist event workers. Each concrete subclass implements
/// one event type. Workers are instantiated once per def and reused.
/// </summary>
public abstract class HauntEventWorker
{
    public HauntEventDef def;

    /// <summary>
    /// Attempt to fire the event. Returns true if the event actually fired.
    /// The pawn is the haunted colonist that triggered the event selection.
    /// </summary>
    public abstract bool TryFire(Pawn pawn, Map map);
}
