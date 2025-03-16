using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace MSSFP.Questing;

public class QuestNode_DisableLoversRetreat : QuestNode
{
    [NoTranslate]
    public SlateRef<string> inSignal;

    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        QuestGen.quest.AddPart(
            new QuestPart_DisableLoversRetreat { inSignal = (QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal")) }
        );
    }
}
