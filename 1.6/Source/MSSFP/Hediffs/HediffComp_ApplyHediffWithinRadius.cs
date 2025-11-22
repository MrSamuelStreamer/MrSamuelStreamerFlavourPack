using System.Linq;
using Verse;

namespace MSSFP.Hediffs;

public class HediffComp_ApplyHediffWithinRadius: HediffComp
{
    public HediffCompProperties_ApplyHediffWithinRadius Props => (HediffCompProperties_ApplyHediffWithinRadius)props;

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);

        if (parent.pawn.IsHashIntervalTick(Props.ticksBetweenChecks))
        {
            foreach (Pawn pawn in GenRadial.RadialDistinctThingsAround(parent.pawn.Position, parent.pawn.Map, Props.proximityRadius, true).OfType<Pawn>().Except([parent.pawn]).Where(CheckPawn))
            {
                if (!pawn.health.hediffSet.TryGetHediff(Props.hediffDef, out Hediff hediff))
                {
                    hediff = pawn.health.AddHediff(Props.hediffDef);
                    hediff.Severity = Props.initialSeverity;
                }
            }
        }
    }

    public bool CheckPawn(Pawn pawn)
    {
        bool canTarget = false;

        if (Props.targetsPlayer && (pawn.Faction?.IsPlayer ?? false))
        {
            canTarget = true;
        }
        if (Props.targetsAnimals && pawn.IsAnimal)
        {
            canTarget = true;
        }
        if (Props.targetsHumans && pawn.RaceProps.Humanlike)
        {
            canTarget = true;
        }
        if (Props.targetsSubHumans && pawn.IsSubhuman)
        {
            canTarget = true;
        }
        if (Props.targetsEntities && pawn.RaceProps.IsAnomalyEntity)
        {
            canTarget = true;
        }
        if (Props.targetsMechs && pawn.RaceProps.IsMechanoid)
        {
            canTarget = true;
        }

        return canTarget;
    }
}
