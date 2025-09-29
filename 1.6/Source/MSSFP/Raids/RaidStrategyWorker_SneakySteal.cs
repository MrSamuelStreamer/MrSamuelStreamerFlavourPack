using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace MSSFP.Raids;

public class RaidStrategyWorker_SneakySteal: RaidStrategyWorker_ImmediateAttack
{
    protected override LordJob MakeLordJob(
        IncidentParms parms,
        Map map,
        List<Pawn> pawns,
        int raidSeed)
    {
        Thing thing = map.listerThings.AllThings.Where(t => t.Faction?.IsPlayer ?? false).RandomElementByWeightWithFallback<Thing>((thing => thing.MarketValue), null);

        return new LordJob_SneakySteal(parms.faction, thing);
    }
}
