using Verse;
using Verse.AI.Group;

namespace MSSFP.Tunnels.Lords;

/// <summary>
/// Two-phase lord job for the BrendaKillsBarbas encounter.
///
/// Phase 1 — LordToil_BrendaKillBarbas:    Brenda is forced to attack and execute Barbas.
/// Phase 2 — LordToil_BrendaAssaultCaravan: After Barbas dies, Brenda turns on the caravan.
///
/// The transition fires when Barbas is dead or despawned, checked every 60 ticks.
/// </summary>
public class LordJob_BrendaKillsBarbas : LordJob
{
    private Pawn _barbas;

    // Required for save/load
    public LordJob_BrendaKillsBarbas() { }

    public LordJob_BrendaKillsBarbas(Pawn barbas)
    {
        _barbas = barbas;
    }

    public override bool LostImportantReferenceDuringLoading => _barbas == null;

    public override StateGraph CreateGraph()
    {
        StateGraph graph = new StateGraph();

        LordToil_BrendaKillBarbas killToil = new LordToil_BrendaKillBarbas(_barbas);
        LordToil_BrendaAssaultCaravan assaultToil = new LordToil_BrendaAssaultCaravan();

        graph.AddToil(killToil);
        graph.AddToil(assaultToil);
        graph.StartingToil = killToil;

        Transition transition = new Transition(killToil, assaultToil);
        transition.AddTrigger(new Trigger_TickCondition(
            () => _barbas == null || _barbas.Dead || !_barbas.Spawned,
            checkEveryTicks: 60));
        graph.AddTransition(transition);

        return graph;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref _barbas, "barbas");
    }
}
