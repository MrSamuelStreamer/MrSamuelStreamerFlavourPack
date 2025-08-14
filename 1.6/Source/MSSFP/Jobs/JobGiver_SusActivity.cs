using System.Linq;
using MSSFP.Comps;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Jobs;

public class JobGiver_SusActivity : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        CompImpostor compImpostor = pawn.GetComp<CompImpostor>();
        if (compImpostor is not { IsSus: true })
            return null;

        float rand = Rand.Value;
        // Random selection of sus activities
        if (rand < 0.33f)
        {
            // Try to start a fire
            Building_Storage stockpile = pawn
                .Map?.listerBuildings.AllBuildingsColonistOfClass<Building_Storage>()
                .RandomElementWithFallback();
            if (stockpile != null)
            {
                return JobMaker.MakeJob(JobDefOf.Ignite, stockpile);
            }
        }
        else if (rand < 0.66f)
        {
            // Try to attack a colonist or animal
            Pawn target = pawn
                .Map?.mapPawns.AllPawnsSpawned.Where(p =>
                    p != pawn && (p.RaceProps.Animal || p.IsColonist)
                )
                .RandomElementWithFallback();
            if (target != null)
            {
                return JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            }
        }
        else
        {
            // Try to destroy unfinished things
            UnfinishedThing unfinishedThing = pawn
                .Map?.listerThings.GetThingsOfType<UnfinishedThing>()
                .RandomElementWithFallback();
            if (unfinishedThing != null)
            {
                return JobMaker.MakeJob(JobDefOf.Ignite, unfinishedThing);
            }
        }

        return null;
    }
}
