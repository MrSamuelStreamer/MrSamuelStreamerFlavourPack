using System.Collections.Generic;
using System.Linq;
using MSSFP.Utils;
using UnityEngine;
using Verse;

namespace MSSFP.ResourceGeneratorMod;

public class ResourceGeneratorSettingsTab(ModSettings settings, Mod mod)
    : SettingsTab(settings, mod)
{
    public Settings Settings => settings as Settings;

    public override string TabName => "Resource Generator";
    public override int TabOrder => 5;


    public override void DoTabContents(
        Listing_Standard options,
        Rect scrollViewRect,
        ref float scrollViewHeight
    )
    {
        if (options.ButtonText("MSS_FP_Settings_AddThingDefToResourceGenerator".Translate()))
        {
            Dialog_ThingDefFinder finder = new();
            Find.WindowStack.Add(finder);
        }

        List<ThingDef> toRemove = [];

        foreach (ThingDef extraBuildable in ExtraBuildables.Where(t => t != null).ToList()) // Handle mods removed and such
        {
            scrollViewHeight += 30;
            if (options.ButtonText($"{extraBuildable.defName} | {extraBuildable.LabelCap}"))
            {
                RemoveBuildable(extraBuildable);
            }
        }
    }


    public void RemoveBuildable(ThingDef buildable)
    {
        Settings.ResourceGeneratorExtraBuildables.Remove(buildable.defName);
    }

    public void AddBuildable(ThingDef buildable)
    {
        Settings.ResourceGeneratorExtraBuildables.Add(buildable.defName);
    }

    public List<ThingDef> ExtraBuildables
    {
        get
        {
            Settings.ResourceGeneratorExtraBuildables ??= [];
            return Settings.ResourceGeneratorExtraBuildables
                .Select(s => DefDatabase<ThingDef>.GetNamed(s))
                .Where(d => d is not null)
                .ToList();
        }
    }


    public override void ExposeData()
    {
        // Persistence handled by Settings.ExposeData() so it survives assembly removal.

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            MSSFPResourceGeneratorMod.UpdateExtras();
        }
    }
}
