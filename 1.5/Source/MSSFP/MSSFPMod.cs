using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MSSFP.HarmonyPatches;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP;

[StaticConstructorOnStartup]
public class MSSFPMod : Mod
{
    public static Settings settings;

    public MSSFPMod(ModContentPack content)
        : base(content)
    {
        ModLog.Debug("Hello world from Mr Samuel Streamer Flavour Pack");

        // initialize settings
        settings = GetSettings<Settings>();
#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.main");
        harmony.PatchAll();

        Type NC = AccessTools.Inner(typeof(Dialog_NamePawn), "NameContext");
        ConstructorInfo CI = AccessTools.Constructor(NC, [typeof(string), typeof(int), typeof(string), typeof(int), typeof(bool), typeof(List<string>)]);
        MethodInfo MI = AccessTools.Method(typeof(NameContext_Patch), nameof(NameContext_Patch.Postfix));

        harmony.Patch(CI, null, new HarmonyMethod(MI));
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
