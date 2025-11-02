using UnityEngine;
using Verse;

namespace MSSFP.Utils;

public class MemesSettingsTab(ModSettings settings, Mod mod) : SettingsTab(settings, mod)
{
    public Settings Settings => settings as Settings;
    public override string TabName => "Memes";
    public override int TabOrder => 3;

    public override void DoTabContents(
        Listing_Standard options,
        Rect scrollViewRect,
        ref float scrollViewHeight
    )
    {
        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnableOutpostFission".Translate(),
            ref Settings.EnableOutpostFission,
            ref scrollViewHeight
        );
        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnableLoversRetreat".Translate(),
            ref Settings.EnableLoversRetreat,
            ref scrollViewHeight
        );
        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnableNonsenseIncidents".Translate(),
            ref Settings.EnableNonsenseIncidents,
            ref scrollViewHeight
        );
        DrawCheckBox(
            options,
            "MSS_FP_Settings_SingleUseMentalFuses".Translate(),
            ref Settings.SingleUseMentalFuses,
            ref scrollViewHeight
        );

        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnableTrekBeamers".Translate(),
            ref Settings.EnableTrekBeamers,
            ref scrollViewHeight
        );
        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnableMercenaryHiring".Translate(),
            ref Settings.EnableMercenaryHiring,
            ref scrollViewHeight
        );

        bool disableSkylanternRaids = !Settings.EnableSkylanternRaids;
        DrawCheckBox(
            options,
            "MSS_FP_Settings_NoSkylanternRaids".Translate(),
            ref disableSkylanternRaids,
            ref scrollViewHeight
        );
        Settings.EnableSkylanternRaids = !disableSkylanternRaids;

        DrawIntAdjuster(
            options,
            "MSS_FP_Settings_DaysForOutpostFission".Translate(Settings.DaysForOutpostFission),
            ref Settings.DaysForOutpostFission,
            1,
            1,
            ref scrollViewHeight
        );
        DrawIntAdjuster(
            options,
            "MSS_FP_Settings_DaysForFission".Translate(Settings.DaysForFission),
            ref Settings.DaysForFission,
            1,
            1,
            ref scrollViewHeight
        );
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref Settings.DrawByMrStreamer, "DrawByMrStreamer", false);
        Scribe_Values.Look(ref Settings.EnableOutpostFission, "EnableOutpostFission", false);
        Scribe_Values.Look(ref Settings.EnableLoversRetreat, "EnableLoversRetreat", false);
        Scribe_Values.Look(ref Settings.SingleUseMentalFuses, "SingleUseMentalFuses", false);
        Scribe_Values.Look(ref Settings.EnableNonsenseIncidents, "EnableNonsenseIncidents", false);
        Scribe_Values.Look(ref Settings.EnableTrekBeamers, "EnableTrekBeamers", true);
        Scribe_Values.Look(ref Settings.EnableSkylanternRaids, "EnableSkylanternRaids", false);
        Scribe_Values.Look(ref Settings.EnableMercenaryHiring, "EnableMercenaryHiring", true);
        Scribe_Values.Look(ref Settings.DaysForOutpostFission, "DaysForOutpostFission", 7);
        Scribe_Values.Look(ref Settings.DaysForFission, "DaysForFission", 7);
    }
}
