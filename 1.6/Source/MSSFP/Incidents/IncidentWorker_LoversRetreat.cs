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

    /// <summary>
    /// Candidate colonists eligible for a lovers' retreat on the given map: free adult
    /// colonists, not downed, not pregnant (unless allowAnyPregnant), each paired with the
    /// spouses that are themselves eligible (on the same map, not downed, not pregnant, and
    /// able to produce a child unless allowAnyPregnant). Shared by CanFireNowSub and GetPawn
    /// so both use identical filters.
    /// </summary>
    public virtual IEnumerable<(Pawn pawn, List<Pawn> spouses)> GetEligibleCouples(Map map)
    {
        bool allowAnyPregnant = MSSFPMod.settings.allowAnyPregnant;

        foreach (Pawn pawn in map.mapPawns.FreeAdultColonistsSpawned.Where(pawn => !pawn.Downed))
        {
            if (pawn.health.hediffSet.HasHediff(HediffDefOf.PregnantHuman))
                continue;

            List<Pawn> eligibleSpouses = pawn
                .GetSpouses(false)
                .Where(spouse =>
                    spouse.Map == map
                    && !spouse.Downed
                    && !spouse.health.hediffSet.HasHediff(HediffDefOf.PregnantHuman)
                    && (allowAnyPregnant || PregnancyUtility.CanEverProduceChild(pawn, spouse).Accepted)
                )
                .ToList();

            if (eligibleSpouses.Count > 0)
                yield return (pawn, eligibleSpouses);
        }
    }

    public virtual bool ColonyHasRomanticCoupleAvailable(IncidentParms parms)
    {
        if (parms.target is not Map map)
        {
            return false;
        }

        return GetEligibleCouples(map).Any();
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

    public virtual (Pawn pawn, Pawn spouse) GetPawnAndSpouse(IncidentParms parms)
    {
        if (parms.target is not Map map)
            return (null, null);

        List<(Pawn pawn, List<Pawn> spouses)> couples = GetEligibleCouples(map).ToList();
        if (couples.Count == 0)
            return (null, null);

        (Pawn pawn, List<Pawn> spouses) couple = couples.RandomElement();
        return (couple.pawn, couple.spouses.RandomElement());
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        (Pawn pawn, Pawn spouse) = GetPawnAndSpouse(parms);
        if (pawn is null || spouse is null)
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
