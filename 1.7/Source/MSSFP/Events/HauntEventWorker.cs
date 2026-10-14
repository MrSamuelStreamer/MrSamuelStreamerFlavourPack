using MSSFP.Defs;
using MSSFP.Hediffs;
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
    /// sourceHaunt identifies the spirit causing the event (for attribution).
    /// </summary>
    public abstract bool TryFire(Pawn pawn, Map map, HediffComp_Haunt sourceHaunt = null);

    /// <summary>Resolves a display name for the spirit causing the event.</summary>
    protected static string SpiritName(HediffComp_Haunt source) =>
        source?.PawnName ?? source?.pawnToDraw?.LabelShort ?? "a restless spirit";
}
