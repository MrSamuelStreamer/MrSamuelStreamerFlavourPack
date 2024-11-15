using UnityEngine;
using Verse;

namespace MSSFP;

public class Settings : ModSettings
{
    public bool destroyFloors = true;

    public void DoWindowContents(Rect wrect)
    {
        Listing_Standard options = new();
        options.Begin(wrect);

        options.CheckboxLabeled("MSS_Mabel_Settings_DestroyFloors".Translate(), ref destroyFloors);
        options.Gap();

        options.End();
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref destroyFloors, "destroyFloors", true);
    }
}
