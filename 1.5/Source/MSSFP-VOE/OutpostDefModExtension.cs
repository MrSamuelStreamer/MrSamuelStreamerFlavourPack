using System.Collections.Generic;
using Verse;

namespace MSSFP.VOE;

public class OutpostDefModExtension : DefModExtension
{
    public class ThingDefWithWeight : IExposable
    {
        public ThingDef thingDef;
        public float weight;
        public List<ThingCategoryDef> stuffCategories;
        public ThingDef stuff;
        public bool minified = false;
        public int stackLimit = -1;

        public ThingDefWithWeight() { }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref thingDef, "thingDef");
            Scribe_Values.Look(ref weight, "weight");
            Scribe_Collections.Look(ref stuffCategories, "stuffCategories", LookMode.Def);
            Scribe_Defs.Look(ref stuff, "stuff");
            Scribe_Values.Look(ref minified, "minified", false);
            Scribe_Values.Look(ref stackLimit, "stackLimit", -1);
        }
    }

    public List<ThingDefWithWeight> thingDefs;
    public IntRange valuePerYearOld = new IntRange(10, 100);
}
