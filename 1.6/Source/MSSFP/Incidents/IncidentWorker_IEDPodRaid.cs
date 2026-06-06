using System.Linq;
using MSSFP.Things;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Incidents;

/// <summary>
///     Hostile drop pods land scattered across the map. Each pod's cargo is a
///     <see cref="Thing_IEDDeployer" /> that scatters vanilla IED traps in a
///     radius around its landing cell.
/// </summary>
public class IncidentWorker_IEDPodRaid : IncidentWorker
{
    private const int MinPods = 5;
    private const float PointsPerPod = 100f;

    private const float BaseRadius = 8f;
    private const float MaxRadius = 13f;
    private const float PointsPerRadius = 800f;

    private const int MinTrapsPerPod = 4;
    private const int MaxTrapsPerPodExclusive = 9; // Rand.Range upper bound is exclusive for ints

    private const int MinOpenDelayTicks = 60;
    private const int MaxOpenDelayTicksInclusive = 480;

    private const int LandingRerollAttempts = 5;
    private const int MinValidScatterCellsRequired = 3;

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (!base.CanFireNowSub(parms)) return false;
        if (!MSSFPMod.settings.EnableIEDPodRaids) return false;
        if (parms.target is not Map map) return false;
        if (Find.FactionManager.RandomEnemyFaction(allowHidden: false, allowDefeated: false, allowNonHumanlike: true) == null)
        {
            return false;
        }
        return true;
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (parms.target is not Map map) return false;

        Faction hostile = parms.faction
            ?? Find.FactionManager.RandomEnemyFaction(allowHidden: false, allowDefeated: false, allowNonHumanlike: true);
        if (hostile == null) return false;

        int pods = Mathf.Max((int)(parms.points / PointsPerPod), MinPods);
        float radius = Mathf.Min(BaseRadius + parms.points / PointsPerRadius, MaxRadius);

        int podsLanded = 0;
        for (int i = 0; i < pods; i++)
        {
            if (!TryFindAcceptableLandingCell(map, radius, out IntVec3 cell))
            {
                continue;
            }

            ThingDef trapDef = WeightedTrapPick();
            if (trapDef == null) continue;

            Thing_IEDDeployer deployer = (Thing_IEDDeployer)ThingMaker.MakeThing(MSSFPDefOf.MSSFP_IEDPodDeployer);
            deployer.trapCount = Rand.Range(MinTrapsPerPod, MaxTrapsPerPodExclusive);
            deployer.radius = radius;
            deployer.trapDef = trapDef;
            deployer.trapFaction = hostile;

            ActiveTransporterInfo info = new ActiveTransporterInfo();
            info.innerContainer.TryAdd(deployer);
            // Vary openDelay (Scribed by vanilla ActiveTransporterInfo) instead of
            // staggering fire ticks. All skyfallers fall on the same tick for
            // visual drama; their OPEN times scatter across ~8s.
            info.openDelay = Rand.RangeInclusive(MinOpenDelayTicks, MaxOpenDelayTicksInclusive);
            info.leaveSlag = false;
            DropPodUtility.MakeDropPodAt(cell, map, info, hostile);
            podsLanded++;
        }

        if (podsLanded == 0) return false;

        SendStandardLetter(
            "MSSFP_IEDPodRaidLetterLabel".Translate(),
            "MSSFP_IEDPodRaidLetterText".Translate(hostile.NameColored),
            LetterDefOf.ThreatBig,
            parms,
            LookTargets.Invalid
        );
        return true;
    }

    /// <summary>
    ///     Pick a landing cell where the scatter radius will yield a meaningful
    ///     number of valid trap cells. Rejects edge-of-map and mountain-interior
    ///     spots that would silently produce a dud raid.
    /// </summary>
    private bool TryFindAcceptableLandingCell(Map map, float radius, out IntVec3 cell)
    {
        int radiusInt = Mathf.CeilToInt(radius);
        for (int attempt = 0; attempt < LandingRerollAttempts; attempt++)
        {
            IntVec3 candidate = DropCellFinder.RandomDropSpot(map);
            if (!candidate.IsValid) continue;
            if (candidate.DistanceToEdge(map) < radiusInt) continue;

            int validCells = GenRadial.RadialCellsAround(candidate, radius, useCenter: true)
                .Count(c => c.InBounds(map)
                            && c.Standable(map)
                            && c.GetEdifice(map) == null
                            && !c.GetTerrain(map).IsWater);
            if (validCells >= MinValidScatterCellsRequired)
            {
                cell = candidate;
                return true;
            }
        }

        cell = IntVec3.Invalid;
        return false;
    }

    private static ThingDef WeightedTrapPick()
    {
        // 70% high-explosive, 30% incendiary. v2 will add antigrain (point-gated).
        // MSSFP-owned variants subclass Building_TrapExplosive with Building_IEDTrap
        // so claim is gated behind an Intellectual disarm job rather than freebie
        // designate-and-deconstruct.
        if (Rand.Value < 0.7f)
        {
            return MSSFPDefOf.MSSFP_TrapIED_HighExplosive;
        }
        return MSSFPDefOf.MSSFP_TrapIED_Incendiary;
    }
}
