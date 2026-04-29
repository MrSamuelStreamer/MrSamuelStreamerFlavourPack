using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSSFP.Haunts;
using MSSFP.Hediffs;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using static MSSFP.Haunts.HauntCleanupUtility;

namespace MSSFP.Comps.Map;

public class HauntedMapComponent(Verse.Map map) : MapComponent(map)
{
    public int LastFiredTick = 0;
    // GenRadial.RadialCellsAround hard-caps at 200 cells radius.
    public int SearchRadius => Math.Min(MSSFPMod.settings.HauntProximityRadius, 200);

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
            pawn.IsColonist
            && pawn.RaceProps.Humanlike
            && !pawn.health.hediffSet.HasHediff(MSSFPDefOf.MSS_FP_PawnDisplayer)
            && pawn.genes?.HasActiveGene(MSSFPDefOf.MSS_FP_Gene_HauntResistant) != true
        );

    public override void MapComponentTick()
    {
        if (!MSSFPMod.settings.EnableGraveHaunts)
            return;
        if (!map.IsPlayerHome)
            return;

        //It's ok if we miss some ticks, so the simple check is fine
        if (LastFiredTick + MSSFPMod.settings.HauntMinCooldownDays * GenDate.TicksPerDay >= Find.TickManager.TicksGame)
            return;

        LastFiredTick = Find.TickManager.TicksGame + MSSFPMod.settings.HauntMinCooldownDays * GenDate.TicksPerDay;

        Pawn pawn = PickWeightedPawn();
        if (pawn == null)
            return;

        Building_Grave grave = GenRadial
            .RadialCellsAround(pawn.Position, SearchRadius, true)
            .SelectMany(cell => Graves.Where(g => g.Position == cell))
            .RandomElementWithFallback();

        LastFiredTick += MSSFPMod.settings.HauntPostFireCooldownDays * GenDate.TicksPerDay;

        if (grave == null)
            return;

        Pawn spirit = grave.Corpse?.InnerPawn;
        if (spirit == null)
            return;

        // One spirit can only haunt one living pawn at a time.
        // If the haunted pawn has left this map (caravan, world), remove the old haunt
        // and fall through to assign a new target.
        if (IsAlreadyHaunting(spirit))
        {
            HediffComp_Haunt existingComp = HauntsCache.GetHauntForSpirit(spirit.thingIDNumber);
            Pawn hauntedPawn = existingComp?.parent?.pawn;
            if (hauntedPawn?.MapHeld == map)
                return; // Still here — don't reassign.

            // Pawn left the map or reference is stale — clean up, then reassign.
            if (existingComp?.parent != null)
            {
                if (hauntedPawn is { Dead: false })
                    hauntedPawn.health.RemoveHediff(existingComp.parent);
                else if (hauntedPawn != null)
                    HauntsCache.RemoveHaunt(hauntedPawn.thingIDNumber, existingComp);
            }
        }

        Hediff hediff = pawn.health.AddHediff(MSSFPDefOf.MSS_FP_PawnDisplayer);
        hediff.Severity = 0.05f;

        if (hediff.TryGetComp(out HediffComp_Haunt comp))
        {
            comp.SetPawnToDraw(spirit);
        }

        Find.LetterStack.ReceiveLetter(
            "MSS_FP_Haunt_InitialLetter_Label".Translate(),
            "MSS_FP_Haunt_InitialLetter_Text".Translate(
                spirit.LabelShort,
                pawn.LabelShort
            ),
            LetterDefOf.NeutralEvent,
            pawn
        );

        HauntProfile profile = HauntProfileBuilder.TryBuild(spirit);
        if (hediff.TryGetComp(out HediffComp_DynamicHaunt dynamicComp) && profile != null)
        {
            dynamicComp.SetProfile(profile);
        }
    }

    /// <summary>
    /// Picks a pawn from PawnPool with gene-based weighting:
    /// HauntSensitive pawns have 1.5× selection weight.
    /// </summary>
    private Pawn PickWeightedPawn() =>
        PawnPool.RandomElementByWeightWithFallback(
            p => p.genes?.HasActiveGene(MSSFPDefOf.MSS_FP_Gene_HauntSensitive) == true
                ? 1.5f
                : 1.0f
        );

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref LastFiredTick, "LastFiredTick");
    }

    public override void MapComponentUpdate()
    {
        if (!MSSFPMod.settings.ShowHaunts)
            return;
        if (WorldRendererUtility.WorldRendered)
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

                HediffComp_DynamicHaunt dynComp =
                    haunt.parent.TryGetComp<HediffComp_DynamicHaunt>();
                string dynInfo = dynComp != null ? $" | {dynComp.DebugStatus()}" : string.Empty;

                sb.AppendLine(
                    $"  {pawn.LabelShort}: {haunt.parent.def.defName} "
                    + $"(sev={severity:F2}, skill={skillInfo}{progInfo}{dynInfo})"
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

        HauntEventMapComponent events = map.GetComponent<HauntEventMapComponent>();
        if (events != null)
            sb.Append(events.DashboardSection());

        Rect rect = new(75f, 10f, 450f, 20f * (totalHaunts + 8 + (events?.RecentEvents.Count ?? 0)));
        Widgets.Label(rect, sb.ToString());
    }
}
