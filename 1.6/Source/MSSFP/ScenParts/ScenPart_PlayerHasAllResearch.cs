using RimWorld;
using Verse;

namespace MSSFP.ScenParts;

public class ScenPart_PlayerHasAllResearch:  ScenPart
{
    public override void PostGameStart()
    {
        Find.ResearchManager.DebugSetAllProjectsFinished();

        foreach (ResearchProjectDef allDef in DefDatabase<ResearchProjectDef>.AllDefs)
        {
            Find.ResearchManager.FinishProject(allDef, doCompletionLetter: false);
        }
    }
}
