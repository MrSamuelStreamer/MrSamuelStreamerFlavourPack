using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MSSFP.ResourceGeneratorMod;

public class Settings : ModSettings
{
    public float ScrollViewHeight = 0;
    public Vector2 scrollPosition = Vector2.zero;

    public List<string> strings = [];

    public void RemoveBuildable(ThingDef buildable)
    {
        strings.Remove(buildable.defName);
    }

    public void AddBuildable(ThingDef buildable)
    {
        strings.Add(buildable.defName);
    }

    public List<ThingDef> ExtraBuildables
    {
        get { return strings.Select(s => DefDatabase<ThingDef>.GetNamed(s)).Where(d => d is not null).ToList(); }
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref strings, "ExtraBuildables", LookMode.Value);

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            MSSFPResourceGeneratorMod.UpdateExtras();
        }
    }
}
