using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Defs;

public class MapwideHediffWorker
{
    public MapwideHediffDef Def;

    public virtual void Initialize(MapwideHediffDef def)
    {
        Def = def;
    }

    public virtual void Apply(Map map)
    {
        if (!Def.excludeWorldObjectDefs.NullOrEmpty() && Def.excludeWorldObjectDefs.Contains(map.Parent.def))
        {
            ModLog.Debug($"Condition excludeWorldObjectDefs failed for {Def.defName} on {map}");
            return;
        }

        if (!Def.onlyWorldObjectDefs.NullOrEmpty() && !Def.onlyWorldObjectDefs.Contains(map.Parent.def))
        {
            ModLog.Debug($"Condition onlyWorldObjectDefs failed for {Def.defName} on {map}");
            return;
        }

        if (map.Parent is Site site)
        {
            if (!Def.excludeSitePartDefs.NullOrEmpty() && site.parts.Any(p => Def.excludeSitePartDefs.Contains(p.def)))
            {
                ModLog.Debug($"Condition excludeSitePartDefs failed for {Def.defName} on {map}");
                return;
            }

            if (!Def.onlySitePartDefs.NullOrEmpty() && !site.parts.Any(p => Def.onlySitePartDefs.Contains(p.def)))
            {
                ModLog.Debug($"Condition onlySitePartDefs failed for {Def.defName} on {map}");
                return;
            }
        }
        else
        {
            if (!Def.onlySitePartDefs.NullOrEmpty())
            {
                ModLog.Debug($"Condition onlySitePartDefs defined but is not a site {Def.defName} on {map}");
                return;
            }
        }

        List<ThingDef> ThingDefsOnMap = map.listerThings.AllThings.Select(t => t.def).ToList();

        if (!Def.notWhenAllThingsOnMap.NullOrEmpty() && Def.notWhenAllThingsOnMap.All(thingDef => ThingDefsOnMap.Contains(thingDef)))
        {
            ModLog.Debug($"Condition notWhenAllThingsOnMap failed for {Def.defName} on {map}");
            return;
        }

        if (!Def.notWhenAnyThingsOnMap.NullOrEmpty() && Def.notWhenAnyThingsOnMap.Any(thingDef => ThingDefsOnMap.Contains(thingDef)))
        {
            ModLog.Debug($"Condition notWhenAllThingsOnMap failed for {Def.defName} on {map}");
            return;
        }

        if (!Def.onlyWhenAllThingsOnMap.NullOrEmpty() && !Def.onlyWhenAllThingsOnMap.All(thingDef => ThingDefsOnMap.Contains(thingDef)))
        {
            ModLog.Debug($"Condition onlyWhenAllThingsOnMap failed for {Def.defName} on {map}");
            return;
        }

        if (!Def.onlyWhenAnyThingsOnMap.NullOrEmpty() && !Def.onlyWhenAnyThingsOnMap.Any(thingDef => ThingDefsOnMap.Contains(thingDef)))
        {
            ModLog.Debug($"Condition notWhenAllThingsOnMap failed for {Def.defName} on {map}");
            return;
        }

        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
        {
            bool canTarget = false;

            if (Def.targetsPlayer && (pawn.Faction?.IsPlayer ?? false))
            {
                canTarget = true;
            }
            if (Def.targetsAnimals && pawn.IsAnimal)
            {
                canTarget = true;
            }
            if (Def.targetsHumans && pawn.RaceProps.Humanlike)
            {
                canTarget = true;
            }
            if (Def.targetsSubHumans && pawn.IsSubhuman)
            {
                canTarget = true;
            }
            if (Def.targetsEntities && pawn.RaceProps.IsAnomalyEntity)
            {
                canTarget = true;
            }
            if (Def.targetsMechs && pawn.RaceProps.IsMechanoid)
            {
                canTarget = true;
            }

            if (canTarget)
            {
                if (!pawn.health.hediffSet.TryGetHediff(Def.hediff, out Hediff hediff))
                {
                    hediff = pawn.health.AddHediff(Def.hediff);
                    hediff.Severity = Def.initialSeverity;
                }
            }
        }
    }


}
