using MSSFP.Utils;
using UnityEngine;
using Verse;

namespace MSSFP.GeneratorMod;

public class GeneratorSettingsTab(ModSettings settings, Mod mod) : SettingsTab(settings, mod)
{
    public Settings Settings => settings as Settings;

    public override string TabName => "Generator Mod";
    public override int TabOrder => 6;

    public override void DoTabContents(
        Listing_Standard options,
        Rect scrollViewRect,
        ref float scrollViewHeight
    )
    {
        DrawCheckBox(
            options,
            "MSS_FP_Settings_GeneratorEnableFasterUpgrades".Translate(),
            ref Settings.GeneratorEnableFasterUpgrades,
            ref scrollViewHeight
        );

        options.Gap(10f);
        scrollViewHeight += 10f;

        Text.Font = GameFont.Tiny;
        options.Label("MSS_FP_Settings_GeneratorFasterUpgradesNote".Translate());
        Text.Font = GameFont.Small;
        scrollViewHeight += 60f;
    }

    public override void ExposeData()
    {
        // Persistence handled by Settings.ExposeData() so it survives assembly removal.
    }
}
