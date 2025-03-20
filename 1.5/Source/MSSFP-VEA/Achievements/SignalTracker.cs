using System.Collections.Generic;
using System.Linq;
using AchievementsExpanded;
using RimWorld;
using Verse;

namespace MSSFP.VAE.Achievements;

public class SignalTracker : TrackerBase, ISignalReceiver
{
    public string Signal;
    public List<string> SignalArgs;
    public List<string> NamedSignalArgs;
    public bool Triggered = false;

    public SignalTracker()
        : base() { }

    public SignalTracker(SignalTracker reference)
        : base(reference)
    {
        Signal = reference.Signal;
        SignalArgs = reference.SignalArgs;
        NamedSignalArgs = reference.NamedSignalArgs;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref Signal, "Signal");
        Scribe_Values.Look(ref Triggered, "Triggered", false);
        Scribe_Collections.Look(ref SignalArgs, "SignalArgs", LookMode.Value);
    }

    public override string Key
    {
        get { return "MSS_SignalTracker"; }
        set { }
    }

    protected override string[] DebugText => [$"SignalTracker: {Signal}]"];

    public virtual bool ExtraConditions()
    {
        return true;
    }

    public override bool Trigger()
    {
        return Triggered && ExtraConditions();
    }

    public virtual HashSet<AchievementCard> Cards => AchievementPointManager.GetCards<SignalTracker>();

    public void Notify_SignalReceived(Signal signal)
    {
        if (signal.tag != Signal)
            return;
        if (!SignalArgs.NullOrEmpty())
        {
            for (int i = 0; i < SignalArgs.Count; i++)
            {
                if (!signal.args.TryGetArg(i, out string arg))
                {
                    return;
                }

                if (arg != SignalArgs[i])
                {
                    return;
                }
            }
        }

        if (NamedSignalArgs != null)
        {
            foreach (string arg in NamedSignalArgs)
            {
                string[] split = arg.Split(':');
                if (split.Length != 2)
                {
                    ModLog.Error($"Invalid arg format: {arg}");
                    return;
                }

                if (!signal.args.TryGetArg(split[0], out object sigArg))
                {
                    return;
                }

                if (sigArg.ToString() != split[1])
                {
                    return;
                }
            }
        }

        if (Current.ProgramState != ProgramState.Playing)
        {
            return;
        }

        Triggered = true;

        if (Trigger())
        {
            Cards.FirstOrDefault(card => card.def.tracker == this)?.UnlockCard();
        }
    }
}
