using System.Linq;
using AchievementsExpanded;
using RimWorld;
using Verse;

namespace MSSFP.Achievements;

public class SignalTracker: TrackerBase, ISignalReceiver
{
    public string Signal;
    public bool Triggered = false;

    public SignalTracker()
    {
        if(!Triggered)
            Find.SignalManager.RegisterReceiver(this);
    }

    public SignalTracker(SweetBabyBoyTracker reference) : base(reference)
    {
        if(!Triggered)
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
        get { return "MSS_SignalTracker"; }
        set { }
    }

    protected override string[] DebugText => [$"SignalTracker: {Signal}]"];

    public override bool Trigger()
    {
        base.Trigger();
        return Triggered;
    }

    public void Notify_SignalReceived(Signal signal)
    {
        if (signal.tag != Signal)
        {
            return;
        }

        if (Current.ProgramState != ProgramState.Playing)
        {
            return;
        }

        Find.SignalManager.DeregisterReceiver(this);
        Triggered = true;

        AchievementCard card = AchievementPointManager.GetCards<SignalTracker>().FirstOrDefault(card => card.tracker == this);

        if (card != null && Trigger())
        {
            card.UnlockCard();
        }
    }
}
