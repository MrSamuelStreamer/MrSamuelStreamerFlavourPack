using UnityEngine;
using Verse;

namespace MSSFP;

public class Settings : ModSettings
{
    public bool destroyFloors = true;
    public bool overrideRelicPool = false;
    public bool disableFroggeNom = false;
    public bool ShowHaunts = true;
    public bool NoSkylanternRaids = false;
    public bool DrawByMrStreamer = false;

    public void DoWindowContents(Rect wrect)
    {
        Listing_Standard options = new();
        options.Begin(wrect);

        options.CheckboxLabeled("MSS_Mabel_Settings_DestroyFloors".Translate(), ref destroyFloors);
        options.Gap();

        options.CheckboxLabeled("MSS_FP_Settings_OverrideRelicPool".Translate(), ref overrideRelicPool);
        options.Gap();

        options.CheckboxLabeled("MSS_FP_Settings_disableFroggeNom".Translate(), ref disableFroggeNom);
        options.Gap();

        options.CheckboxLabeled("MSS_FP_Settings_ShowHaunts".Translate(), ref ShowHaunts);
        options.Gap();

        options.CheckboxLabeled("MSS_FP_Settings_NoSkylanternRaids".Translate(), ref NoSkylanternRaids);
        options.Gap();

        options.CheckboxLabeled("MSS_FP_Settings_DrawByMrStreamer".Translate(), ref DrawByMrStreamer);
        options.Gap();

        options.End();
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref destroyFloors, "destroyFloors", true);
        Scribe_Values.Look(ref overrideRelicPool, "overrideRelicPool", false);
        Scribe_Values.Look(ref disableFroggeNom, "disableFrogge", false);
        Scribe_Values.Look(ref ShowHaunts, "ShowHaunts", true);
        Scribe_Values.Look(ref NoSkylanternRaids, "NoSkylanternRaids", false);
        Scribe_Values.Look(ref DrawByMrStreamer, "DrawByMrStreamer", false);
    }
}
