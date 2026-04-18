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
    public bool EnableEcho = false;
    public bool ShowHauntDevDashboard = true;
    public float HauntProgressionSpeedMultiplier = 1.0f;
    public float HauntRegressionSpeedMultiplier = 1.0f;
    public bool AlwaysShowNamedHaunts = false;
    public bool EnableGraveHaunts = false;
    public bool EnablePoltergeistEvents = false;
    public float PoltergeistIntensityMultiplier = 1.0f;
    public float PoltergeistEventThreshold = 0.5f;
    public int HauntProximityRadius = 50;
    public int HauntMinCooldownDays = 2;
    public int HauntPostFireCooldownDays = 4;
    public bool EnableKillHaunts = true;
    public float KillHauntBaseChance = 0.15f;
    public int KillHauntCooldownTicks = 60000;
    public int MaxBadHauntsPerPawn = 5;
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
    public float FactionLeaderRaidChance = 0.4f;
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
    public bool ShowElevationOverlay = false;
    public float BreakdownMTBDays = BreakdownMTBDaysDefault;
    public bool NullDefSafetyPatch = true;
    public bool EnableCodexPunch = true;
    public float CodexPunchChanceMultiplier = 1.0f;
    public bool EnableUserTemplateLoading = true;
    public bool EnableTemplateWandererJoin = false;
    public float TemplateWandererJoinChanceMultiplier = 1.0f;

    // Fields for optional assembly tabs — these MUST live here (not on the tab)
    // so they survive save/load cycles when the optional assembly is removed.
    public bool GeneratorEnableFasterUpgrades = false;
    public List<string> ResourceGeneratorExtraBuildables = [];

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

    protected List<SettingsTab> Tabs = new();

    public T GetSettings<T>() where T: SettingsTab
    {
        return Tabs.FirstOrDefault(t => t is T) as T;
    }

    public void RegisterTab(SettingsTab tab)
    {
        Tabs.Add(tab);
        Tabs = Tabs.OrderBy(t => t.TabOrder).ToList();
    }

    public SettingsTab DefaultTab => Tabs.FirstOrDefault(t => t.IsDefault);

    public SettingsTab SelectedTab;

    public void DoWindowContents(Rect wrect)
    {
        SelectedTab ??= DefaultTab;

        List<TabRecord> tabRecords = new();
        foreach (SettingsTab tab in Tabs)
        {
            SettingsTab localTab = tab;
            tabRecords.Add(new TabRecord(
                localTab.TabName,
                () => SelectedTab = localTab,
                SelectedTab == localTab
            ));
        }

        // Content area — TabDrawer renders tabs in the 32f above this rect
        Rect contentRect = new(
            wrect.x,
            wrect.y + TabDrawer.TabHeight,
            wrect.width,
            wrect.height - TabDrawer.TabHeight
        );

        TabDrawer.DrawTabs(contentRect, tabRecords);

        SelectedTab?.DrawTab(contentRect);
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
        Scribe_Values.Look(ref WanderDelayTicks, "WanderDelayTicks", 0);
        Scribe_Values.Look(ref EnableGenesOnGrowthMoment, "EnableGenesOnGrowthMoment", false);
        Scribe_Values.Look(ref Active10SecondsToSpeed, "Active10SecondsToSpeed", false);
        Scribe_Values.Look(ref OverrideFactionLeaderSpawn, "OverrideFactionLeaderSpawn", true);
        Scribe_Values.Look(ref BoostChanceToSpawnExistingPawns, "BoostChanceToSpawnExistingPawns", true);
        Scribe_Values.Look(ref FactionLeaderRaidChance, "FactionLeaderRaidChance", 0.4f);
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
        Scribe_Values.Look(ref ShowElevationOverlay, "ShowElevationOverlay", false);
        Scribe_Values.Look(ref BreakdownMTBDays, "BreakdownMTBDays", BreakdownMTBDaysDefault);
        Scribe_Collections.Look(ref MonitoredSpeeds, "MonitoredSpeeds", LookMode.Value);

        // Optional assembly tab fields — saved here so they persist when the assembly is removed
        Scribe_Values.Look(ref GeneratorEnableFasterUpgrades, "GeneratorEnableFasterUpgrades", false);
        Scribe_Collections.Look(ref ResourceGeneratorExtraBuildables, "ExtraBuildables", LookMode.Value);

        foreach (SettingsTab settingsTab in Tabs)
        {
            settingsTab.ExposeData();
        }
    }
}
