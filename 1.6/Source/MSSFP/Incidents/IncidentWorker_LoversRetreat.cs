using System;
using System.Collections.Generic;
using System.Linq;
using MSSFP.Comps;
using MSSFP.Comps.Game;
using MSSFP.Comps.Map;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.Incidents;

public class IncidentWorker_LoversRetreat : IncidentWorker
{
    public IntRange TimeAway = new IntRange(GenDate.TicksPerHour * 6, GenDate.TicksPerDay);

    public virtual bool ColonyHasRomanticCoupleAvailable(IncidentParms parms)
    {
        if (parms.target is not Map map)
        {
            return false;
        }

        bool allowAnyPregnant = MSSFPMod.settings.allowAnyPregnant;

        foreach (Pawn pawn in map.mapPawns.FreeAdultColonistsSpawned.Where(pawn => !pawn.Downed))
        {
            if (!pawn.health.hediffSet.HasHediff(HediffDefOf.PregnantHuman))
            {
                foreach (Pawn spouse in pawn.GetSpouses(false))
                {
                    if (
                        !spouse.health.hediffSet.HasHediff(HediffDefOf.PregnantHuman)
                        && spouse.Map == map
                    )
                    {
                        // If following normal rules, check if they can produce a child
                        if (
                            allowAnyPregnant
                            || PregnancyUtility.CanEverProduceChild(pawn, spouse).Accepted
                        )
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    public override float ChanceFactorNow(IIncidentTarget target) => 1f;

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        return base.CanFireNowSub(parms)
            && MSSFPMod.settings.EnableLoversRetreat
            && Current.Game.GetComponent<LoversRetreatGameomponent>().LoversRetreatEnabled
            && ColonyHasRomanticCoupleAvailable(parms)
            && parms.target is Map map
            && !map.mapPawns.AllPawns.Any(p => p.HostileTo(Faction.OfPlayer));
    }

    public virtual Pawn GetPawn(IncidentParms parms)
    {
        return parms.target is not Map map
            ? null
            : map
                .mapPawns.AllPawnsSpawned.Where(pawn =>
                    pawn.ageTracker.Adult
                    && !pawn.Downed
                    && pawn.GetSpouses(false).Any(spouse => spouse.Map == map)
                )
                .RandomElementWithFallback();
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        Pawn pawn = GetPawn(parms);
        Pawn spouse = pawn?.GetSpouses(false).RandomElement();
        if (spouse is null)
            return false;
        if (
            !RCellFinder.TryFindBestExitSpot(pawn, out IntVec3 pawnSpot, TraverseMode.ByPawn, false)
        )
            return false;
        if (
            !RCellFinder.TryFindBestExitSpot(
                spouse,
                out IntVec3 spouseSpot,
                TraverseMode.ByPawn,
                false
            )
        )
            return false;

        LoversRetreatMapComponent comp = pawn.Map.GetComponent<LoversRetreatMapComponent>();

        if (comp is null)
            return false;

        //wander to midpoint, then exit at spot
        if (
            !RCellFinder.TryFindRandomClearCellsNear(
                spouse.Position,
                3,
                spouse.Map,
                out List<IntVec3> cells
            )
        )
            return false;

        Job pawnJob1 = JobMaker.MakeJob(
            MSSFPDefOf.MSSFP_GoToThen,
            cells.RandomElement(),
            GenDate.TicksPerHour * 6,
            true
        );
        pawnJob1.targetB = pawnSpot;
        pawnJob1.targetC = spouse;
        pawn.jobs.StartJob(pawnJob1, JobCondition.InterruptForced);

        Job spouseJob1 = JobMaker.MakeJob(
            MSSFPDefOf.MSSFP_GoToThen,
            spouse.Position,
            GenDate.TicksPerHour * 6,
            true
        );
        spouseJob1.targetB = spouseSpot;
        spouseJob1.targetC = pawn;
        spouse.jobs.StartJob(spouseJob1, JobCondition.InterruptForced);

        int timeAway = TimeAway.RandomInRange;
        comp.AddPair(pawn, spouse, timeAway);

        LookTargets targets = new LookTargets(new List<Pawn>([pawn, spouse]));

        Find.LetterStack.ReceiveLetter(
            "MSSFP_LoversRetreatLabel".Translate(pawn.NameShortColored, spouse.NameShortColored),
            "MSSFP_LoversRetreadText".Translate(
                pawn.NameShortColored,
                spouse.NameShortColored,
                timeAway / GenDate.TicksPerHour
            ),
            LetterDefOf.PositiveEvent,
            targets
        );

        TaleRecorder.RecordTale(MSSFPDefOf.MSSFP_Lovers_Retreat_Tale, pawn, spouse);
        return true;
    }
}
