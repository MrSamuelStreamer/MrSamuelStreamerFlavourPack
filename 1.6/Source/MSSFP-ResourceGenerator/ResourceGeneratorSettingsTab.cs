using System.Collections.Generic;
using System.Linq;
using MSSFP.Utils;
using UnityEngine;
using Verse;

namespace MSSFP.ResourceGeneratorMod;

public class ResourceGeneratorSettingsTab(ModSettings settings, Mod mod) : SettingsTab(settings, mod)
{
    public Settings Settings => settings as Settings;

    public override string TabName => "Resource Generator";
    public override int TabOrder => 5;

    public override void DoTabContents(Listing_Standard options, Rect scrollViewRect, ref float scrollViewHeight)
    {
        if (options.ButtonText("Add ThingDef to resource generator list"))
        {
            Dialog_ThingDefFinder finder = new(Settings);
            Find.WindowStack.Add(finder);
        }

        List<ThingDef> toRemove = [];

        foreach (ThingDef extraBuildable in Settings.ExtraBuildables.Where(t => t != null).ToList()) // Handle mods removed and such
        {
            scrollViewHeight += 30;
            if (options.ButtonText($"{extraBuildable.defName} | {extraBuildable.LabelCap}"))
            {
                Settings.RemoveBuildable(extraBuildable);
            }
        }
    }

    public override void ExposeData()
    {
        // pass through to base settings
        PostSaveActions.Add(Settings.Mod.WriteSettings);
    }
}
