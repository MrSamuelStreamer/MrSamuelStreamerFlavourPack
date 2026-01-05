using RimWorld;
using Verse;

namespace MSSFP.ScenParts;

public class ScenPart_PlayerHasAllResearch:  ScenPart
{
    public override void PostGameStart()
    {
        Find.ResearchManager.DebugSetAllProjectsFinished();
    }
}
