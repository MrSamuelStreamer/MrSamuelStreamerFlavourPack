using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Defs;

public class MapwideHediffDef: Def
{
    public HediffDef hediff;
    public bool targetsPlayer = true;
    public bool targetsAnimals = false;
    public bool targetsHumans = false;
    public bool targetsSubHumans = false;
    public bool targetsEntities = false;
    public bool targetsMechs = false;

    public float initialSeverity = 1f;

    public int ticksBetweenChecks = 600;

    public List<WorldObjectDef> excludeWorldObjectDefs;
    public List<WorldObjectDef> onlyWorldObjectDefs;

    public List<SitePartDef> excludeSitePartDefs;
    public List<SitePartDef> onlySitePartDefs;

    public List<ThingDef> onlyWhenAnyThingsOnMap;
    public List<ThingDef> notWhenAnyThingsOnMap;
    public List<ThingDef> onlyWhenAllThingsOnMap;
    public List<ThingDef> notWhenAllThingsOnMap;

    public Type mapwideHediffWorkerClass = typeof(MapwideHediffWorker);

    public MapwideHediffWorker _worker;

    public MapwideHediffWorker Worker
    {
        get
        {
            if (_worker == null)
            {
                _worker = (MapwideHediffWorker)Activator.CreateInstance(mapwideHediffWorkerClass);
                _worker.Initialize( this);
            }

            return _worker;
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string configError in base.ConfigErrors())
        {
            yield return configError;
        }

        if (!(mapwideHediffWorkerClass?.IsAssignableFrom(typeof(MapwideHediffWorker)) ?? false))
        {
            yield return "mapwideHediffWorkerClass must be a subclass of MapwideHediffWorker";
        }

        if(hediff == null) yield return "hediff is null";

        if(!excludeWorldObjectDefs.NullOrEmpty() && !onlyWorldObjectDefs.NullOrEmpty())
        {
            foreach (WorldObjectDef worldObjectDef in excludeWorldObjectDefs.Where(d => onlyWorldObjectDefs.Contains(d)))
            {
                yield return $"WorldObjectDef {worldObjectDef.defName} is both excluded and only";
            }
        }

        if(!excludeSitePartDefs.NullOrEmpty() && !onlySitePartDefs.NullOrEmpty())
        {
            foreach (SitePartDef sitePartDef in excludeSitePartDefs.Where(d => onlySitePartDefs.Contains(d)))
            {
                yield return $"SitePartDef {sitePartDef.defName} is both excluded and only";
            }
        }
    }
}
