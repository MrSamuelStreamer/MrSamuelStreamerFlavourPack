using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using UnityEngine;
using Verse;

namespace MSSFP.ResourceGeneratorMod;

public class Dialog_ThingDefFinder(Settings settings) : Dialog_OptionLister
{
    public Settings settings = settings;

    public List<ThingDef> Things = DefDatabase<ThingDef>
        .AllDefsListForReading.Where(def => def.recipeMaker != null || !def.costList.NullOrEmpty() && def.costStuffCount > 0)
        .ToList();

    public void NewColumn(float columnWidth)
    {
        curY = 0.0f;
        curX += columnWidth + 17f;
    }

    protected void NewColumnIfNeeded(float columnWidth, float neededHeight)
    {
        if (curY + (double)neededHeight <= windowRect.height)
            return;
        NewColumn(columnWidth);
    }

    protected override void DoListingItems(Rect inRect, float columnWidth)
    {
        foreach (ThingDef thingDef in Things.Where(def => def.defName.Contains(filter) || def.label.Contains(filter)).Except(settings.ExtraBuildables))
        {
            Text.Font = GameFont.Tiny;
            NewColumnIfNeeded(columnWidth, 22f);
            Rect rect = new(curX, curY, columnWidth, 22f);
            TooltipHandler.TipRegion(rect, thingDef.description);
            if (DevGUI.ButtonText(rect, $"Add {thingDef.LabelCap}"))
            {
                settings.AddBuildable(thingDef);
            }

            curY += 22f + verticalSpacing;
            totalOptionsHeight += 22f + verticalSpacing;
        }
    }
}
