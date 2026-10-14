using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Holo;

/// <summary>
/// Game-scoped registry of "holo apparel clones" — runtime-tagged apparel instances that exist
/// only as projected duplicates worn by holo pawns. Tracked by <see cref="Thing.thingIDNumber"/>
/// because runtime-added ThingComp instances do NOT round-trip <see cref="Pawn_ApparelTracker"/>
/// scribe (apparel comps are rebuilt from def.comps on load; dynamic adds are dropped).
///
/// SCRIBE: <see cref="cloneIds"/> persists with the game save. On load the GameComponent
/// instantiates and re-reads the set, restoring marker semantics for any worn-but-saved clones.
///
/// MARKER LIFECYCLE:
///   - <see cref="HoloApparelFactory"/> calls <see cref="Mark"/> immediately after clone construction.
///   - <see cref="MSSFP.HarmonyPatches.Thing_Destroy_HoloApparelCloneCleanup_Patch"/> calls
///     <see cref="Unmark"/> in a postfix on <see cref="Thing.Destroy"/> — catches every destroy
///     path (vanish, drop-then-destroy, mod-direct) so the set doesn't leak monotonic IDs.
/// </summary>
public class HoloApparelRegistry : GameComponent
{
    private HashSet<int> cloneIds = new();

    /// <summary>Convenience accessor. Null when no game loaded (main menu).</summary>
    public static HoloApparelRegistry Instance => Current.Game?.GetComponent<HoloApparelRegistry>();

    public HoloApparelRegistry(Game game) { }

    public void Mark(Thing t)
    {
        if (t == null) return;
        cloneIds.Add(t.thingIDNumber);
    }

    public bool IsClone(Thing t)
    {
        if (t == null) return false;
        return cloneIds.Contains(t.thingIDNumber);
    }

    public void Unmark(Thing t)
    {
        if (t == null) return;
        cloneIds.Remove(t.thingIDNumber);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref cloneIds, "cloneIds", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit && cloneIds == null)
            cloneIds = new HashSet<int>();
    }
}
