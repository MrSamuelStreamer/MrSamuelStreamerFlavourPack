using System;
using System.Linq;
using System.Reflection;
using AchievementsExpanded;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.Achievements;

public class SignalTracker: TrackerBase, ISignalReceiver
{
    public string Signal;

    public SignalTracker()
    {
        Find.SignalManager.RegisterReceiver(this);
    }


    public SignalTracker(SweetBabyBoyTracker reference) : base(reference)
    {
        Find.SignalManager.RegisterReceiver(this);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref Signal, "Signal");
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

        return true;
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

        AchievementCard card = AchievementPointManager.GetCards<SignalTracker>().FirstOrDefault(card => card.tracker == this);

        if (card != null && Trigger())
        {
            card.UnlockCard();
        }
    }
}
