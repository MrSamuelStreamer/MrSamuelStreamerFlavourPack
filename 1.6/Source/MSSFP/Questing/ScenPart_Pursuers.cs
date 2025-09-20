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
    public FactionDef faction;
    public PawnsArrivalModeDef raidArrivalMode;
    public List<MapGeneratorDef> safeMapGenerators = [];
    public List<LandmarkDef> safeLandmarks = [];
    public string alertPursuerThreatCriticalText;
    public string alertPursuerThreatText;
    public string alertPursuerThreatCriticalDescText;
    public string alertPursuerThreatDescText;
    public string letterLabelPursuerThreat;
    public string letterTextPursuerThreat;
    public string letterLabelPursuerThreatFoiled;
    public string letterTextPursuerThreatFoiled;

    public void CopyToScenPart(ScenPart_Pursuers scenPart)
    {

        scenPart.initialWarningDelay = initialWarningDelay;
        scenPart.initialRaidDelay = initialRaidDelay;
        scenPart.warningDelayRange = warningDelayRange;
        scenPart.raidDelayRange = raidDelayRange;
        scenPart.minRaidPoints = minRaidPoints;
        scenPart.raidPointMultiplier = raidPointMultiplier;
        scenPart.gravEngineCheckInterval = gravEngineCheckInterval;
        scenPart.faction = faction;
        scenPart.raidArrivalMode = raidArrivalMode;
        scenPart.safeMapGenerators = safeMapGenerators;
        scenPart.safeLandmarks = safeLandmarks;
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
    public bool questCompleted;
    public Map cachedAlertMap;
    public Alert_PursuerThreat alertCached;
    public bool hasGravEngineCached = true;
    public int lastCheckedGravEngineTick = -999999;
    public List<Map> tmpWarningKeys;
    public List<int> tmpWarningValues;
    public List<Map> tmpRaidKeys;
    public List<int> tmpRaidValues;
    public int startMapId = -1;

    public int initialWarningDelay = 2700;
    public int initialRaidDelay = 30000;
    public IntRange warningDelayRange = new(840000, 960000);
    public IntRange raidDelayRange = new(1080000, 2100000);
    public int minRaidPoints = 5000;
    public float raidPointMultiplier = 1.5f;
    public int gravEngineCheckInterval = 2500;
    public FactionDef faction;
    public PawnsArrivalModeDef raidArrivalMode;
    public List<MapGeneratorDef> safeMapGenerators = [];
    public List<LandmarkDef> safeLandmarks = [];
    public string alertPursuerThreatCriticalText = "Incoming Pursuers";
    public string alertPursuerThreatText = "Pursuers";
    public string alertPursuerThreatCriticalDescText = "They've found you, they will not stop";
    public string alertPursuerThreatDescText = "The pursuers are closing in on your location";
    public string letterLabelPursuerThreat = "Incoming Pursuers";
    public string letterTextPursuerThreat =
        "Pursuers are close to determining your location. When they locate you, they will send a massive force to kill you.\\n\\nGather your supplies, then flee!";
    public string letterLabelPursuerThreatFoiled;
    public string letterTextPursuerThreatFoiled;

    public ScenPart_Pursuers()
    {
        ModLog.Log("here");
    }

    public virtual void PostLoad()
    {
        if (def.HasModExtension<PursuersModExtension>())
        {
            def.GetModExtension<PursuersModExtension>().CopyToScenPart(this);
        }
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

    public Lazy<PursuersModExtension> pursuersModExtension => new(() => def.GetModExtension<PursuersModExtension>());

    public Alert_PursuerThreat AlertCached
    {
        get
        {
            if (Disabled)
                return null;
            if (cachedAlertMap != Find.CurrentMap)
                alertCached = null;
            if (alertCached != null || !mapWarningTimers.TryGetValue(Find.CurrentMap, out int mapRaidTick) || Find.TickManager.TicksGame <= mapRaidTick)
                return alertCached;
            alertCached = new Alert_PursuerThreat
            {
                raidTick = mapRaidTimers[Find.CurrentMap],
                alertPursuerThreatCriticalDescText = alertPursuerThreatCriticalDescText,
                alertPursuerThreatCriticalText = alertPursuerThreatCriticalText,
                alertPursuerThreatDescText = alertPursuerThreatDescText,
                alertPursuerThreatText = alertPursuerThreatText
            };
            cachedAlertMap = Find.CurrentMap;
            return alertCached;
        }
    }

    public virtual bool Disabled => questCompleted || (gravEngineCheckInterval > 0 && !hasGravEngineCached);

    public override bool OverrideDangerMusic => onStartMap;

    public override void ExposeData()
    {
        base.ExposeData();
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            PostLoad();
        }
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            foreach (Map key in mapWarningTimers.Keys.ToList())
            {
                if (key == null || key.Parent is { Destroyed: false }) continue;
                mapWarningTimers.Remove(key);
                mapRaidTimers.Remove(key);
            }
        }

        Scribe_Values.Look(ref onStartMap, "initialMap");
        Scribe_Values.Look(ref startMapId, "startMapId", -1);
        Scribe_Collections.Look(ref mapWarningTimers, "mapWarningTimers", LookMode.Reference, LookMode.Value, ref tmpWarningKeys, ref tmpWarningValues);
        Scribe_Collections.Look(ref mapRaidTimers, "mapRaidTimers", LookMode.Reference, LookMode.Value, ref tmpRaidKeys, ref tmpRaidValues);
        Scribe_Values.Look(ref questCompleted, "questCompleted");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            mapWarningTimers ??= new Dictionary<Map, int>();
            mapRaidTimers ??= new Dictionary<Map, int>();
        }

        lastCheckedGravEngineTick = -999999;

        Scribe_Values.Look(ref initialWarningDelay, "initialWarningDelay", 2700);
        Scribe_Values.Look(ref initialRaidDelay, "initialRaidDelay", 30000);
        Scribe_Values.Look(ref warningDelayRange, "warningDelayRange", new IntRange(840000, 960000));
        Scribe_Values.Look(ref raidDelayRange, "raidDelayRange", new IntRange(1080000, 2100000));
        Scribe_Values.Look(ref minRaidPoints, "minRaidPoints", 5000);
        Scribe_Values.Look(ref raidPointMultiplier, "raidPointMultiplier", 1.5f);
        Scribe_Values.Look(ref gravEngineCheckInterval, "gravEngineCheckInterval", 2500);
        Scribe_Defs.Look(ref faction, "faction");
        Scribe_Defs.Look(ref raidArrivalMode, "raidArrivalMode");
        Scribe_Collections.Look(ref safeMapGenerators, "safeMapGenerators", LookMode.Def);
        Scribe_Collections.Look(ref safeLandmarks, "safeLandmarks", LookMode.Def);
        Scribe_Values.Look(ref alertPursuerThreatCriticalText, "alertPursuerThreatCriticalText");
        Scribe_Values.Look(ref alertPursuerThreatText, "alertPursuerThreatText");
        Scribe_Values.Look(ref alertPursuerThreatCriticalDescText, "alertPursuerThreatCriticalDescText");
        Scribe_Values.Look(ref alertPursuerThreatDescText, "alertPursuerThreatDescText");
        Scribe_Values.Look(ref letterLabelPursuerThreat, "letterLabelPursuerThreat");
        Scribe_Values.Look(ref letterTextPursuerThreat, "letterTextPursuerThreat");
        Scribe_Values.Look(ref letterLabelPursuerThreatFoiled, "letterLabelPursuerThreatFoiled");
        Scribe_Values.Look(ref letterTextPursuerThreatFoiled, "letterTextPursuerThreatFoiled");
    }

    public override void PostWorldGenerate()
    {
        onStartMap = true;
        mapWarningTimers.Clear();
        mapRaidTimers.Clear();
    }

    public override void PostMapGenerate(Map map)
    {
        if (!map.IsPlayerHome)
            return;
        if (startMapId < 0) startMapId = map.uniqueID;
        StartTimers(map);
        lastCheckedGravEngineTick = -999999;
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

    public override void Tick()
    {
        if (gravEngineCheckInterval > 0 && Find.TickManager.TicksGame > lastCheckedGravEngineTick + gravEngineCheckInterval)
        {
            hasGravEngineCached = GravshipUtility.PlayerHasGravEngine();
            lastCheckedGravEngineTick = Find.TickManager.TicksGame;
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
                    Find.LetterStack.ReceiveLetter(letterLabelPursuerThreat, letterTextPursuerThreat,
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

    public virtual void StartTimers(Map map)
    {
        bool safe = safeMapGenerators.Contains(map.generatorDef);
        if (!safe && Find.World.landmarks.landmarks.TryGetValue(map.Tile, out Landmark landmark) && safeLandmarks.Contains(landmark.def))
        {
            safe = true;
        }

        if (safe)
        {
            Find.LetterStack.ReceiveLetter(letterLabelPursuerThreatFoiled, letterTextPursuerThreatFoiled,
                LetterDefOf.PositiveEvent);
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

    public override void Randomize()
    {
        base.Randomize();
    }

    public override IEnumerable<Alert> GetAlerts()
    {
        if (Find.CurrentMap is { IsPlayerHome: true } && AlertCached != null)
            yield return AlertCached;
    }

    private float scenPartRectHeight = 700;
    public override void DoEditInterface(Listing_ScenEdit listing)
    {
        if(safeMapGenerators == null) safeMapGenerators = [];
        if(safeLandmarks == null) safeLandmarks = [];

        float height = scenPartRectHeight +
                       (safeMapGenerators.Count * 30) +
                       (safeLandmarks.Count * 30);
        Rect scenPartRect = listing.GetScenPartRect(this, RowHeight + height);

        Widgets.DrawMenuSection(scenPartRect);
        Listing_Standard section = new();
        section.Begin(new Rect(scenPartRect.x + 4f, scenPartRect.y + 4f, scenPartRect.width - 4f * 2f, scenPartRect.height - (4f + 4f)));

        try
        {
            if (section.ButtonText(faction.LabelCap))
            {
                FloatMenuUtility.MakeMenu(DefDatabase<FactionDef>.AllDefsListForReading, (selectedFaction) => selectedFaction.LabelCap, (selectedFaction) => delegate
                {
                    faction = selectedFaction;
                });
            }

            section.Gap();
            section.Label("MSSFP_Pursuer_InitialWarningDelay".Translate(initialWarningDelay));
            section.IntAdjuster(ref initialWarningDelay, 60, 0);

            section.Gap();
            section.Label("MSSFP_Pursuer_InitialRaidDelay".Translate(initialRaidDelay));
            section.IntAdjuster(ref initialRaidDelay, 60, 0);

            section.Gap();
            section.Label("MSSFP_Pursuer_WarningDelayRange".Translate(warningDelayRange.min, warningDelayRange.max));
            section.IntAdjuster(ref warningDelayRange.min, 60, 0);
            section.IntAdjuster(ref warningDelayRange.max, 60, 0);

            section.Gap();
            section.Label("MSSFP_Pursuer_RaidDelayRange".Translate(raidDelayRange.min, raidDelayRange.max));
            section.IntAdjuster(ref raidDelayRange.min, 60, 0);
            section.IntAdjuster(ref raidDelayRange.max, 60, 0);

            section.Gap();
            section.Label("MSSFP_Pursuer_MinRaidPoints".Translate(minRaidPoints));
            section.IntAdjuster(ref minRaidPoints, 100, 0);

            section.Gap();
            section.Label("MSSFP_Pursuer_RaidPointMultiplier".Translate());
            raidPointMultiplier =
                section.SliderLabeled("MSSFP_Pursuer_RaidPointMultiplier".Translate(), raidPointMultiplier, 0, 100);

            section.Gap();
            section.Label("MSSFP_Pursuer_GravEngineCheckInterval".Translate(gravEngineCheckInterval));
            section.IntAdjuster(ref gravEngineCheckInterval, 100, 0);

            section.Gap();
            string ram_label = raidArrivalMode?.defName ?? "";
            if (section.ButtonText("MSSFP_Pursuer_RaidArrivalMode".Translate(ram_label)))
            {
                FloatMenuUtility.MakeMenu(DefDatabase<PawnsArrivalModeDef>.AllDefsListForReading, (mode) => mode.defName, (PawnsArrivalModeDef mode) => delegate
                {
                    raidArrivalMode = mode;
                });
            }

            section.GapLine();

            if (section.ButtonText("MSSFP_Pursuer_SafeMapGenerators".Translate()))
            {
                FloatMenuUtility.MakeMenu(DefDatabase<MapGeneratorDef>.AllDefsListForReading.Except(safeMapGenerators), (gen) => gen.defName,
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
                FloatMenuUtility.MakeMenu(DefDatabase<LandmarkDef>.AllDefsListForReading.Except(safeLandmarks), (gen) => gen.defName,
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
        }
        finally
        {
            listing.EndSection(section);
        }
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
