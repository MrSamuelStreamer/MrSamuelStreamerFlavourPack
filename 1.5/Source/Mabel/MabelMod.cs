using Verse;
using UnityEngine;
using HarmonyLib;

namespace Mabel;

public class MabelMod : Mod
{
    public static Settings settings;

    public MabelMod(ModContentPack content) : base(content)
    {

        // initialize settings
        settings = GetSettings<Settings>();
#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("Taggerung.rimworld.Mabel.main");	
        harmony.PatchAll();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        settings.DoWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return "Mabel_SettingsCategory".Translate();
    }
}
