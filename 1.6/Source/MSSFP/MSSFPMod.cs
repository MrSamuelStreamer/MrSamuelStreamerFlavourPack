using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MSSFP.HarmonyPatches;
using MSSFP.Utils;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP;

[StaticConstructorOnStartup]
public class MSSFPMod : Mod
{
    public static Settings settings;
    public static MSSFPMod Mod;
    private static Harmony _harmony;

    public MSSFPMod(ModContentPack content)
        : base(content)
    {
        Mod = this;
        ModLog.Log("Loading the Mr Samuel Streamer Flavour Pack");

        // initialize settings
        settings = GetSettings<Settings>();

#if DEBUG
        Harmony.DEBUG = true;
#endif
        _harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.main");
        _harmony.PatchAll();

        Type NC = AccessTools.Inner(typeof(Dialog_NamePawn), "NameContext");
        ConstructorInfo CI = AccessTools.Constructor(
            NC,
            [
                typeof(string),
                typeof(int),
                typeof(string),
                typeof(int),
                typeof(bool),
                typeof(List<string>),
            ]
        );
        MethodInfo MI = AccessTools.Method(
            typeof(NameContext_Patch),
            nameof(NameContext_Patch.Postfix)
        );

        _harmony.Patch(CI, null, new HarmonyMethod(MI));

        ToggleSettlementDefeatPatch(settings.ReformationPointsPerDefeatedFaction > 0 && settings.EnableExtraReformationPoints);
    }

    public static void ToggleSettlementDefeatPatch(bool enable)
    {
        MethodInfo original = AccessTools.Method(typeof(SettlementDefeatUtility), nameof(SettlementDefeatUtility.CheckDefeated));
        MethodInfo prefix = AccessTools.Method(typeof(SettlementDefeatUtility_Patch), nameof(SettlementDefeatUtility_Patch.CheckDefeated_Prefix));

        if (enable)
        {
            _harmony.Patch(original, prefix: new HarmonyMethod(prefix));
        }
        else
        {
            _harmony.Unpatch(original, prefix);
        }
    }

    public override void WriteSettings()
    {
        base.WriteSettings();

        ToggleSettlementDefeatPatch(settings.ReformationPointsPerDefeatedFaction > 0 && settings.EnableExtraReformationPoints);

        // Snapshot and clear before iterating so a throwing action doesn't
        // leave stale entries or cause duplicate execution on next save.
        List<Action> actions = new(SettingsTab.PostSaveActions);
        SettingsTab.PostSaveActions.Clear();

        foreach (Action postSaveAction in actions)
        {
            try
            {
                postSaveAction.Invoke();
            }
            catch (Exception e)
            {
                ModLog.Error($"PostSaveAction failed: {e}");
            }
        }
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
