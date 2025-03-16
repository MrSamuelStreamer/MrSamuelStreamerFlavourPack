using System.Collections.Generic;
using System.Linq;
using AchievementsExpanded;
using RimWorld;
using Verse;

namespace MSSFP.VAE.Achievements;

public class PawnKindAndSignalTracker : SignalTracker
{
    public List<PawnKindDef> PawnKindDefs;

    public PawnKindAndSignalTracker()
        : base() { }

    public PawnKindAndSignalTracker(PawnKindAndSignalTracker reference)
        : base(reference)
    {
        PawnKindDefs = reference.PawnKindDefs;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref PawnKindDefs, "RaceDefs", LookMode.Def);
    }

    public override string Key
    {
        get { return "MSS_PawnKindAndSignalTracker"; }
        set { }
    }

    protected override string[] DebugText
    {
        get
        {
            if (PawnKindDefs.NullOrEmpty())
                return [$"PawnMSS_PawnKindAndSignalTrackerAndSignalTracker: {Signal} + [<empty>]"];
            return [$"PawnMSS_PawnKindAndSignalTrackerAndSignalTracker: {Signal} + [{string.Join(", ", PawnKindDefs.Select(def => def.defName))}]"];
        }
    }

    public override bool ExtraConditions()
    {
        if (PawnKindDefs.NullOrEmpty())
            return false;
        return PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction.Any(pawn => PawnKindDefs.Contains(pawn.kindDef));
    }

    public override HashSet<AchievementCard> Cards => AchievementPointManager.GetCards<PawnKindAndSignalTracker>();
}
