using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Rituals;

public class RitualObligationTargetWorker_Things : RitualObligationTargetWorker_ThingDef
{
    public RitualObligationTargetWorker_Things() { }

    public RitualObligationTargetWorker_Things(RitualObligationTargetFilterDef def)
        : base(def) { }

    protected override RitualTargetUseReport CanUseTargetInternal(
        TargetInfo target,
        RitualObligation obligation
    )
    {
        if (!base.CanUseTargetInternal(target, obligation).canUse)
            return false;

        Thing thing1 = target.Thing;

        foreach (ThingDef thingDef in def.thingDefs)
        {
            if (thing1.def == thingDef)
                continue;

            List<Thing> forCell = target.Map.listerBuldingOfDefInProximity.GetForCell(
                target.Cell,
                20,
                thingDef
            );
            if (forCell.Count == 0)
                return "MSSFP_RitualObligationTargetWorker_Things_NotFound".Translate(
                    thingDef.label,
                    20,
                    thing1.def.label
                );
        }
        return true;
    }

    public override IEnumerable<string> GetTargetInfos(RitualObligation obligation)
    {
        return def.thingDefs.Select(thingDef => thingDef.label);
    }
}
