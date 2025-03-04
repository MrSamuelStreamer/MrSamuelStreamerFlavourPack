using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace MSSFP.Questing;

public class QuestNode_Perganante: QuestNode
{
    [NoTranslate]
    public SlateRef<string> inSignal;
    public SlateRef<MapParent> mapParent;

    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        QuestGen.quest.AddPart(new QuestPart_Perganante
        {
            inSignal = (QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal")),
            mapParent = mapParent.GetValue(slate),
        });
    }
}
