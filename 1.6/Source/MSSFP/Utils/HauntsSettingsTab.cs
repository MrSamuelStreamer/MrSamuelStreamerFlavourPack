using UnityEngine;
using Verse;

namespace MSSFP.Utils;

public class HauntSettingsTab(ModSettings settings, Mod mod) : SettingsTab(settings, mod)
{
    public Settings Settings => settings as Settings;
    public override string TabName => "Haunts";
    public override int TabOrder => 1;

    public override void DoTabContents(Listing_Standard options, Rect scrollViewRect, ref float scrollViewHeight)
    {
        DrawCheckBox(options, "MSS_FP_Settings_ShowHaunts".Translate(), ref Settings.ShowHaunts, ref scrollViewHeight);
        DrawCheckBox(options, "MSS_FP_Settings_EnablePossession".Translate(), ref Settings.EnablePossession, ref scrollViewHeight);
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref Settings.ShowHaunts, "ShowHaunts", false);
        Scribe_Values.Look(ref Settings.EnablePossession, "DisablePossession", false);
    }
}
