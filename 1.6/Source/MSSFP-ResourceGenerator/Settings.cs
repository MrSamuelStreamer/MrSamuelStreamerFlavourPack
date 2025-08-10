using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MSSFP.ResourceGeneratorMod;

public class Settings : ModSettings
{
    public float ScrollViewHeight = 0;
    public Vector2 scrollPosition = Vector2.zero;

    public void DoWindowContents(Rect wrect)
    {
        Rect viewRect = new(0, 0, wrect.width - 16, ScrollViewHeight);
        ScrollViewHeight = 0;
        scrollPosition = GUI.BeginScrollView(new Rect(0, 40, wrect.width, wrect.height), scrollPosition, viewRect);

        Listing_Standard options = new();
        options.Begin(viewRect);

        try
        {
            ScrollViewHeight += 30;
            if (options.ButtonText("Add ThingDef to resource generator list"))
            {
                Dialog_ThingDefFinder finder = new();
                Find.WindowStack.Add(finder);
            }

            List<ThingDef> toRemove = [];

            foreach (ThingDef extraBuildable in ExtraBuildables.Where(t => t != null)) // Handle mods removed and such
            {
                ScrollViewHeight += 30;
                if (options.ButtonText($"{extraBuildable.defName} | {extraBuildable.LabelCap}"))
                {
                    toRemove.Add(extraBuildable);
                }
            }

            toRemove.ForEach(t => ExtraBuildables.Remove(t));
        }
        finally
        {
            GUI.EndScrollView();
            options.End();
        }
    }

    protected List<string> strings = [];

    public List<ThingDef> ExtraBuildables
    {
        get { return strings.Select(s => DefDatabase<ThingDef>.GetNamed(s)).Where(d => d is not null).ToList(); }
        set { strings = value.Where(d => d is not null).Select(d => d.defName).ToList(); }
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
