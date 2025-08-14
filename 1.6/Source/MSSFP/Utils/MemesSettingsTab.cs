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
            "MSS_Mabel_Settings_DestroyFloors".Translate(),
            ref Settings.MabelDestroyFloors,
            ref scrollViewHeight
        );
        DrawCheckBox(
            options,
            "MSS_FP_Settings_disableFroggeNom".Translate(),
            ref Settings.DisableFroggeNom,
            ref scrollViewHeight
        );
        DrawCheckBox(
            options,
            "MSS_FP_Settings_enableMogus".Translate(),
            ref Settings.EnableMogus,
            ref scrollViewHeight
        );
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
            "MSS_FP_Settings_EnableFroggeIncidents".Translate(),
            ref Settings.EnableFroggeIncidents,
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
            "MSS_FP_Settings_EnableDirtJobs".Translate(),
            ref Settings.EnableDirtJobs,
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
            "MSS_FP_Settings_EnableTaffRaids".Translate(),
            ref Settings.EnableTaffRaids,
            ref scrollViewHeight
        );
        DrawCheckBox(
            options,
            "MSS_FP_Settings_EnableOskarianTech".Translate(),
            ref Settings.EnableOskarianTech,
            ref scrollViewHeight
        );

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
        Scribe_Values.Look(ref Settings.MabelDestroyFloors, "destroyFloors", false);
        Scribe_Values.Look(ref Settings.DisableFroggeNom, "disableFrogge", false);
        Scribe_Values.Look(ref Settings.DrawByMrStreamer, "DrawByMrStreamer", false);
        Scribe_Values.Look(ref Settings.EnableOutpostFission, "EnableOutpostFission", false);
        Scribe_Values.Look(ref Settings.EnableLoversRetreat, "EnableLoversRetreat", false);
        Scribe_Values.Look(ref Settings.EnableFroggeIncidents, "EnableFroggeIncidents", false);
        Scribe_Values.Look(ref Settings.SingleUseMentalFuses, "SingleUseMentalFuses", false);
        Scribe_Values.Look(ref Settings.EnableNonsenseIncidents, "DisableNonsenseIncidents", true);
        Scribe_Values.Look(ref Settings.EnableOskarianTech, "EnableOskarianTech", false);
        Scribe_Values.Look(ref Settings.EnableDirtJobs, "EnableDirtJobs", false);
        Scribe_Values.Look(ref Settings.EnableTrekBeamers, "EnableTrekBeamers", true);
        Scribe_Values.Look(ref Settings.EnableTaffRaids, "EnableTaffRaids", true);
        Scribe_Values.Look(ref Settings.DaysForOutpostFission, "DaysForOutpostFission", 7);
        Scribe_Values.Look(ref Settings.DaysForFission, "DaysForFission", 7);
    }
}
