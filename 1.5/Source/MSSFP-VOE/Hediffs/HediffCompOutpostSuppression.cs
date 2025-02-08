using System.Linq;
using Outposts;
using RimWorld;
using Verse;

namespace MSSFP.VOE.Hediffs;

public class HediffCompOutpostSuppression: HediffComp
{
    public HediffCompProperties_OutpostSuppression Props => (HediffCompProperties_OutpostSuppression) props;

    public override void CompPostTick(ref float severityAdjustment)
    {
        if(Props.OutpostDefs.NullOrEmpty() || parent.pawn is not { Map: null }) return;
        if(!parent.pawn.IsHashIntervalTick(GenDate.TicksPerDay)) return;

        if(!Find.World.worldObjects.AllWorldObjects.Where(wo=>Props.OutpostDefs.Contains(wo.def)).Any(wo=>wo is Outpost o && o.AllPawns.Contains(parent.pawn))) return;

        HediffComp_Immunizable immunizable = parent.GetComp<HediffComp_Immunizable>();
        if (immunizable == null) return;

        if (immunizable.Immunity < 1)
        {
            severityAdjustment -= immunizable.Props.severityPerDayNotImmune * 0.75f;
        }
        else
        {

            severityAdjustment -= immunizable.Props.severityPerDayImmune * 0.75f;
        }

    }

}
