using System;
using System.Linq;
using VEF.AnimalBehaviours;
using Verse;

namespace MSSFP.VFE;

public class HediffSwitchMapComponent(Map map) : MapComponent(map)
{
    private static HediffDef _cachedReproDef;

    public static HediffDef ReproDef =>
        _cachedReproDef ??= DefDatabase<HediffDef>.AllDefsListForReading
            .FirstOrDefault(g => g.defName == "AG_AsexualFission");

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (Find.TickManager.TicksGame % 600 != 0)
            return;

        HediffDef reproDef = ReproDef;
        if (reproDef == null)
            return;

        foreach (
            Pawn pawn in map.mapPawns.AllHumanlike.Where(p =>
                p.health.hediffSet.HasHediff(reproDef)
            )
        )
        {
            HediffWithComps h = (HediffWithComps)
                pawn.health.hediffSet.GetFirstHediffOfDef(reproDef);
            HediffComp_AsexualReproduction comp =
                h.TryGetComp<HediffComp_AsexualReproduction>();

            if (comp == null || h.TryGetComp<HediffComp_TweakedAsexualReproduction>() != null)
                continue;

            HediffComp_TweakedAsexualReproduction newComp =
                (HediffComp_TweakedAsexualReproduction)
                    Activator.CreateInstance(typeof(HediffComp_TweakedAsexualReproduction));

            newComp.props = comp.props;
            newComp.asexualFissionCounter = comp.asexualFissionCounter;
            newComp.parent = h;

            h.comps.Add(newComp);
            h.comps.Remove(comp);
        }
    }
}
