using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP;

public class FroggeLeapResearchComponent(World world) : WorldComponent(world)
{
    public bool EventHasFired = false;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look<bool>(ref EventHasFired, "EventHasFired", false);
    }

    public override void WorldComponentTick()
    {
        if(EventHasFired) return;
        if(Find.FactionManager.OfPlayer.def.techLevel < TechLevel.Industrial) return;
        if (Find.TickManager.TicksGame % GenDate.TicksPerDay == GenDate.TicksPerDay/2)
        {
            IncidentParms iParams = new IncidentParms { target = Find.CurrentMap, forced = true};
            EventHasFired = Find.Storyteller.TryFire(new FiringIncident(MSSFPDefOf.MSS_FroggeResearch, null, iParams), false);
        }
    }
}
