using UnityEngine;
using Verse;

namespace MSSFP.Utils;

public class MainSettingsTab(ModSettings settings, Mod mod) : SettingsTab(settings, mod)
{
    public Settings Settings => settings as Settings;
    public override string TabName => "Main";
    public override bool IsDefault => true;
    public override int TabOrder => 0;

    public override void DoTabContents(Listing_Standard options, Rect scrollViewRect, ref float scrollViewHeight)
    {
        DrawCheckBox(options, "MSS_FP_Settings_OverrideRelicPool".Translate(), ref Settings.OverrideRelicPool, ref scrollViewHeight);
        DrawCheckBox(options, "MSS_FP_Settings_EnableWanderDelayModification".Translate(), ref Settings.EnableWanderDelayModification, ref scrollViewHeight);

        if (Settings.EnableWanderDelayModification)
        {
            float wanderDelaySeconds = Settings.WanderDelayTicks / 60f;

            wanderDelaySeconds = options.SliderLabeled(
                "MSS_FP_Settings_WanderDelaySeconds".Translate((wanderDelaySeconds).ToString("F1")),
                wanderDelaySeconds,
                -2f,
                200f,
                tooltip: "MSS_FP_Settings_WanderDelaySeconds_Tooltip"
            );

            Settings.WanderDelayTicks = Mathf.RoundToInt(wanderDelaySeconds * 60f);
            scrollViewHeight += 30f;
        }
    }

    public override void ExposeData()
    {
        if (Settings == null)
            return;
        Scribe_Values.Look(ref Settings.OverrideRelicPool, "overrideRelicPool", false);
        Scribe_Values.Look(ref Settings.DrawByMrStreamer, "DrawByMrStreamer", false);
        Scribe_Values.Look(ref Settings.EnableWanderDelayModification, "EnableWanderDelayModification", false);
        Scribe_Values.Look(ref Settings.WanderDelayTicks, "WanderDelayTicks", 0);
    }
}
