using Verse;
using UnityEngine;
using HarmonyLib;

namespace Mr_Samuel_Streamer_Flavour_Pack;

public class Mr_Samuel_Streamer_Flavour_PackMod : Mod
{
    public static Settings settings;

    public Mr_Samuel_Streamer_Flavour_PackMod(ModContentPack content) : base(content)
    {
        Log.Message("Hello world from Mr Samuel Streamer Flavour Pack");

        // initialize settings
        settings = GetSettings<Settings>();
#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("MrSamuelStreamer.rimworld.Mr_Samuel_Streamer_Flavour_Pack.main");	
        harmony.PatchAll();
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        base.DoSettingsWindowContents(inRect);
        settings.DoWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return "Mr Samuel Streamer Flavour Pack_SettingsCategory".Translate();
    }
}
