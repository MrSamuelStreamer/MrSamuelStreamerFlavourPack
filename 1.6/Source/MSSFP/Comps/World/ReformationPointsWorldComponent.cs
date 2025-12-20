using MSSFP.Utils;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Comps.World;

public class ReformationPointsWorldComponent(RimWorld.Planet.World world) : WorldComponent(world), ISignalReceiver
{
    public int NextYearTick = GenDate.TicksPerYear;
    public int TechsCompletedSinceLastGivingPoints = 0;

    public override void WorldComponentTick()
    {
        if(!MSSFPMod.settings.EnableExtraReformationPoints) return;
        base.WorldComponentTick();
        if (Find.TickManager.TicksGame > NextYearTick)
        {
            NextYearTick = Find.TickManager.TicksGame + GenDate.TicksPerYear;
            if(AddPoints(MSSFPMod.settings.AnnualReformationPoints))
                Messages.Message("MSS_NewYear".Translate(MSSFPMod.settings.AnnualReformationPoints), MessageTypeDefOf.PositiveEvent, true);

            int yearsPassed = Find.TickManager.TicksGame / GenDate.TicksPerYear;

            if (yearsPassed % 5 == 0)
            {
                if(AddPoints(yearsPassed))
                    Messages.Message("MSS_YearsPassed".Translate(yearsPassed, yearsPassed), MessageTypeDefOf.PositiveEvent, true);
            }
        }
    }

    public bool AddPoints(int points)
    {
        if(points <= 0) return false;
        if (Find.FactionManager.OfPlayer.ideos.PrimaryIdeo.Fluid)
        {
            if (!Find.FactionManager.OfPlayer.ideos.PrimaryIdeo.development.TryAddDevelopmentPoints(points))
            {
                ModLog.Warn($"Couldn't add reformation points to ideo {Find.FactionManager.OfPlayer.ideos.PrimaryIdeo.name}");
                return false;
            }
        }
        else
        {
            ModLog.Warn("Couldn't find fluid ideo to add development points to.");
            return false;
        }

        return true;
    }

    public override void FinalizeInit(bool fromLoad)
    {
        base.FinalizeInit(fromLoad);
        Find.SignalManager.RegisterReceiver(this);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref NextYearTick, "NextYearTick");
        Scribe_Values.Look(ref TechsCompletedSinceLastGivingPoints, "TechsCompletedSinceLastGivingPoints", 0);
    }

    public void Notify_SignalReceived(Signal signal)
    {
        if(!MSSFPMod.settings.EnableExtraReformationPoints) return;

        switch (signal.tag)
        {
            case Signals.MSS_BabyAddedToFaction:
                if(AddPoints(MSSFPMod.settings.ReformationPointsPerBaby))
                    Messages.Message("MSS_BabyAddedToFaction".Translate(MSSFPMod.settings.ReformationPointsPerBaby), MessageTypeDefOf.PositiveEvent, true);
                break;
            case Signals.MSS_FactionDefeated:
                if(AddPoints(MSSFPMod.settings.ReformationPointsPerDefeatedFaction))
                    Messages.Message("MSS_FactionDefeated".Translate(signal.args.GetArg(0), MSSFPMod.settings.ReformationPointsPerDefeatedFaction), MessageTypeDefOf.PositiveEvent, true);
                break;
            case Signals.ResearchCompleted:
                TechsCompletedSinceLastGivingPoints++;
                if(TechsCompletedSinceLastGivingPoints < MSSFPMod.settings.TechsToGetPoints) break;
                TechsCompletedSinceLastGivingPoints = 0;

                if(AddPoints(MSSFPMod.settings.ReformationPointsForTechs))
                    Messages.Message("MSS_ReformationPointsForTechs".Translate(MSSFPMod.settings.TechsToGetPoints, MSSFPMod.settings.ReformationPointsForTechs), MessageTypeDefOf.PositiveEvent, true);
                break;
            case Signals.MSS_SeasonChanged:
                if(AddPoints(MSSFPMod.settings.ReformationPointsPerSeasonChange))
                    Messages.Message("MSS_SeasonChanged".Translate(MSSFPMod.settings.ReformationPointsPerSeasonChange), MessageTypeDefOf.PositiveEvent, true);
                break;
            default: break;
        }
    }
}
