using Verse;
using UnityEngine;
using HarmonyLib;

namespace MSSFP;

public class MSSFPMod : Mod
{
    public static Settings settings;

    public MSSFPMod(ModContentPack content) : base(content)
    {
        ModLog.Debug("Hello world from Mr Samuel Streamer Flavour Pack");

        // initialize settings
        settings = GetSettings<Settings>();
#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.main");
        harmony.PatchAll();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        settings.DoWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return "MSSFP_SettingsCategory".Translate();
    }
}
