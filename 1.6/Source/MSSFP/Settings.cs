using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MSSFP.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP;

public class Settings : ModSettings
{
    public static float BreakdownMTBDaysDefault = 13680000f / GenDate.TicksPerDay;

    public bool ShowHaunts = false;
    public bool EnableSkylanternRaids = false;
    public bool DrawByMrStreamer = false;
    public bool EnableGenesOnGrowthMoment = false;
    public float GeneEventChance = 1f;
    public float GoodGeneChance = 1f / 4f;
    public float BadGeneChance = 1f / 4f;
    public float NeutralGeneChance = 1f / 4f;
    public float RandomGeneChance = 1f / 4f;
    public int DaysForFission = 7;
    public bool EnableLoversRetreat = false;
    public bool allowAnyPregnant = false;
    public bool SingleUseMentalFuses = false;
    public bool EnablePossession = false;
    public bool DisableBSIncorporateGeneLimit = false;
    public bool EnableGeneStealerNeed = false;
    public bool EnableGeneMutators = false;
    public bool EnableTrekBeamers = true;
    public bool EnableMercenaryHiring = true;
    public bool useMrStreamerMercenaries = false;
    public bool EnableWanderDelayModification = false;
    public bool WanderDelayIncludeHumanoids = false;
    public int WanderDelayTicks = 0;
    public bool EnableColonistPortraitHiding = true;
    public bool ShowHiddenPortraits = false;
    public bool Enable10SecondsToSpeed = false;
    public bool Active10SecondsToSpeed = true;
    public bool OverrideFactionLeaderSpawn = true;
    public bool BoostChanceToSpawnExistingPawns = true;
    public bool EnableExtraReformationPoints = false;
    public int AnnualReformationPoints = 5;
    public int ReformationPointsPerBaby = 1;
    public int ReformationPointsPerDefeatedFaction = 5;
    public int TechsToGetPoints = 5;
    public int ReformationPointsForTechs = 1;
    public int ReformationPointsPerSeasonChange = 1;
    public int TenSecondsToSpeedDelay = 10;
    public bool EnableRecoilDamage = false;
    public float RecoilDamageMultiplier = 0.1f;
    public bool EnableRecoilKnockback = false;
    public float RecoilKnockbackMultiplier = 0.1f;
    public float BreakdownMTBDays = BreakdownMTBDaysDefault;

    public HashSet<Verse.TimeSpeed> MonitoredSpeeds = new()
    {
        Verse.TimeSpeed.Paused,
        Verse.TimeSpeed.Normal,
    };

    public bool IsSpeedMonitored(Verse.TimeSpeed speed)
    {
        return MonitoredSpeeds?.Contains(speed) ?? false;
    }

    public void ToggleSpeedMonitoring(Verse.TimeSpeed speed)
    {
        if (MonitoredSpeeds == null)
            MonitoredSpeeds = new HashSet<Verse.TimeSpeed>();

        if (MonitoredSpeeds.Contains(speed))
            MonitoredSpeeds.Remove(speed);
        else
            MonitoredSpeeds.Add(speed);
    }

    public string GetMonitoredSpeedsText()
    {
        if (MonitoredSpeeds == null || MonitoredSpeeds.Count == 0)
            return "None";

        return string.Join(", ", MonitoredSpeeds.Select(s => s.ToString()));
    }

    public Settings()
    {
        Assembly[] assemblies;
        List<Type> types = [];

        // Get assemblies
        try
        {
            assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a=>a.FullName.Contains("MSSFP")).ToArray();
        }
        catch (Exception ex)
        {
            ModLog.Error($"Failed to load assemblies: {ex}");
            return;
        }

        // get all SettingsTab subtypes
        foreach (Assembly assembly in assemblies)
        {
            foreach (Type type in assembly.GetTypes()
                         .Where(t => !t.IsAbstract && typeof(SettingsTab).IsAssignableFrom(t)))
            {
                try
                {
                    types.Add(type);
                }
                catch (Exception ex)
                {
                    ModLog.Error($"Failed to load types from assembly {assembly.FullName}: {ex}");
                }
            }
        }

        // init all SettingsTabs
        foreach (Type type in types.Distinct())
        {
            try
            {
                if(Tabs.Any(t=>t.GetType() == type)) continue;
                if (Activator.CreateInstance(type, this, MSSFPMod.Mod) is SettingsTab tab)
                {
                    RegisterTab(tab);
                }
            }
            catch (Exception ex)
            {
                ModLog.Error($"Failed to create instance of tab {type.FullName}: {ex}");
            }
        }
    }

    protected static List<SettingsTab> Tabs = new();

    public T GetSettings<T>() where T: SettingsTab
    {
        return Tabs.FirstOrDefault(t => t is T) as T;
    }

    public static void RegisterTab(SettingsTab tab)
    {
        Tabs.Add(tab);
        Tabs = Tabs.OrderBy(t => t.TabOrder).ToList();
    }

    public SettingsTab DefaultTab => Tabs.FirstOrDefault(t => t.IsDefault);

    private float ScrollViewWidth = 0;
    public Vector2 scrollPosition = Vector2.zero;

    public SettingsTab SelectedTab;

    public static Lazy<MethodInfo> _TempGuiContent = new(() =>
        typeof(GUIContent).GetMethod(
            "Temp",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            [typeof(string)],
            null
        )
    );

    public static GUIContent TempGuiContent(string t) =>
        _TempGuiContent.Value.Invoke(null, [t]) as GUIContent;

    public static Color TabRectColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    public Vector2 RectForLabelText(string text)
    {
        GUIContent content = new(text);
        return Text.CurFontStyle.CalcSize(content);
    }

    public void DoWindowContents(Rect wrect)
    {
        float tabsHeight = 30 + 16;
        Rect tabButtonScrollerRect = new(wrect.x, wrect.y, wrect.width, tabsHeight);
        Rect tabScrollContainerRect = new(wrect.x, wrect.y, ScrollViewWidth, 30);

#if DEBUG
        Widgets.DrawRectFast(tabButtonScrollerRect, Color.red);
        Widgets.DrawRectFast(tabScrollContainerRect, Color.green);
#endif
        ScrollViewWidth = 0;
        scrollPosition = GUI.BeginScrollView(
            tabButtonScrollerRect,
            scrollPosition,
            tabScrollContainerRect
        );

        foreach (SettingsTab tab in Tabs)
        {
            float width = RectForLabelText(tab.TabName).x + 8;

            Rect buttonRect = new(
                tabScrollContainerRect.x + ScrollViewWidth,
                tabScrollContainerRect.y,
                width,
                30
            );

            if (SelectedTab == tab)
            {
                TextAnchor anchor = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.DrawAtlas(buttonRect, Widgets.ButtonBGAtlasClick);
                Widgets.Label(buttonRect, tab.TabName);
                Text.Anchor = anchor;
            }
            else
            {
                if (Widgets.ButtonText(buttonRect, tab.TabName))
                {
                    SelectedTab = tab;
                }
            }

            ScrollViewWidth += buttonRect.width + 2;
        }

        GUI.EndScrollView();

        Widgets.DrawLineHorizontal(0, wrect.yMin + tabsHeight + 1, wrect.width, Color.grey);
        Widgets.DrawLineHorizontal(0, wrect.yMin + tabsHeight + 2, wrect.width, Color.grey);

        Rect tabRect = new(
            tabButtonScrollerRect.xMin,
            wrect.yMin + tabsHeight + 3,
            wrect.width,
            wrect.height - tabButtonScrollerRect.height - 3
        );

        Widgets.DrawRectFast(tabRect, TabRectColor);

        SelectedTab ??= DefaultTab;

        SelectedTab.DrawTab(tabRect);
    }

    public override void ExposeData()
    {
        ModLog.Debug($"ExposeData {Scribe.mode} - {Tabs.Count}");
        // Save/load main settings directly to ensure they persist
        Scribe_Values.Look(
            ref EnableWanderDelayModification,
            "EnableWanderDelayModification",
            false
        );
        Scribe_Values.Look(ref WanderDelayIncludeHumanoids, "WanderDelayIncludeHumanoids", false);
        Scribe_Values.Look(ref WanderDelayTicks, "WanderDelayTicks", 0);
        Scribe_Values.Look(ref EnableGenesOnGrowthMoment, "EnableGenesOnGrowthMoment", false);
        Scribe_Values.Look(ref EnableColonistPortraitHiding, "EnableColonistPortraitHiding", true);
        Scribe_Values.Look(ref ShowHiddenPortraits, "ShowHiddenPortraits", false);
        Scribe_Values.Look(ref Enable10SecondsToSpeed, "Enable10SecondsToSpeed", false);
        Scribe_Values.Look(ref Active10SecondsToSpeed, "Active10SecondsToSpeed", false);
        Scribe_Values.Look(ref OverrideFactionLeaderSpawn, "OverrideFactionLeaderSpawn", true);
        Scribe_Values.Look(ref BoostChanceToSpawnExistingPawns, "BoostChanceToSpawnExistingPawns", true);
        Scribe_Values.Look(ref TenSecondsToSpeedDelay, "TenSecondsToSpeedDelay", 10);
        Scribe_Values.Look(ref EnableExtraReformationPoints, "EnableAnnualReformationPoints", false);
        Scribe_Values.Look(ref AnnualReformationPoints, "AnnualReformationPoints", 5);
        Scribe_Values.Look(ref ReformationPointsPerBaby, "ReformationPointsPerBaby", 1);
        Scribe_Values.Look(ref ReformationPointsPerDefeatedFaction, "ReformationPointsPerDefeatedFaction", 5);
        Scribe_Values.Look(ref TechsToGetPoints, "TechsToGetPoints", 5);
        Scribe_Values.Look(ref ReformationPointsForTechs, "ReformationPointsForTechs", 1);
        Scribe_Values.Look(ref ReformationPointsPerSeasonChange, "ReformationPointsPerSeasonChange", 1);
        Scribe_Values.Look(ref EnableRecoilDamage, "EnableRecoilDamage", false);
        Scribe_Values.Look(ref RecoilDamageMultiplier, "RecoilDamageMultiplier", 0.1f);
        Scribe_Values.Look(ref EnableRecoilKnockback, "EnableRecoilKnockback", false);
        Scribe_Values.Look(ref RecoilKnockbackMultiplier, "RecoilKnockbackMultiplier", 0.1f);
        Scribe_Values.Look(ref BreakdownMTBDays, "BreakdownMTBDays", BreakdownMTBDaysDefault);
        Scribe_Collections.Look(ref MonitoredSpeeds, "MonitoredSpeeds", LookMode.Value);

        foreach (SettingsTab settingsTab in Tabs)
        {
            settingsTab.ExposeData();
        }
    }
}
