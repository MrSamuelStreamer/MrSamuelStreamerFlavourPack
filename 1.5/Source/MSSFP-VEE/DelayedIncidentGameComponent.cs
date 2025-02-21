using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.VEE;

public class DelayedIncidentGameComponent : GameComponent
{
    public bool HaveDoneNuclearUltratech = false;

    public Game Game;
    public DelayedIncidentGameComponent(){}

    public DelayedIncidentGameComponent(Game game)
    {
        Game = game;
    }
    public Dictionary<FiringIncident, int> DelayedIncidentDefs = new Dictionary<FiringIncident, int>();

    public void AddNewDelayedIncidentDef(IncidentDef delayedIncidentDef, Map map, int delay)
    {
        IncidentParms iParams = new IncidentParms { target = map==null ? Find.World : map, forced = true};
        FiringIncident fi = new FiringIncident(delayedIncidentDef, null, iParams);
        DelayedIncidentDefs[fi] = Find.TickManager.TicksGame + delay;
    }


    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref DelayedIncidentDefs, "DelayedIncidentDefs", LookMode.Deep, LookMode.Value);
    }

    public override void GameComponentTick()
    {
        base.GameComponentTick();
        List<FiringIncident> ToDelayFurther = [];
        List<FiringIncident> ToRemove = [];

        try
        {
            foreach (KeyValuePair<FiringIncident, int> delayedIncidentDef in DelayedIncidentDefs)
            {
                if (delayedIncidentDef.Value <= Find.TickManager.TicksGame)
                {
                    if (delayedIncidentDef.Key.def.Worker.TryExecute(delayedIncidentDef.Key.parms))
                    {
                        delayedIncidentDef.Key.parms.target.StoryState.Notify_IncidentFired(delayedIncidentDef.Key);
                        ToRemove.Add(delayedIncidentDef.Key);
                    }
                    else
                    {
                        ToDelayFurther.Add(delayedIncidentDef.Key);
                    }
                }
            }
        }
        finally
        {
            foreach (FiringIncident firingIncident in ToDelayFurther)
            {
                DelayedIncidentDefs[firingIncident] += GenDate.TicksPerDay;
            }

            foreach (FiringIncident firingIncident in ToRemove)
            {
                DelayedIncidentDefs.Remove(firingIncident);
            }
        }

        if (!HaveDoneNuclearUltratech && Find.FactionManager.OfPlayer.def.techLevel > TechLevel.Spacer)
        {
            HaveDoneNuclearUltratech = true;
            MSSFVEEPDefOf.MSS_NuclearFallout.baseChance = 20;
            MSSFVEEPDefOf.MSS_NuclearFallout.minRefireDays = 15;
            MSSFVEEPDefOf.MSS_NuclearFallout.earliestDay = 0;
            MSSFVEEPDefOf.MSS_NuclearFallout.letterText = """
                                                          The ever increasing use of nuclear weapons across the planet has polluted the atmosphere with radioactive dust.

                                                          A storm has caused a dense pocket of radioactive dust to mix in with itself, permeating the rain.

                                                          It will last anywhere between a few hours to several days.
                                                          """;

        }
    }
}
