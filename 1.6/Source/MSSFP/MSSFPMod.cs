using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MSSFP.HarmonyPatches;
using MSSFP.PawnPortability;
using MSSFP.Utils;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP;

public class MSSFPMod : Mod
{
    public static Settings settings;
    public static MSSFPMod Mod;
    private static Harmony _harmony;

    public MSSFPMod(ModContentPack content)
        : base(content)
    {
        Mod = this;
        ModLog.Log("Loading MSS Tweaks and Fun");

        // initialize settings
        settings = GetSettings<Settings>();

#if DEBUG
        Harmony.DEBUG = true;
#endif
        _harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.main");
        _harmony.PatchAll();

        // Conditional cross-mod compat patch — silently no-ops when HAR is absent.
        // Guards against NVA-removed apparel defs cascading into PawnCanWear exceptions.
        HAR_GetApparelFromApparelProps_NullGuard.TryRegister(_harmony);

        // Conditional cross-mod compat patch — silently no-ops when XMT is absent.
        // Guards against XMT social ThoughtWorkers NRE'ing on Big&Small sapient-animal-
        // converted pawns (including MSSFP's raven creepjoiner), which would otherwise
        // generate thousands of caught-NRE log writes per minute and crater TPS.
        XMT_SocialThought_NullGuard.TryRegister(_harmony);

        // Conditional cross-mod compat patch — silently no-ops when Intimacy - Socio
        // Butterfly (LovelyDovey.Recreation.WithEuterpe) is absent. Guards against the
        // SocioButterfly JobGiver_GetFood postfix NRE'ing on humanlike pawns whose ThingDef
        // is not vanilla "Human" — Big&Small sapient animals (incl. MSSFP's sapient raven
        // creepjoiner), HAR races, Androids, custom xenos. SocioButterfly only injects
        // Comp_SocioButterflyTracker into the Human ThingDef, then derefs the comp without
        // null-checking. Without this guard, every food-priority eval per affected pawn
        // throws an NRE that ThinkNode_PrioritySorter catches and logs.
        SocioButterfly_GetFood_NullGuard.TryRegister(_harmony);

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

    internal static void ApplySettingsToDefs()
    {
        if (settings == null)
            return;
        settings.MechFormingSpeedBaseValue = Mathf.Max(0.025f,settings.MechFormingSpeedBaseValue);
        StatDef statDef = StatDef.Named("MechFormingSpeed");
        if (statDef != null)
        {
            statDef.defaultBaseValue = 1f / settings.MechFormingSpeedBaseValue;
        }
    }

    public override void WriteSettings()
    {
        base.WriteSettings();
        ApplySettingsToDefs();

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

/// <summary>
/// Runs after all defs are loaded. By this point MSSFPMod's instance constructor has
/// already executed and MSSFPMod.settings is fully initialised.
/// </summary>
[StaticConstructorOnStartup]
internal static class MSSFPStartup
{
    static MSSFPStartup()
    {
        MSSFPMod.ApplySettingsToDefs();
        if (MSSFPMod.settings?.EnableUserTemplateLoading == true)
            UserPawnTemplateRegistry.LoadAll();
        else
            ModLog.Log($"User template loading disabled in settings [MSSFPMod.settings is null? {MSSFPMod.settings == null}");
    }
}
