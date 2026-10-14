using MSSFP.Comps.Map;
using RimWorld;
using Verse;

namespace MSSFP.Comps;

/// <summary>
///     Gives a raid-spawned <see cref="MSSFP.Things.Building_IEDTrap" /> a finite
///     lifespan. Most hostile IEDs left undisarmed eventually decay into a dud
///     (silent vanish); a minority detonate on schedule instead. One warning
///     letter fires a day before the first trap of a raid batch is due, via
///     <see cref="IEDDecayMapComponent" />.
///
///     Player-faction traps (re-deployed from a successful disarm's minified
///     drop) never decay — mirrors the same faction check
///     <see cref="MSSFP.Things.Building_IEDTrap" /> uses everywhere else to tell
///     "still hostile" apart from "player-owned".
/// </summary>
public class CompIEDDecay : ThingComp
{
    /// <summary>Shared id stamped by the deployer that scattered this trap; drives one-warning-per-raid.</summary>
    public int batchId;

    private int spawnTick;
    private int jitterTicks;
    private bool warned;

    private const int CheckInterval = 250; // CompTickRare cadence — decay resolves in days, not ticks.

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        if (!respawningAfterLoad)
        {
            spawnTick = GenTicks.TicksGame;
            jitterTicks = Rand.Range(0, GenDate.TicksPerDay);
            return;
        }

        // Save-compat: a trap scribed before this feature existed has spawnTick 0,
        // which would make Age == TicksGame and expire it on the very next check.
        // Grandfather it to a full lifespan counted from load instead.
        if (spawnTick == 0)
        {
            spawnTick = GenTicks.TicksGame;
        }
    }

    public override void CompTick() => CheckDecay(1);

    public override void CompTickRare() => CheckDecay(CheckInterval);

    private void CheckDecay(int interval)
    {
        if (!parent.Spawned) return;
        if (parent.Faction == Faction.OfPlayer) return;

        int lifespanDays = MSSFPMod.settings.IEDLifespanDays;
        if (lifespanDays <= 0) return;

        int lifespan = lifespanDays * GenDate.TicksPerDay + jitterTicks;
        int age = GenTicks.TicksGame - spawnTick;

        if (!warned && age >= lifespan - GenDate.TicksPerDay)
        {
            warned = true;
            parent.Map.GetComponent<IEDDecayMapComponent>()?.TryWarnBatch(batchId, parent.Position, parent.Map);
        }

        if (age >= lifespan)
        {
            Expire();
        }
    }

    private void Expire()
    {
        CompExplosive explosive = parent.GetComp<CompExplosive>();

        if (Rand.Value < MSSFPMod.settings.IEDDecayDetonateChance)
        {
            // Reuse the same wick path a normal spring takes so blast type, sound,
            // overlay and blast-mark filth all match — reads as "it finally went off"
            // rather than a scripted event.
            if (explosive != null)
            {
                explosive.StartWick();
            }
            else
            {
                parent.Destroy(DestroyMode.Vanish);
            }
            return;
        }

        // Dud — stop any latent wick first so a half-armed trap can't pop after
        // despawn (same reasoning as Building_IEDTrap.OnDisarmSuccess).
        explosive?.StopWick();
        if (!parent.Destroyed)
        {
            parent.Destroy(DestroyMode.Vanish);
        }
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref spawnTick, "iedSpawnTick");
        Scribe_Values.Look(ref jitterTicks, "iedJitterTicks");
        Scribe_Values.Look(ref batchId, "iedBatchId");
        Scribe_Values.Look(ref warned, "iedWarned");
    }

    public override string CompInspectStringExtra()
    {
        int lifespanDays = MSSFPMod.settings.IEDLifespanDays;
        if (lifespanDays <= 0) return null;

        int lifespan = lifespanDays * GenDate.TicksPerDay + jitterTicks;
        int remaining = lifespan - (GenTicks.TicksGame - spawnTick);
        if (remaining <= 0) return null;

        return "MSSFP_IEDDecayInspectString".Translate(remaining.ToStringTicksToPeriod());
    }
}
