using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using Verse;

namespace MSSFP.Comps.Map;

public class DroneController(Verse.Map map) : MapComponent(map)
{
    public List<DroneSwarm> Swarms = [];

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref Swarms, "Swarms", LookMode.Deep);
    }

    public override void MapComponentTick()
    {
        foreach (DroneSwarm swarm in Swarms)
        {
            swarm.Tick();
        }
    }

    [DebugAction(
        "General",
        null,
        false,
        false,
        false,
        false,
        0,
        false,
        actionType = DebugActionType.ToolMap,
        allowedGameStates = AllowedGameStates.PlayingOnMap,
        displayPriority = 1000
    )]
    private static void SpawnDroneSwarm()
    {
        foreach (Pawn pawn in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Pawn>())
        {
            DroneSwarm swarm = new DroneSwarm(200, pawn);
            swarm.InitSwarm();
        }
    }
}
