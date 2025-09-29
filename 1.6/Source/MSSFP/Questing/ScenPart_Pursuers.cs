using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Questing;

public class Alert_PursuerThreat : Alert_Scenario
{
    public int raidTick;
    public string alertPursuerThreatCriticalText;
    public string alertPursuerThreatText;
    public string alertPursuerThreatCriticalDescText;
    public string alertPursuerThreatDescText;

    private bool Red => Find.TickManager.TicksGame > raidTick - 60000;

    private bool Critical => Find.TickManager.TicksGame > raidTick;

    public override AlertReport GetReport() => AlertReport.Active;

    public override string GetLabel()
    {
        return Critical ? alertPursuerThreatCriticalText : alertPursuerThreatText + ": " + (raidTick - Find.TickManager.TicksGame).ToStringTicksToPeriodVerbose();
    }

    public override TaggedString GetExplanation()
    {
        return Critical ? alertPursuerThreatCriticalDescText : alertPursuerThreatDescText;
    }

    protected override Color BGColor => !Red ? Color.clear : Alert_Critical.BgColor();
}

public class PursuersModExtension : DefModExtension
{
    public int initialWarningDelay = 2700;
    public int initialRaidDelay = 30000;
    public IntRange warningDelayRange = new(840000, 960000);
    public IntRange raidDelayRange = new(1080000, 2100000);
    public int minRaidPoints = 5000;
    public float raidPointMultiplier = 1.5f;
    public int gravEngineCheckInterval = 2500;
    public int safetyCheckInterval = 3000;
    public FactionDef faction;
    public PawnsArrivalModeDef raidArrivalMode;
    public List<MapGeneratorDef> safeMapGenerators = [];
    public List<LandmarkDef> safeLandmarks = [];
    public List<ThingDef> safeThings = [];
    public List<TileMutatorDef> safeMutators = [];
    public string alertPursuerThreatCriticalText = "MSSFP_Scen_Pursuers_alertPursuerThreatCriticalText";
    public string alertPursuerThreatText = "MSSFP_Scen_Pursuers_alertPursuerThreatText";
    public string alertPursuerThreatCriticalDescText = "MSSFP_Scen_Pursuers_alertPursuerThreatCriticalDescText";
    public string alertPursuerThreatDescText = "MSSFP_Scen_Pursuers_alertPursuerThreatDescText";
    public string letterLabelPursuerThreat = "MSSFP_Scen_Pursuers_letterLabelPursuerThreat";
    public string letterTextPursuerThreat = "MSSFP_Scen_Pursuers_letterTextPursuerThreat";
    public string letterLabelPursuerThreatFoiled = "MSSFP_Scen_Pursuers_letterLabelPursuerThreatFoiled";
    public string letterTextPursuerThreatFoiled = "MSSFP_Scen_Pursuers_letterTextPursuerThreatFoiled";
    public bool allowOnNonPlayerHome = true;

    public void CopyToScenPart(ScenPart_Pursuers scenPart)
    {

        scenPart.initialWarningDelay = initialWarningDelay;
        scenPart.initialRaidDelay = initialRaidDelay;
        scenPart.warningDelayRange = warningDelayRange;
        scenPart.raidDelayRange = raidDelayRange;
        scenPart.minRaidPoints = minRaidPoints;
        scenPart.raidPointMultiplier = raidPointMultiplier;
        scenPart.gravEngineCheckInterval = gravEngineCheckInterval;
        scenPart.safetyCheckInterval = safetyCheckInterval;
        scenPart.faction = faction;
        scenPart.raidArrivalMode = raidArrivalMode;
        scenPart.safeMapGenerators = safeMapGenerators;
        scenPart.safeLandmarks = safeLandmarks;
        scenPart.safeThings = safeThings;
        scenPart.safeMutators = safeMutators;
        scenPart.alertPursuerThreatCriticalText = alertPursuerThreatCriticalText;
        scenPart.alertPursuerThreatText = alertPursuerThreatText;
        scenPart.alertPursuerThreatCriticalDescText = alertPursuerThreatCriticalDescText;
        scenPart.alertPursuerThreatDescText = alertPursuerThreatDescText;
        scenPart.letterLabelPursuerThreat = letterLabelPursuerThreat;
        scenPart.letterTextPursuerThreat = letterTextPursuerThreat;
        scenPart.letterLabelPursuerThreatFoiled = letterLabelPursuerThreatFoiled;
        scenPart.letterTextPursuerThreatFoiled = letterTextPursuerThreatFoiled;
        scenPart.allowOnNonPlayerHome = allowOnNonPlayerHome;
    }

    public override void ResolveReferences(Def parentDef)
    {
        base.ResolveReferences(parentDef);
        faction ??= FactionDefOf.Mechanoid;
        raidArrivalMode ??= PawnsArrivalModeDefOf.RandomDrop;
    }
}

public class ScenPart_Pursuers : ScenPart
{
    public bool onStartMap = true;
    public Dictionary<Map, int> mapWarningTimers = new();
    public Dictionary<Map, int> mapRaidTimers = new();
    public HashSet<int> eternallySafeMaps = [];
    public bool questCompleted;
    public Map cachedAlertMap;
    public Alert_PursuerThreat alertCached;
    public bool hasGravEngineCached = false;
    public bool hasSafetyThingCached = false;
    public int lastCheckedGravEngineTick = -999999;
    public int lastCheckedSafetyTick = -999999;
    public List<Map> tmpWarningKeys;
    public List<int> tmpWarningValues;
    public List<Map> tmpRaidKeys;
    public List<int> tmpRaidValues;
    public int startMapId = -1;

    public int initialWarningDelay;
    public int initialRaidDelay;
    public IntRange warningDelayRange;
    public IntRange raidDelayRange;
    public int minRaidPoints;
    public float raidPointMultiplier;
    public int gravEngineCheckInterval;
    public int safetyCheckInterval;
    public bool allowOnNonPlayerHome = true;
    public FactionDef faction;
    public PawnsArrivalModeDef raidArrivalMode;
    public List<MapGeneratorDef> safeMapGenerators = [];
    public List<LandmarkDef> safeLandmarks = [];
    public List<ThingDef> safeThings = [];
    public List<TileMutatorDef> safeMutators = [];
    public string alertPursuerThreatCriticalText = "MSSFP_Scen_Pursuers_alertPursuerThreatCriticalText";
    public string alertPursuerThreatText = "MSSFP_Scen_Pursuers_alertPursuerThreatText";
    public string alertPursuerThreatCriticalDescText = "MSSFP_Scen_Pursuers_alertPursuerThreatCriticalDescText";
    public string alertPursuerThreatDescText = "MSSFP_Scen_Pursuers_alertPursuerThreatDescText";
    public string letterLabelPursuerThreat = "MSSFP_Scen_Pursuers_letterLabelPursuerThreat";
    public string letterTextPursuerThreat = "MSSFP_Scen_Pursuers_letterTextPursuerThreat";
    public string letterLabelPursuerThreatFoiled = "MSSFP_Scen_Pursuers_letterLabelPursuerThreatFoiled";
    public string letterTextPursuerThreatFoiled = "MSSFP_Scen_Pursuers_letterTextPursuerThreatFoiled";

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (PursuersModExt.Value == null)
            yield return "ScenPart_Pursuers requires a PursuersModExtension.";
    }

    public virtual void PostLoad()
    {
        PursuersModExt.Value.CopyToScenPart(this);
    }

    [DebugAction("Scenario", "Reset Pursuer Timers", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void ResetPursuerScenPart()
    {
        foreach (ScenPart_Pursuers scenPart in Find.Scenario.AllParts.OfType<ScenPart_Pursuers>())
        {
            scenPart.PostWorldGenerate();
            foreach (Map map in Find.Maps)
            {
                scenPart.PostMapGenerate(map);
            }
        }
        Log.Message("Pursuer timers reset.");
    }

    public Lazy<PursuersModExtension> PursuersModExt => new(() => def.GetModExtension<PursuersModExtension>());

    public Faction Faction => Find.FactionManager.FirstFactionOfDef(faction);

    public bool MapAllowed(Map map)
    {
        if (map == null) return false;
        ModLog.Debug($"Map {map.uniqueID} is player home: {map.IsPlayerHome}, allowOnNonPlayerHome: {allowOnNonPlayerHome}");
        return allowOnNonPlayerHome || map.IsPlayerHome;
    }

    public Alert_PursuerThreat AlertCached
    {
        get
        {
            Map currentMap = Find.CurrentMap;
            ModLog.Debug(Faction.NameColored);
            if (currentMap == null || cachedAlertMap != currentMap)
            {
                alertCached = null;
                cachedAlertMap = null;
            }
            if (Disabled || currentMap == null)
                return null;
            if (alertCached != null || !mapWarningTimers.TryGetValue(currentMap, out int mapRaidWarnTick) || Find.TickManager.TicksGame <= mapRaidWarnTick)
                return alertCached;
            if (!mapRaidTimers.TryGetValue(currentMap, out int raidTick)) return null;
            alertCached = new Alert_PursuerThreat
            {
                raidTick = raidTick,
                alertPursuerThreatCriticalDescText = alertPursuerThreatCriticalDescText.Translate(Faction.NameColored),
                alertPursuerThreatCriticalText = alertPursuerThreatCriticalText.Translate(Faction.NameColored),
                alertPursuerThreatDescText = alertPursuerThreatDescText.Translate(Faction.NameColored),
                alertPursuerThreatText = alertPursuerThreatText.Translate(Faction.NameColored)
            };
            cachedAlertMap = currentMap;
            return alertCached;
        }
    }

    public virtual bool Disabled => questCompleted || (gravEngineCheckInterval > 0 && !hasGravEngineCached);

    public override bool OverrideDangerMusic => onStartMap;

    public override void ExposeData()
    {
        base.ExposeData();
        SafetyNullFix();
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            PostLoad();
        }
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            foreach (Map key in mapWarningTimers.Keys.ToList())
            {
                if (key == null || key.Parent is { Destroyed: false }) continue;
                ModLog.Debug($"Removing timers for map {key?.uniqueID} during save");
                mapWarningTimers.Remove(key);
                mapRaidTimers.Remove(key);
            }
        }

        Scribe_Values.Look(ref onStartMap, "initialMap");
        Scribe_Values.Look(ref startMapId, "startMapId", -1);
        Scribe_Collections.Look(ref mapWarningTimers, "mapWarningTimers", LookMode.Reference, LookMode.Value, ref tmpWarningKeys, ref tmpWarningValues);
        Scribe_Collections.Look(ref mapRaidTimers, "mapRaidTimers", LookMode.Reference, LookMode.Value, ref tmpRaidKeys, ref tmpRaidValues);
        Scribe_Values.Look(ref questCompleted, "questCompleted");
        Scribe_Values.Look(ref initialWarningDelay, "initialWarningDelay", PursuersModExt.Value.initialWarningDelay);
        Scribe_Values.Look(ref initialRaidDelay, "initialRaidDelay", PursuersModExt.Value.initialRaidDelay);
        Scribe_Values.Look(ref warningDelayRange, "warningDelayRange", PursuersModExt.Value.warningDelayRange);
        Scribe_Values.Look(ref raidDelayRange, "raidDelayRange", PursuersModExt.Value.raidDelayRange);
        Scribe_Values.Look(ref minRaidPoints, "minRaidPoints", PursuersModExt.Value.minRaidPoints);
        Scribe_Values.Look(ref raidPointMultiplier, "raidPointMultiplier", PursuersModExt.Value.raidPointMultiplier);
        Scribe_Values.Look(ref gravEngineCheckInterval, "gravEngineCheckInterval", PursuersModExt.Value.gravEngineCheckInterval);
        Scribe_Values.Look(ref safetyCheckInterval, "safetyCheckInterval", PursuersModExt.Value.safetyCheckInterval);
        Scribe_Values.Look(ref allowOnNonPlayerHome, "allowOnNonPlayerHome", PursuersModExt.Value.allowOnNonPlayerHome);
        Scribe_Defs.Look(ref faction, "faction");
        Scribe_Defs.Look(ref raidArrivalMode, "raidArrivalMode");
        Scribe_Collections.Look(ref safeMapGenerators, "safeMapGenerators", LookMode.Def);
        Scribe_Collections.Look(ref safeLandmarks, "safeLandmarks", LookMode.Def);
        Scribe_Collections.Look(ref safeThings, "safeThings", LookMode.Def);
        Scribe_Collections.Look(ref safeMutators, "safeMutators", LookMode.Def);
        Scribe_Values.Look(ref alertPursuerThreatCriticalText, "alertPursuerThreatCriticalText", PursuersModExt.Value.alertPursuerThreatCriticalText);
        Scribe_Values.Look(ref alertPursuerThreatText, "alertPursuerThreatText", PursuersModExt.Value.alertPursuerThreatText);
        Scribe_Values.Look(ref alertPursuerThreatCriticalDescText, "alertPursuerThreatCriticalDescText", PursuersModExt.Value.alertPursuerThreatCriticalDescText);
        Scribe_Values.Look(ref alertPursuerThreatDescText, "alertPursuerThreatDescText", PursuersModExt.Value.alertPursuerThreatDescText);
        Scribe_Values.Look(ref letterLabelPursuerThreat, "letterLabelPursuerThreat", PursuersModExt.Value.letterLabelPursuerThreat);
        Scribe_Values.Look(ref letterTextPursuerThreat, "letterTextPursuerThreat", PursuersModExt.Value.letterTextPursuerThreat);
        Scribe_Values.Look(ref letterLabelPursuerThreatFoiled, "letterLabelPursuerThreatFoiled", PursuersModExt.Value.letterLabelPursuerThreatFoiled);
        Scribe_Values.Look(ref letterTextPursuerThreatFoiled, "letterTextPursuerThreatFoiled", PursuersModExt.Value.letterTextPursuerThreatFoiled);

        if (Scribe.mode != LoadSaveMode.PostLoadInit) return;

        SafetyNullFix();
        faction ??= PursuersModExt.Value.faction;
        raidArrivalMode ??= PursuersModExt.Value.raidArrivalMode;
        lastCheckedGravEngineTick = -999999;
        lastCheckedSafetyTick = -999999;

    }

    public void SafetyNullFix()
    {
        mapWarningTimers ??= new Dictionary<Map, int>();
        mapRaidTimers ??= new Dictionary<Map, int>();
        safeMapGenerators ??= [];
        safeLandmarks ??= [];
        safeThings ??= [];
        safeMutators ??= [];
        eternallySafeMaps ??= [];
    }

    public override void PostWorldGenerate()
    {
        onStartMap = true;
        mapWarningTimers.Clear();
        mapRaidTimers.Clear();

        Faction fac = Find.FactionManager.FirstFactionOfDef(faction);
        if (fac != null) return;

        // Create the faction if it doesn't exist
        FactionGenerator.CreateFactionAndAddToManager(faction);
    }

    public override void PostMapGenerate(Map map)
    {
        if (!MapAllowed(map))
            return;
        if (startMapId < 0) startMapId = map.uniqueID;
        StartTimers(map);
        lastCheckedGravEngineTick = -999999;
        lastCheckedSafetyTick = -999999;
    }

    public override void MapRemoved(Map map)
    {
        if (mapWarningTimers.Remove(map))
            mapRaidTimers.Remove(map);
        if (map.uniqueID != startMapId) return;
        onStartMap = false;
    }

    public virtual void AdjustTimers(int ticks, Map map)
    {
        if (mapWarningTimers.ContainsKey(map))
            mapWarningTimers[map] = Math.Max(Find.TickManager.TicksGame + 1, mapWarningTimers[map] + ticks);
        if (mapRaidTimers.ContainsKey(map))
            mapRaidTimers[map] = Math.Max(Find.TickManager.TicksGame + 1, mapRaidTimers[map] + ticks);
        if (cachedAlertMap != map || AlertCached == null) return;
        AlertCached.raidTick = mapRaidTimers[map];
    }

    public bool HasAnySafetyThing(Map map)
    {
        foreach (ThingDef safeDef in safeThings)
        {
            if (map.listerThings.ThingsOfDef(safeDef).Any()) return true;
        }
        return false;
    }

    public bool MapIsSafeNow(Map map) => hasSafetyThingCached || questCompleted || eternallySafeMaps.Contains(map.uniqueID);

    public override void Tick()
    {
        if (questCompleted) return;
        if (gravEngineCheckInterval > 0 && Find.TickManager.TicksGame > lastCheckedGravEngineTick + gravEngineCheckInterval)
        {
            hasGravEngineCached = GravshipUtility.PlayerHasGravEngine();
            lastCheckedGravEngineTick = Find.TickManager.TicksGame;
        }

        if (Find.CurrentMap is {} currentMap && safetyCheckInterval > 0 && safeThings.Count > 0 && Find.TickManager.TicksGame > lastCheckedSafetyTick + safetyCheckInterval && !eternallySafeMaps.Contains(currentMap.uniqueID))
        {
            hasSafetyThingCached = HasAnySafetyThing(currentMap);
            lastCheckedSafetyTick = Find.TickManager.TicksGame;
            if (mapWarningTimers.ContainsKey(currentMap) && hasSafetyThingCached)
            {
                MarkMapSafe(currentMap);
            }
            if (!hasSafetyThingCached && !mapWarningTimers.ContainsKey(currentMap) && MapAllowed(currentMap))
            {
                StartTimers(currentMap);
            }
        }

        if (Disabled)
        {
            foreach (Map map in Find.Maps)
            {
                if (mapWarningTimers.ContainsKey(map))
                {
                    mapWarningTimers[map]++;
                    mapRaidTimers[map]++;
                }
            }
        }
        else
        {
            foreach (Map key in mapWarningTimers.Keys)
            {
                if (Find.TickManager.TicksGame == mapWarningTimers[key])
                {
                    Find.LetterStack.ReceiveLetter(letterLabelPursuerThreat.Translate(Faction), letterTextPursuerThreat.Translate(Faction),
                        LetterDefOf.ThreatSmall);
                }
            }

            foreach (Map key in mapRaidTimers.Keys)
            {
                if (Find.TickManager.TicksGame >= mapRaidTimers[key] && (Find.TickManager.TicksGame - mapRaidTimers[key]) % 30000 == 0)
                    FireRaid(key);
            }
        }
    }

    public void MarkMapSafe(Map map, bool sendLetter = true, bool eternal = false)
    {
        if (sendLetter) Find.LetterStack.ReceiveLetter(letterLabelPursuerThreatFoiled.Translate(Faction), letterTextPursuerThreatFoiled.Translate(Faction),
            LetterDefOf.PositiveEvent);
        mapWarningTimers.Remove(map);
        mapRaidTimers.Remove(map);
        if (eternal) eternallySafeMaps.Add(map.uniqueID);
        if (cachedAlertMap != map) return;
        alertCached = null;
    }

    public virtual void StartTimers(Map map)
    {
        if (map == null) return;
        if (eternallySafeMaps.Contains(map.uniqueID)) return;
        bool safe = (map.TileInfo?.Landmark is { } landmarkInfo && safeLandmarks.Contains(landmarkInfo.def)) ||
                    (map.generatorDef is { } generatorDef && safeMapGenerators.Contains(generatorDef)) ||
                    (map.TileInfo?.Mutators?.Any(m => safeMutators.Contains(m)) ?? false);

        if (safe)
        {
            MarkMapSafe(map, sendLetter: true, eternal: true);
            return;
        }

        if (map.IsStartingMap)
        {
            mapWarningTimers[map] = Find.TickManager.TicksGame + initialWarningDelay;
            mapRaidTimers[map] = Find.TickManager.TicksGame + initialRaidDelay;
        }
        else
        {
            mapWarningTimers[map] = Find.TickManager.TicksGame + warningDelayRange.RandomInRange;
            mapRaidTimers[map] = Find.TickManager.TicksGame + raidDelayRange.RandomInRange;
        }
        ModLog.Debug($"Started Pursuer timers on map {map.uniqueID}: warning at {mapWarningTimers[map]}, raid at {mapRaidTimers[map]}");
    }

    public void Notify_QuestCompleted() => questCompleted = true;

    public virtual void FireRaid(Map map)
    {
        IncidentDefOf.RaidEnemy.Worker.TryExecute(new IncidentParms
        {
            forced = true,
            target = map,
            points = Mathf.Max(minRaidPoints, StorytellerUtility.DefaultThreatPointsNow(map) * raidPointMultiplier),
            faction = FactionUtility.DefaultFactionFrom(faction),
            raidArrivalMode = raidArrivalMode ?? PawnsArrivalModeDefOf.RandomDrop,
            raidStrategy = RaidStrategyDefOf.ImmediateAttack
        });
    }

    public override IEnumerable<Alert> GetAlerts()
    {
        if (MapAllowed(Find.CurrentMap) && AlertCached != null)
            yield return AlertCached;
    }

    private float scenPartRectHeight = 900;

    public override void DoEditInterface(Listing_ScenEdit listing)
    {
        safeMapGenerators ??= [];
        safeLandmarks ??= [];
        safeThings ??= [];
        safeMutators ??= [];

        float height = scenPartRectHeight +
                       (safeMapGenerators.Count * 30) +
                       (safeThings.Count * 30) +
                       (safeMutators.Count * 30) +
                       (safeLandmarks.Count * 30);
        Rect scenPartRect = listing.GetScenPartRect(this, RowHeight + height);

        Widgets.DrawMenuSection(scenPartRect);
        Listing_Standard section = new();
        section.Begin(new Rect(scenPartRect.x + 4f, scenPartRect.y + 4f, scenPartRect.width - 4f * 2f, scenPartRect.height - (4f + 4f)));

        try
        {
            if (section.ButtonText(faction.LabelCap))
            {
                FloatMenuUtility.MakeMenu(DefDatabase<FactionDef>.AllDefsListForReading, selectedFaction => selectedFaction.LabelCap, selectedFaction => delegate
                {
                    faction = selectedFaction;
                });
            }

            section.Gap();
            section.Label("MSSFP_Pursuer_InitialWarningDelay".Translate(initialWarningDelay.ToStringTicksToPeriodVerbose(allowQuadrums: false)));
            section.IntAdjuster(ref initialWarningDelay, 2500, 0);

            section.Gap();
            section.Label("MSSFP_Pursuer_InitialRaidDelay".Translate(initialRaidDelay.ToStringTicksToPeriodVerbose(allowQuadrums: false)));
            section.IntAdjuster(ref initialRaidDelay, 2500, 0);

            section.Gap();
            section.Label("MSSFP_Pursuer_WarningDelayRange".Translate(warningDelayRange.min.ToStringTicksToPeriodVerbose(allowQuadrums: false), warningDelayRange.max.ToStringTicksToPeriodVerbose(allowQuadrums: false)));
            section.IntAdjuster(ref warningDelayRange.min, 2500, 0);
            section.IntAdjuster(ref warningDelayRange.max, 2500, 0);

            section.Gap();
            section.Label("MSSFP_Pursuer_RaidDelayRange".Translate(raidDelayRange.min.ToStringTicksToPeriodVerbose(allowQuadrums: false), raidDelayRange.max.ToStringTicksToPeriodVerbose(allowQuadrums: false)));
            section.IntAdjuster(ref raidDelayRange.min, 2500, 0);
            section.IntAdjuster(ref raidDelayRange.max, 2500, 0);

            section.Gap();
            section.Label("MSSFP_Pursuer_MinRaidPoints".Translate(minRaidPoints));
            section.IntAdjuster(ref minRaidPoints, 100, 0);

            section.Gap();
            section.CheckboxLabeled("MSSFP_Pursuer_AllowOnNonPlayerHome".Translate(), ref allowOnNonPlayerHome);

            section.Gap();
            raidPointMultiplier =
                section.SliderLabeled("MSSFP_Pursuer_RaidPointMultiplier".Translate(raidPointMultiplier.ToString("0.##")), raidPointMultiplier, 0, 10, 0.6f);

            section.Gap();
            section.Label("MSSFP_Pursuer_GravEngineCheckInterval".Translate(gravEngineCheckInterval));
            section.IntAdjuster(ref gravEngineCheckInterval, 100, -1);

            section.Gap();
            section.Label("MSSFP_Pursuer_SafetyCheckInterval".Translate(safetyCheckInterval));
            section.IntAdjuster(ref safetyCheckInterval, 100, -1);

            section.Gap();
            string ram_label = raidArrivalMode?.defName ?? "";
            if (section.ButtonText("MSSFP_Pursuer_RaidArrivalMode".Translate(ram_label)))
            {
                FloatMenuUtility.MakeMenu(DefDatabase<PawnsArrivalModeDef>.AllDefsListForReading, mode => mode.defName, mode => delegate
                {
                    raidArrivalMode = mode;
                });
            }

            section.GapLine();

            if (section.ButtonText("MSSFP_Pursuer_SafeMapGenerators".Translate()))
            {
                FloatMenuUtility.MakeMenu(DefDatabase<MapGeneratorDef>.AllDefsListForReading.Except(safeMapGenerators), gen => gen.defName,
                    gen => delegate
                    {
                        safeMapGenerators.Add(gen);
                    });
            }

            MapGeneratorDef selectedGen = null;
            foreach (MapGeneratorDef gen in safeMapGenerators)
            {
                if (section.ButtonText("MSSFP_Pursuer_RemoveGen".Translate(gen.defName)))
                {
                    selectedGen = gen;
                    break;
                }
            }

            if (selectedGen != null) safeMapGenerators.Remove(selectedGen);

            section.GapLine();

            if (section.ButtonText("MSSFP_Pursuer_Landmarks".Translate()))
            {
                FloatMenuUtility.MakeMenu(DefDatabase<LandmarkDef>.AllDefsListForReading.Except(safeLandmarks), gen => gen.defName,
                    gen => delegate
                    {
                        safeLandmarks.Add(gen);
                    });
            }

            LandmarkDef selectedLandmark = null;
            foreach (LandmarkDef gen in safeLandmarks)
            {
                if (section.ButtonText("MSSFP_Pursuer_LandmarksRemove".Translate(gen.defName)))
                {
                    selectedLandmark = gen;
                    break;
                }
            }

            if (selectedLandmark != null) safeLandmarks.Remove(selectedLandmark);

            section.GapLine();

            if (section.ButtonText("MSSFP_Pursuer_Mutators".Translate()))
            {
                FloatMenuUtility.MakeMenu(DefDatabase<TileMutatorDef>.AllDefsListForReading.Except(safeMutators), gen => gen.defName,
                    gen => delegate
                    {
                        safeMutators.Add(gen);
                    });
            }

            TileMutatorDef selectedMutator = null;
            foreach (TileMutatorDef gen in safeMutators)
            {
                if (section.ButtonText("MSSFP_Pursuer_MutatorsRemove".Translate(gen.defName)))
                {
                    selectedMutator = gen;
                    break;
                }
            }
            if (selectedMutator != null) safeMutators.Remove(selectedMutator);

            section.GapLine();


            if (section.ButtonText("MSSFP_Pursuer_Things".Translate()))
            {
                FloatMenuUtility.MakeMenu(DefDatabase<ThingDef>.AllDefsListForReading
                        .Except(safeThings)
                        .Where(t => !t.IsBlueprint &&
                            !t.isFrameInt &&
                            !t.isUnfinishedThing &&
                            !t.IsApparel &&
                            !t.IsWeapon &&
                            !t.IsWall &&
                            !t.IsFilth), gen => gen.defName,
                    gen => delegate
                    {
                        safeThings.Add(gen);
                    });
            }

            ThingDef selectedThing = null;
            foreach (ThingDef gen in safeThings)
            {
                if (section.ButtonText("MSSFP_Pursuer_ThingsRemove".Translate(gen.defName)))
                {
                    selectedThing = gen;
                    break;
                }
            }

            if (selectedThing != null) safeThings.Remove(selectedThing);
        }
        finally
        {
            listing.EndSection(section);
        }
        ExposeData();
    }
}

public class CompProperties_UseEffectThrowOffPursuer : CompProperties_UseEffect
{
    public int adjustTicks = 60000;

    public CompProperties_UseEffectThrowOffPursuer()
    {
        compClass = typeof(Comp_UseEffectThrowOffPursuer);
    }
}

public class Comp_UseEffectThrowOffPursuer : CompUseEffect
{
    public CompProperties_UseEffectThrowOffPursuer Props => (CompProperties_UseEffectThrowOffPursuer) props;

    public override void DoEffect(Pawn pawn)
    {
        if (parent.Spawned)
        {
            foreach (ScenPart_Pursuers scenPart in Find.Scenario.AllParts.OfType<ScenPart_Pursuers>())
            {
                if (scenPart.Disabled) continue;
                scenPart.AdjustTimers(Props.adjustTicks, pawn.Map);
            }
        }
        Messages.Message("MSSFP_ThrowOffPursuerUseMessage".Translate(), pawn, MessageTypeDefOf.PositiveEvent, false);
    }
}
