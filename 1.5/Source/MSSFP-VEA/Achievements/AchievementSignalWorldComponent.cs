using System.Collections.Generic;
using System.Linq;
using AchievementsExpanded;
using RimWorld.Planet;
using Verse;

namespace MSSFP.VAE.Achievements;

public class AchievementSignalWorldComponent(World world) : WorldComponent(world)
{
    public override void FinalizeInit()
    {
        IEnumerable<RaceDefAndSignalTracker> trackers = AchievementPointManager.GetCards<RaceDefAndSignalTracker>().Select(card => card.def.tracker as RaceDefAndSignalTracker);

        foreach (RaceDefAndSignalTracker tracker in trackers)
        {
            Find.SignalManager.RegisterReceiver(tracker);
        }
    }
}
