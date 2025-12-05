using MSSFP.Utils;
using UnityEngine;
using Verse;

namespace MSSFP.GeneratorMod;

public class GeneratorSettingsTab(ModSettings settings, Mod mod) : SettingsTab(settings, mod)
{
    public Settings Settings => settings as Settings;

    public override string TabName => "Generator Mod";
    public override int TabOrder => 6;

    public bool EnableFasterUpgrades = false;

    public override void DoTabContents(
        Listing_Standard options,
        Rect scrollViewRect,
        ref float scrollViewHeight
    )
    {
        DrawCheckBox(
            options,
            "Speed up Genetron upgrades by 4x",
            ref EnableFasterUpgrades,
            ref scrollViewHeight
        );

        options.Gap(10f);
        scrollViewHeight += 10f;

        Text.Font = GameFont.Tiny;
        options.Label(
            "Makes all Genetron upgrade requirements 1/4 of their original time.\nRequires game restart to take effect."
        );
        Text.Font = GameFont.Small;
        scrollViewHeight += 60f;
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref EnableFasterUpgrades, "GeneratorEnableFasterUpgrades", false);
    }
}
