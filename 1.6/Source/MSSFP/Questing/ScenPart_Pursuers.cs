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
                alertPursuerThreatCriticalDescText = pursuersModExtension.Value.alertPursuerThreatCriticalDescText,
                alertPursuerThreatCriticalText = pursuersModExtension.Value.alertPursuerThreatCriticalText,
                alertPursuerThreatDescText = pursuersModExtension.Value.alertPursuerThreatDescText,
                alertPursuerThreatText = pursuersModExtension.Value.alertPursuerThreatText
            };
            cachedAlertMap = Find.CurrentMap;
            return alertCached;
        }
    }

    public virtual bool Disabled => questCompleted || (pursuersModExtension.Value.gravEngineCheckInterval > 0 && !hasGravEngineCached);

    public override bool OverrideDangerMusic => onStartMap;

    public override void ExposeData()
    {
        base.ExposeData();
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
        if (pursuersModExtension.Value.gravEngineCheckInterval > 0 && Find.TickManager.TicksGame > lastCheckedGravEngineTick + pursuersModExtension.Value.gravEngineCheckInterval)
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
                    Find.LetterStack.ReceiveLetter(pursuersModExtension.Value.letterLabelPursuerThreat, pursuersModExtension.Value.letterTextPursuerThreat,
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
        bool safe = pursuersModExtension.Value.safeMapGenerators.Contains(map.generatorDef);
        if (!safe && Find.World.landmarks.landmarks.TryGetValue(map.Tile, out Landmark landmark) && pursuersModExtension.Value.safeLandmarks.Contains(landmark.def))
        {
            safe = true;
        }

        if (safe)
        {
            Find.LetterStack.ReceiveLetter(pursuersModExtension.Value.letterLabelPursuerThreatFoiled, pursuersModExtension.Value.letterTextPursuerThreatFoiled,
                LetterDefOf.PositiveEvent);
            return;
        }

        if (map.IsStartingMap)
        {
            mapWarningTimers[map] = Find.TickManager.TicksGame + pursuersModExtension.Value.initialWarningDelay;
            mapRaidTimers[map] = Find.TickManager.TicksGame + pursuersModExtension.Value.initialRaidDelay;
        }
        else
        {
            mapWarningTimers[map] = Find.TickManager.TicksGame + pursuersModExtension.Value.warningDelayRange.RandomInRange;
            mapRaidTimers[map] = Find.TickManager.TicksGame + pursuersModExtension.Value.raidDelayRange.RandomInRange;
        }
    }

    public void Notify_QuestCompleted() => questCompleted = true;

    public virtual void FireRaid(Map map)
    {
        IncidentDefOf.RaidEnemy.Worker.TryExecute(new IncidentParms
        {
            forced = true,
            target = map,
            points = Mathf.Max(pursuersModExtension.Value.minRaidPoints, StorytellerUtility.DefaultThreatPointsNow(map) * pursuersModExtension.Value.raidPointMultiplier),
            faction = FactionUtility.DefaultFactionFrom(pursuersModExtension.Value.faction),
            raidArrivalMode = pursuersModExtension.Value.raidArrivalMode ?? PawnsArrivalModeDefOf.RandomDrop,
            raidStrategy = RaidStrategyDefOf.ImmediateAttack
        });
    }

    public override IEnumerable<Alert> GetAlerts()
    {
        if (Find.CurrentMap is { IsPlayerHome: true } && AlertCached != null)
            yield return AlertCached;
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
