using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MSSFP.Comps.Game;

/// <summary>
/// Safety net for humanlike pawns stranded inside another pawn's carry tracker.
///
/// DELIBERATELY LIVES IN THE CORE ASSEMBLY, not in a compat layer. The failure this
/// guards against is precisely the case where a compat assembly is *gone*: MSSFP's
/// Rimbody pawn-lifting layer (see <c>MSSFP.RB</c>) is gated by <c>loadFolders.xml</c>
/// on Rimbody being active. If a save is written mid-lift and the player then removes
/// Rimbody — or a Rimbody update breaks our hard assembly reference — the JobDef and its
/// JobDriver both vanish. On load the orphaned job is dropped, but the carried colonist
/// stays inside <c>Pawn_CarryTracker.innerContainer</c>, which IS scribed with the save.
/// No vanilla system ever rescues a humanlike out of another pawn's carry tracker, so the
/// colonist is neither on the map nor in any caravan: silent, unrecoverable pawn loss.
///
/// This component references no Rimbody types whatsoever, so it keeps working after the
/// mod that created the situation is uninstalled.
///
/// The two-strike rule matters. A carrier legitimately holds a pawn with a null job for a
/// tick or two during normal job handoff (rescue, capture, baby transport), so dropping on
/// first sight would fight vanilla. A carrier still holding a humanlike with no job a full
/// sweep interval later is genuinely stuck.
/// </summary>
public class CarriedPawnRescueGameComponent : GameComponent
{
    // RimWorld constructs GameComponents by reflection with the Game instance; we keep
    // no reference to it, everything this needs comes from Find.
    public CarriedPawnRescueGameComponent(Verse.Game game) { }

    private const int SweepIntervalTicks = 2500;

    /// <summary>Carrier thingIDNumbers seen holding a jobless humanlike on the previous sweep.</summary>
    private readonly HashSet<int> suspectedStuck = new();

    public override void FinalizeInit()
    {
        // Loading a save is the highest-value moment to check: it is exactly when a
        // removed compat assembly leaves an orphaned carry behind.
        suspectedStuck.Clear();
        Sweep(forceDrop: true);
    }

    public override void GameComponentTick()
    {
        if (Find.TickManager.TicksGame % SweepIntervalTicks != 0)
            return;

        Sweep(forceDrop: false);
    }

    /// <param name="forceDrop">
    /// Skip the two-strike rule. Used at load time, where a stuck carry has already
    /// survived a save/load cycle and needs no further corroboration.
    /// </param>
    private void Sweep(bool forceDrop)
    {
        List<Verse.Map> maps = Find.Maps;
        if (maps == null)
            return;

        HashSet<int> seenThisSweep = new();

        foreach (Verse.Map map in maps)
        {
            foreach (Pawn carrier in map.mapPawns.AllPawnsSpawned.ToList())
            {
                if (carrier?.carryTracker?.CarriedThing is not Pawn carried)
                    continue;
                if (carried.RaceProps is not { Humanlike: true })
                    continue;
                // A live job means someone is deliberately carrying this pawn — a rescue,
                // an arrest, a baby being moved. Not our business.
                if (carrier.CurJob != null)
                    continue;

                seenThisSweep.Add(carrier.thingIDNumber);

                if (!forceDrop && !suspectedStuck.Contains(carrier.thingIDNumber))
                    continue;

                if (carrier.carryTracker.TryDropCarriedThing(carrier.Position, ThingPlaceMode.Near, out Thing dropped))
                {
                    ModLog.Warn(
                        $"Rescued {carried.LabelShortCap} from {carrier.LabelShortCap}'s carry tracker "
                            + $"(carrier had no job). Dropped at {(dropped?.Position ?? carrier.Position)}."
                    );
                }
                else
                {
                    ModLog.Error(
                        $"Could not drop {carried.LabelShortCap} carried by {carrier.LabelShortCap}; "
                            + "pawn may be stuck in the carry tracker."
                    );
                }
            }
        }

        suspectedStuck.Clear();
        foreach (int id in seenThisSweep)
            suspectedStuck.Add(id);
    }
}
