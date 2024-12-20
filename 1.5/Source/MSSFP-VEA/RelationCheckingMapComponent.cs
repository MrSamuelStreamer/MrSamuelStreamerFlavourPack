using MSSFP.VAE.Achievements;
using Verse;

namespace MSSFP.VAE;

public class RelationCheckingMapComponent(Map map) : MapComponent(map)
{
    public int NextCheck = 3600;
    public override void MapComponentTick()
    {
        if(Find.TickManager.TicksGame < NextCheck) return;

        NextCheck = Find.TickManager.TicksGame + 3600;

        foreach (Pawn pawn in map.mapPawns.AllHumanlike)
        {
            SweetBabyBoyTracker.CheckPawnRelations(pawn);
        }
    }
}
