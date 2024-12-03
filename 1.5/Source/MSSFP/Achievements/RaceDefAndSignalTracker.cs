using System.Collections.Generic;
using System.Linq;
using AchievementsExpanded;
using RimWorld;
using Verse;

namespace MSSFP.Achievements;

public class RaceDefAndSignalTracker : SignalTracker
{
    public List<ThingDef> RaceDefs;

    public RaceDefAndSignalTracker() : base()
    {
    }

    public RaceDefAndSignalTracker(RaceDefAndSignalTracker reference) : base(reference)
    {
        RaceDefs = reference.RaceDefs;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref RaceDefs, "RaceDefs", LookMode.Def);
    }

    public override string Key
    {
        get { return "MSS_RaceDefAndSignalTracker"; }
        set { }
    }

    protected override string[] DebugText
    {
        get
        {
            if (RaceDefs.NullOrEmpty())
                return [$"MSS_RaceDefAndSignalTracker: {Signal} + [<empty>]"];;
            return [$"MSS_RaceDefAndSignalTracker: {Signal} + [{string.Join(", ", RaceDefs.Select(def => def.defName))}]"];
        }
    }

    public override bool ExtraConditions()
    {
        if (RaceDefs.NullOrEmpty())
            return false;
        ModLog.Log($"MSS_RaceDefAndSignalTracker: {Signal} + [ExtraConditions]");
        return PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction.Any(pawn => RaceDefs.Contains(pawn.def));
    }

    public override HashSet<AchievementCard> Cards => AchievementPointManager.GetCards<RaceDefAndSignalTracker>();
}
