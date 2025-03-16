using MSSFP.Comps.Game;
using RimWorld;
using Verse;

namespace MSSFP.Questing;

public class QuestPart_DisableLoversRetreat : QuestPart
{
    public string inSignal;

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (signal.tag != inSignal)
            return;

        Current.Game.GetComponent<LoversRetreatGameomponent>().LoversRetreatEnabled = false;
    }
}
