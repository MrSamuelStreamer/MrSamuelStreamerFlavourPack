using System.Collections.Generic;
using System.Linq;
using AchievementsExpanded;
using RimWorld;
using Verse;

namespace MSSFP.Achievements;

public class RaceDefAndSignalTracker : TrackerBase, ISignalReceiver
{
    public string Signal;
    public List<string> SignalArgs;
    public bool Triggered = false;
    public List<ThingDef> RaceDefs;

    public RaceDefAndSignalTracker()
    {
        if (!Triggered)
            Find.SignalManager.RegisterReceiver(this);
    }

    public RaceDefAndSignalTracker(RaceDefAndSignalTracker reference) : base(reference)
    {
        if (!Triggered)
            Find.SignalManager.RegisterReceiver(this);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref Signal, "Signal");
        Scribe_Values.Look(ref Triggered, "Triggered", false);
    }

    public override string Key
    {
        get { return "MSS_RaceDefAndSignalTracker"; }
        set { }
    }

    protected override string[] DebugText => [$"MSS_RaceDefAndSignalTracker: {Signal} + [{string.Join(", ", RaceDefs.Select(def=>def.defName))}]"];

    public override bool Trigger()
    {
        base.Trigger();

        return Triggered && PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction.Any(pawn => RaceDefs.Contains(pawn.def));
    }

    public void Notify_SignalReceived(Signal signal)
    {
        if (signal.tag != Signal)
        {
            return;
        }

        for (int i = 0; i < SignalArgs.Count; i++)
        {
            if (!signal.args.TryGetArg(i, out string arg)) return;
            if(arg != SignalArgs[i]) return;
        }

        if (Current.ProgramState != ProgramState.Playing)
        {
            return;
        }

        Triggered = true;
        Find.SignalManager.DeregisterReceiver(this);

        AchievementCard card = AchievementPointManager.GetCards<SignalTracker>().FirstOrDefault(card => card.tracker == this);

        if (card != null && Trigger())
        {
            card.UnlockCard();
        }
    }
}
