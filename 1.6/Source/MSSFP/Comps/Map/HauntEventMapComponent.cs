using System.Collections.Generic;
using System.Text;
using MSSFP.Defs;
using MSSFP.Hediffs;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Comps.Map;

/// <summary>
/// Drives the poltergeist event system. Ticks every 6 in-game hours,
/// evaluates map haunt intensity, and fires events when the threshold is crossed.
/// Also handles deferred light restoration from the FlickeringLights event.
/// </summary>
public class HauntEventMapComponent(Verse.Map map) : MapComponent(map)
{
    // ── Check interval ───────────────────────────────────────────────────────
    private const int CheckIntervalTicks = 2500 * 6; // ~6 in-game hours

    private int nextCheckTick = -1;

    // ── Light restore queue (saved so lights restore after a reload) ─────────
    private List<int> pendingRestoreIds = new();
    private List<int> pendingRestoreTicks = new();

    // ── Dev dashboard event log ──────────────────────────────────────────────
    public readonly List<string> RecentEvents = new();
    private const int MaxEventLog = 10;

    public override void MapComponentTick()
    {
        int now = Find.TickManager.TicksGame;

        // Restore flickered lights
        for (int i = pendingRestoreIds.Count - 1; i >= 0; i--)
        {
            if (now < pendingRestoreTicks[i])
                continue;
            Thing thing = map.listerThings.AllThings.Find(t => t.thingIDNumber == pendingRestoreIds[i]);
            CompFlickable flick = thing?.TryGetComp<CompFlickable>();
            if (flick != null)
                flick.SwitchIsOn = true;
            pendingRestoreIds.RemoveAt(i);
            pendingRestoreTicks.RemoveAt(i);
        }

        if (!MSSFPMod.settings.EnablePoltergeistEvents)
            return;
        if (!map.IsPlayerHome)
            return;

        if (nextCheckTick < 0)
            nextCheckTick = now + CheckIntervalTicks;
        if (now < nextCheckTick)
            return;

        nextCheckTick = now + CheckIntervalTicks;
        TryFireEvent();
    }

    /// <summary>
    /// Force a poltergeist event immediately (used by exorcism retaliation).
    /// Picks any eligible event regardless of current intensity/threshold.
    /// </summary>
    public void ForceRandomEvent(Pawn focusPawn)
    {
        HauntEventDef eventDef = PickEvent(1f);
        if (eventDef == null)
            return;
        bool fired = eventDef.Worker.TryFire(focusPawn, map);
        if (fired)
            LogEvent($"[retaliation] {eventDef.label}");
    }

    /// <summary>Called by FlickeringLights worker to schedule re-enable.</summary>
    public void ScheduleLightRestore(int thingId, int atTick)
    {
        pendingRestoreIds.Add(thingId);
        pendingRestoreTicks.Add(atTick);
    }

    private void TryFireEvent()
    {
        float intensity = CalculateIntensity(out float maxSeverity);
        if (intensity <= 0f)
            return;

        float timeMultiplier = GetTimeMultiplier();
        float effective = intensity * timeMultiplier * MSSFPMod.settings.PoltergeistIntensityMultiplier;

        // Colony size scaling: threshold rises 10% per colonist above 5
        int colonists = map.mapPawns.FreeColonistsSpawnedCount;
        float threshold = MSSFPMod.settings.PoltergeistEventThreshold
            * Mathf.Max(1f, 1f + (colonists - 5) * 0.1f);

        if (effective < threshold)
            return;

        Pawn focusPawn = PickHauntedPawn();
        if (focusPawn == null)
            return;

        HauntEventDef eventDef = PickEvent(maxSeverity);
        if (eventDef == null)
            return;

        bool fired = eventDef.Worker.TryFire(focusPawn, map);
        if (fired)
            LogEvent(eventDef.label);
    }

    private float CalculateIntensity(out float maxSeverity)
    {
        float total = 0f;
        maxSeverity = 0f;
        foreach (Pawn pawn in map.mapPawns.AllHumanlike)
        {
            if (!HauntsCache.Haunts.TryGetValue(pawn.thingIDNumber, out var haunts))
                continue;
            foreach (HediffComp_Haunt haunt in haunts)
            {
                float sev = haunt.parent.Severity;
                total += sev;
                if (sev > maxSeverity)
                    maxSeverity = sev;
            }
        }
        return total;
    }

    private float GetTimeMultiplier()
    {
        float hour = GenLocalDate.HourFloat(map);
        return (hour >= 21f || hour < 5f) ? 1.5f : 1.0f;
    }

    private Pawn PickHauntedPawn()
    {
        List<Pawn> candidates = new();
        foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
        {
            if (HauntsCache.Haunts.ContainsKey(pawn.thingIDNumber))
                candidates.Add(pawn);
        }
        return candidates.Count > 0 ? candidates.RandomElement() : null;
    }

    private static HauntEventDef PickEvent(float maxSeverity)
    {
        List<HauntEventDef> eligible = new();
        List<float> weights = new();

        foreach (HauntEventDef d in DefDatabase<HauntEventDef>.AllDefsListForReading)
        {
            if (maxSeverity < d.minSeverity)
                continue;
            eligible.Add(d);
            weights.Add(d.weight);
        }

        if (eligible.Count == 0)
            return null;

        float totalWeight = 0f;
        foreach (float w in weights)
            totalWeight += w;

        float roll = Rand.Value * totalWeight;
        float cumulative = 0f;
        for (int i = 0; i < eligible.Count; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
                return eligible[i];
        }
        return eligible[eligible.Count - 1];
    }

    private void LogEvent(string label)
    {
        string entry = $"[{GenDate.DateFullStringAt(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(map.Tile))}] {label}";
        RecentEvents.Add(entry);
        if (RecentEvents.Count > MaxEventLog)
            RecentEvents.RemoveAt(0);
    }

    public string DashboardSection()
    {
        float intensity = CalculateIntensity(out _);
        float timeMultiplier = GetTimeMultiplier();
        float effective = intensity * timeMultiplier * MSSFPMod.settings.PoltergeistIntensityMultiplier;
        int colonists = map.mapPawns.FreeColonistsSpawnedCount;
        float threshold = MSSFPMod.settings.PoltergeistEventThreshold
            * Mathf.Max(1f, 1f + (colonists - 5) * 0.1f);

        int ticksUntilNext = nextCheckTick - Find.TickManager.TicksGame;
        string nextStr = ticksUntilNext > 0
            ? $"{(float)ticksUntilNext / GenDate.TicksPerHour:F1}h"
            : "imminent";

        StringBuilder sb = new();
        sb.AppendLine("--- Poltergeist Events ---");
        sb.AppendLine(
            $"  Enabled: {MSSFPMod.settings.EnablePoltergeistEvents} | "
            + $"Intensity: {effective:F2} / threshold {threshold:F2} | "
            + $"Time mult: {timeMultiplier:F1}x | Next: {nextStr}"
        );
        if (RecentEvents.Count > 0)
        {
            sb.AppendLine("  Recent events:");
            foreach (string e in RecentEvents)
                sb.AppendLine($"    {e}");
        }
        return sb.ToString();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref nextCheckTick, "nextCheckTick", -1);
        Scribe_Collections.Look(ref pendingRestoreIds, "pendingRestoreIds", LookMode.Value);
        Scribe_Collections.Look(ref pendingRestoreTicks, "pendingRestoreTicks", LookMode.Value);
        pendingRestoreIds ??= new List<int>();
        pendingRestoreTicks ??= new List<int>();
    }
}
