using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Genes;

public class Gene_GrassToucher: Gene
{
    // an hour spent surrounded by plants should make severity 1
    public const float SeverityGain = 1f / (GenDate.TicksPerHour * 9f);

    public override void Tick()
    {
        if(pawn.Map == null) return;
        if(!pawn.IsHashIntervalTick(60)) return;

        Hediff hediff = pawn.health.GetOrAddHediff(MSSFPDefOf.MSSFP_TouchedGrass);
        foreach (Plant plant in GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, 6, true).OfType<Plant>())
        {
            hediff.Severity += SeverityGain;
        }
    }

    public override void PostRemove()
    {
        Hediff firstHediffOfDef = pawn.health.hediffSet.GetFirstHediffOfDef(MSSFPDefOf.MSSFP_TouchedGrass);
        if (firstHediffOfDef != null)
            this.pawn.health.RemoveHediff(firstHediffOfDef);
        base.PostRemove();
    }
}
