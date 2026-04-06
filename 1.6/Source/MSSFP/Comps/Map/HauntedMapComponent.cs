using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSSFP.Hediffs;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Comps.Map;

public class HauntedMapComponent(Verse.Map map) : MapComponent(map)
{
    public int LastFiredTick = 0;
    public int SearchRadius => MSSFPMod.settings.HauntProximityRadius;

    /// <summary>
    /// Sum of all haunt severities on this map. Updated lazily when the dashboard renders.
    /// Future extensions (e.g. poltergeist events) can use this as a trigger threshold.
    /// </summary>
    public float MapIntensityScore { get; private set; }

    public IEnumerable<Building_Grave> Graves =>
        map.listerBuildings.AllBuildingsColonistOfClass<Building_Grave>()
            .Where(g => g.Corpse != null);

    public IEnumerable<Pawn> PawnsNearGraves =>
        Graves
            .SelectMany(thing => GenRadial.RadialCellsAround(thing.Position, SearchRadius, true))
            .Distinct()
            .SelectMany(cell => map.thingGrid.ThingsAt(cell))
            .OfType<Pawn>();

    public IEnumerable<Pawn> PawnPool =>
        PawnsNearGraves.Where(pawn =>
            !pawn.health.hediffSet.HasHediff(MSSFPDefOf.MSS_FP_PawnDisplayer)
        );

    public override void MapComponentTick()
    {
        if (!MSSFPMod.settings.EnablePossession)
            return;
        if (!map.IsPlayerHome)
            return;

        //It's ok if we miss some ticks, so the simple check is fine
        if (LastFiredTick + MSSFPMod.settings.HauntMinCooldownDays * GenDate.TicksPerDay >= Find.TickManager.TicksGame)
            return;

        LastFiredTick = Find.TickManager.TicksGame + MSSFPMod.settings.HauntMinCooldownDays * GenDate.TicksPerDay;

        Pawn pawn = PawnPool.RandomElementWithFallback();
        if (pawn == null)
            return;

        Building_Grave grave = GenRadial
            .RadialCellsAround(pawn.Position, SearchRadius, true)
            .SelectMany(cell => Graves.Where(g => g.Position == cell))
            .RandomElementWithFallback();

        LastFiredTick += MSSFPMod.settings.HauntPostFireCooldownDays * GenDate.TicksPerDay;

        Hediff hediff = pawn.health.AddHediff(MSSFPDefOf.MSS_FP_PawnDisplayer);

        if (hediff.TryGetComp(out HediffComp_Haunt comp))
        {
            comp.SetPawnToDraw(grave.Corpse.InnerPawn);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref LastFiredTick, "LastFiredTick");
    }

    public override void MapComponentUpdate()
    {
        if (!MSSFPMod.settings.ShowHaunts)
            return;
        foreach (Pawn pawn in map.mapPawns.AllHumanlike)
        {
            HauntsCache.TryUpdateWander(pawn.thingIDNumber, pawn.TrueCenter());
            HauntsCache.TryDrawAt(pawn.thingIDNumber, pawn.TrueCenter());
        }
    }

    public override void MapComponentOnGUI()
    {
        if (!DebugSettings.ShowDevGizmos || !MSSFPMod.settings.ShowHauntDevDashboard)
            return;

        StringBuilder sb = new();
        sb.AppendLine("=== Haunts Dev Dashboard ===");

        int totalHaunts = 0;
        float totalSeverity = 0f;

        foreach (Pawn pawn in map.mapPawns.AllHumanlike)
        {
            if (!HauntsCache.Haunts.TryGetValue(pawn.thingIDNumber, out var haunts))
                continue;

            foreach (HediffComp_Haunt haunt in haunts)
            {
                totalHaunts++;
                float severity = haunt.parent.Severity;
                totalSeverity += severity;

                string skillInfo = haunt.skillToBoost != null
                    ? $"{haunt.skillToBoost.defName} +{haunt.SkillBoostLevel}"
                    : "none";

                HediffComp_HauntProgression prog =
                    haunt.parent.TryGetComp<HediffComp_HauntProgression>();
                string progInfo = prog != null ? $" | {prog.DebugStatus()}" : string.Empty;

                sb.AppendLine(
                    $"  {pawn.LabelShort}: {haunt.parent.def.defName} "
                    + $"(sev={severity:F2}, skill={skillInfo}{progInfo})"
                );
            }
        }

        MapIntensityScore = totalSeverity;

        float hour = GenLocalDate.HourFloat(map);
        float timeMultiplier = (hour >= 21f || hour < 5f) ? 1.5f : 1.0f;

        sb.Insert(
            sb.ToString().IndexOf('\n') + 1,
            $"Haunts: {totalHaunts} | Intensity: {totalSeverity:F2} | "
                + $"Time mult: {timeMultiplier:F1}x | "
                + $"Effective: {totalSeverity * timeMultiplier:F2}\n"
        );

        int ticksUntilNext = LastFiredTick
            + MSSFPMod.settings.HauntMinCooldownDays * GenDate.TicksPerDay
            - Find.TickManager.TicksGame;
        string cooldownStr = ticksUntilNext > 0
            ? $"{(float)ticksUntilNext / GenDate.TicksPerDay:F1}d"
            : "ready";
        sb.AppendLine($"Next spawn cooldown: {cooldownStr}");

        Rect rect = new(75f, 10f, 400f, 20f * (totalHaunts + 5));
        Widgets.Label(rect, sb.ToString());
    }
}
