using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.VME;

public class WorldComponent_BloodCourtDuelCooldowns : WorldComponent
{
    public static WorldComponent_BloodCourtDuelCooldowns Instance;

    private int ritualCooldownExpires;

    public WorldComponent_BloodCourtDuelCooldowns(World world)
        : base(world)
    {
        Instance = this;
    }

    public void StartCooldown()
    {
        int days = Mathf.Clamp(MSSFPMod.settings?.BloodCourtDuelCooldownDays ?? 15, 1, 60);
        ritualCooldownExpires = Find.TickManager.TicksGame + days * GenDate.TicksPerDay;
    }

    public bool IsOnCooldown()
    {
        if (ritualCooldownExpires <= 0)
            return false;
        if (Find.TickManager.TicksGame >= ritualCooldownExpires)
        {
            ritualCooldownExpires = 0;
            return false;
        }

        return true;
    }

    public int DaysRemaining()
    {
        if (!IsOnCooldown())
            return 0;
        int ticksLeft = ritualCooldownExpires - Find.TickManager.TicksGame;
        return Mathf.Max(1, Mathf.CeilToInt(ticksLeft / (float)GenDate.TicksPerDay));
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ritualCooldownExpires, "bloodCourtRitualCooldownExpires", 0);
    }
}
