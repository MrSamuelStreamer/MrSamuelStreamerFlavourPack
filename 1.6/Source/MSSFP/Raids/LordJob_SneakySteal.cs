using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace MSSFP.Raids;

public class LordJob_SneakySteal : LordJob
{
    private Faction faction;
    private Thing targetThing;

    public LordJob_SneakySteal()
    {
    }

    public LordJob_SneakySteal(Faction faction, Thing targetThing)
    {
        this.faction = faction;
        this.targetThing = targetThing;
    }

    /// <summary>
    /// Creates a state graph for burgling behavior that includes:
    /// - Hunting enemies with fallback location
    /// - Travel behavior between locations
    /// - Normal and urgent map exit behaviors
    /// - Transitions for dangerous temperatures, trapped pawns, and timed exit
    /// The graph manages how raiders move, fight, and escape based on various conditions.
    /// </summary>
    /// <returns>A complete state graph defining raider behavior patterns</returns>
    public override StateGraph CreateGraph()
    {
        StateGraph stateGraph = new();

        // Create escape map toild
        LordToil_ExitMapAndDefendSelf lordToilExitMap = new();
        // Add to the graph
        stateGraph.AddToil(lordToilExitMap);

        LordJob_Steal stealJob = new LordJob_Steal();
        LordToil stealingToil = stateGraph.AttachSubgraph(stealJob.CreateGraph()).StartingToil;

        {
            Transition stealIfCanExit = new(lordToilExitMap, stealingToil);
            stealIfCanExit.AddTrigger(new Trigger_PawnCanReachMapEdge());
            stateGraph.AddTransition(stealIfCanExit);
        }

        // Create urgent exit map behavior
        LordToil_ExitMap ltUrgentExit = new(LocomotionUrgency.Jog, true);
        // Add urgent exit behavior to main graph
        stateGraph.AddToil(ltUrgentExit);

        // Escape on dangerous temperatures
        {
            Transition escapeOnDangerousTemps = new(stealingToil, ltUrgentExit);
            // Ensure exit destination is set
            escapeOnDangerousTemps.AddPreAction(new TransitionAction_EnsureHaveExitDestination());
            // Trigger on dangerous temperatures
            escapeOnDangerousTemps.AddTrigger(new Trigger_PawnExperiencingDangerousTemperatures());
            // End all jobs when transitioning
            escapeOnDangerousTemps.AddPostAction(new TransitionAction_EndAllJobs());
            // Add temperature transition to main graph
            stateGraph.AddTransition(escapeOnDangerousTemps);
        }

        // Create transition for trapped pawns
        {
            Transition transition2 = new(lordToilExitMap, ltUrgentExit);
            // Add travel behaviors as sources
            transition2.AddSources(stealingToil);
            // Add message about trapped visitors leaving
            // Trigger when pawn cannot reach map edge
            transition2.AddTrigger(new Trigger_PawnCannotReachMapEdge());
            // Add trapped transition to main graph
            stateGraph.AddTransition(transition2);
        }

        // If the pawn can reach the map edge, we can do the starting toil
        {
            Transition transition3 = new(ltUrgentExit, stealingToil);
            // Trigger when pawn can reach map edge again
            transition3.AddTrigger(new Trigger_PawnCanReachMapEdge());
            // Ensure exit destination is set
            transition3.AddPreAction(new TransitionAction_EnsureHaveExitDestination());
            // Add escape transition to main graph
            stateGraph.AddTransition(transition3);
        }

        // Create transition for time-based exit
        {
            Transition transition4 = new(lordToilExitMap, stealingToil);
            // Trigger after 25000 ticks
            transition4.AddTrigger(new Trigger_TicksPassed(GenDate.TicksPerHour*24));
            // Ensure exit destination is set
            transition4.AddPreAction(new TransitionAction_EnsureHaveExitDestination());
            // Add time-based transition to main graph
            stateGraph.AddTransition(transition4);
        }

        return stateGraph;
    }

    public override void ExposeData()
    {
        Scribe_References.Look(ref faction, "faction");
        Scribe_Values.Look(ref targetThing, "targetThing");
    }
}
