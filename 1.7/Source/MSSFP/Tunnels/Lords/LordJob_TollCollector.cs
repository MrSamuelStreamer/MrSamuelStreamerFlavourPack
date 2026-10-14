using Verse;
using Verse.AI.Group;
using RimWorld;

namespace MSSFP.Tunnels.Lords;

/// <summary>
/// Custom lord job for the Toll Collector encounter.
/// Two-toil state graph:
///   toil_standoff (DefendPoint at spawn position)
///     → "MSSFP_TollRefused" → toil_assault (AssaultColony)
///
/// Payment is handled outside the lord state machine: MapComponent_TollCollectorDialog
/// calls DeSpawn() directly on acceptance. LordToil_ExitMap is NOT used because
/// tunnel encounter maps are enclosed caves with no map edges to path to.
/// </summary>
public class LordJob_TollCollector : LordJob
{
    public const string MemoTollRefused = "MSSFP_TollRefused";

    private IntVec3 _standPos;

    // Parameterless constructor required for ExposeData / save-load.
    public LordJob_TollCollector() { }

    public LordJob_TollCollector(IntVec3 standPos)
    {
        _standPos = standPos;
    }

    public override StateGraph CreateGraph()
    {
        StateGraph graph = new StateGraph();

        LordToil_DefendPoint toil_standoff = new LordToil_DefendPoint(_standPos);
        LordToil_AssaultColony toil_assault = new LordToil_AssaultColony();

        graph.AddToil(toil_standoff);
        graph.AddToil(toil_assault);
        graph.StartingToil = toil_standoff;

        Transition refused = new Transition(toil_standoff, toil_assault);
        refused.AddTrigger(new Trigger_Memo(MemoTollRefused));
        graph.AddTransition(refused);

        return graph;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _standPos, "standPos");
    }
}
