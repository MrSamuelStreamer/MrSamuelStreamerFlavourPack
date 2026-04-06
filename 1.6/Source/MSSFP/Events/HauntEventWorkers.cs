using System.Collections.Generic;
using System.Linq;
using MSSFP.Comps.Map;
using MSSFP.Hediffs;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Events;

// ──────────────────────────────────────────────────────────────────────────────
// Flickering Lights — turn off up to 3 nearby CompFlickable lamps for 30 s
// ──────────────────────────────────────────────────────────────────────────────
public class HauntEventWorker_FlickeringLights : HauntEventWorker
{
    private const float SearchRadius = 8f;
    private const int RestoreTicks = 1800;

    public override bool TryFire(Pawn pawn, Map map)
    {
        List<CompFlickable> candidates = new();
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, SearchRadius, true))
        {
            if (!cell.InBounds(map))
                continue;
            foreach (Thing thing in map.thingGrid.ThingsAt(cell))
            {
                CompFlickable flick = thing.TryGetComp<CompFlickable>();
                if (flick != null && flick.SwitchIsOn)
                    candidates.Add(flick);
            }
        }

        if (candidates.Count == 0)
            return false;

        HauntEventMapComponent comp = map.GetComponent<HauntEventMapComponent>();
        int restoreAt = Find.TickManager.TicksGame + RestoreTicks;
        int count = Mathf.Min(3, candidates.Count);

        foreach (CompFlickable flick in candidates.InRandomOrder().Take(count))
        {
            flick.SwitchIsOn = false;
            comp?.ScheduleLightRestore(flick.parent.thingIDNumber, restoreAt);
        }

        Messages.Message(
            "MSS_FP_Event_FlickeringLights_Msg".Translate(pawn.LabelShort),
            pawn,
            MessageTypeDefOf.NeutralEvent,
            false
        );
        return true;
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// Object Displacement — teleport a nearby item 3–5 cells in a random direction
// ──────────────────────────────────────────────────────────────────────────────
public class HauntEventWorker_ObjectDisplacement : HauntEventWorker
{
    private const float SearchRadius = 6f;
    private const int TeleportMin = 3;
    private const int TeleportMax = 5;

    public override bool TryFire(Pawn pawn, Map map)
    {
        List<Thing> candidates = new();
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, SearchRadius, true))
        {
            if (!cell.InBounds(map))
                continue;
            foreach (Thing thing in map.thingGrid.ThingsAt(cell))
            {
                if (thing is not Pawn && thing.def.EverHaulable)
                    candidates.Add(thing);
            }
        }

        if (candidates.Count == 0)
            return false;

        Thing target = candidates.RandomElement();
        int dist = Rand.RangeInclusive(TeleportMin, TeleportMax);
        IntVec3 dest = target.Position
            + GenAdj.CardinalDirections[Rand.RangeInclusive(0, 3)] * dist;

        if (!dest.InBounds(map) || !dest.Walkable(map))
            return false;

        target.DeSpawn();
        GenPlace.TryPlaceThing(target, dest, map, ThingPlaceMode.Near);

        Messages.Message(
            "MSS_FP_Event_ObjectDisplacement_Msg".Translate(target.LabelShort),
            new TargetInfo(dest, map),
            MessageTypeDefOf.NeutralEvent,
            false
        );
        return true;
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// Cold Spot — mood penalty for nearby pawns
// ──────────────────────────────────────────────────────────────────────────────
public class HauntEventWorker_ColdSpot : HauntEventWorker
{
    private const float Radius = 5f;

    public override bool TryFire(Pawn pawn, Map map)
    {
        bool any = false;
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, Radius, true))
        {
            if (!cell.InBounds(map))
                continue;
            foreach (Thing thing in map.thingGrid.ThingsAt(cell))
            {
                if (thing is not Pawn target || !target.RaceProps.Humanlike)
                    continue;
                if (target.needs?.mood?.thoughts?.memories == null)
                    continue;
                target.needs.mood.thoughts.memories.TryGainMemory(
                    MSSFPDefOf.MSS_FP_Event_ColdSpot
                );
                any = true;
            }
        }

        if (!any)
            return false;

        Messages.Message(
            "MSS_FP_Event_ColdSpot_Msg".Translate(pawn.LabelShort),
            pawn,
            MessageTypeDefOf.NeutralEvent,
            false
        );
        return true;
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// Ghostly Wail — mood penalty for all free colonists on the map
// ──────────────────────────────────────────────────────────────────────────────
public class HauntEventWorker_GhostlyWail : HauntEventWorker
{
    public override bool TryFire(Pawn pawn, Map map)
    {
        bool any = false;
        foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
        {
            if (colonist.needs?.mood?.thoughts?.memories == null)
                continue;
            colonist.needs.mood.thoughts.memories.TryGainMemory(
                MSSFPDefOf.MSS_FP_Event_EerieSound
            );
            any = true;
        }

        if (!any)
            return false;

        Messages.Message(
            "MSS_FP_Event_GhostlyWail_Msg".Translate(),
            pawn,
            MessageTypeDefOf.NeutralEvent,
            false
        );
        return true;
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// Spontaneous Fire — small fire starts on a flammable thing or floor near pawn
// ──────────────────────────────────────────────────────────────────────────────
public class HauntEventWorker_SpontaneousFire : HauntEventWorker
{
    private const float SearchRadius = 5f;
    private const float FireSize = 0.1f;

    public override bool TryFire(Pawn pawn, Map map)
    {
        List<IntVec3> candidates = new();
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, SearchRadius, true))
        {
            if (!cell.InBounds(map) || !cell.Walkable(map))
                continue;
            if (FireUtility.GetFiresNearCell(cell, map).Count > 0)
                continue;
            if (FireUtility.TerrainFlammability(cell, map) > 0.1f)
                candidates.Add(cell);
        }

        if (candidates.Count == 0)
            return false;

        IntVec3 fireCell = candidates.RandomElement();
        FireUtility.TryStartFireIn(fireCell, map, FireSize, null);

        Messages.Message(
            "MSS_FP_Event_SpontaneousFire_Msg".Translate(pawn.LabelShort),
            new TargetInfo(fireCell, map),
            MessageTypeDefOf.ThreatSmall,
            false
        );
        return true;
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// Possession Attempt — haunted pawn briefly enters WanderPsychotic mental state
// ──────────────────────────────────────────────────────────────────────────────
public class HauntEventWorker_PossessionAttempt : HauntEventWorker
{
    public override bool TryFire(Pawn pawn, Map map)
    {
        if (pawn.InMentalState)
            return false;
        if (pawn.mindState?.mentalStateHandler == null)
            return false;

        bool started = pawn.mindState.mentalStateHandler.TryStartMentalState(
            MentalStateDefOf.Wander_Psychotic,
            "MSS_FP_Event_PossessionAttempt_Reason".Translate(),
            forced: false,
            causedByMood: false
        );

        if (!started)
            return false;

        Messages.Message(
            "MSS_FP_Event_PossessionAttempt_Msg".Translate(pawn.LabelShort),
            pawn,
            MessageTypeDefOf.ThreatSmall,
            false
        );
        return true;
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// Manifestation — haunts become briefly visible; nearby pawns are terrified
// ──────────────────────────────────────────────────────────────────────────────
public class HauntEventWorker_Manifestation : HauntEventWorker
{
    private const float HauntRadius = 8f;
    private const float WitnessRadius = 8f;
    private const int ManifestTicks = 1800; // 30 seconds

    public override bool TryFire(Pawn pawn, Map map)
    {
        int manifestUntil = Find.TickManager.TicksGame + ManifestTicks;
        bool anyManifested = false;

        // Force nearby haunts visible
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, HauntRadius, true))
        {
            if (!cell.InBounds(map))
                continue;
            foreach (Thing thing in map.thingGrid.ThingsAt(cell))
            {
                if (thing is not Pawn hauntedPawn)
                    continue;
                if (!HauntsCache.Haunts.TryGetValue(hauntedPawn.thingIDNumber, out var haunts))
                    continue;
                foreach (HediffComp_Haunt haunt in haunts)
                {
                    haunt.ManifestUntilTick = manifestUntil;
                    anyManifested = true;
                }
            }
        }

        if (!anyManifested)
            return false;

        // Give witnesses the "apparition" thought
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, WitnessRadius, true))
        {
            if (!cell.InBounds(map))
                continue;
            foreach (Thing thing in map.thingGrid.ThingsAt(cell))
            {
                if (thing is not Pawn witness || !witness.RaceProps.Humanlike)
                    continue;
                if (witness.needs?.mood?.thoughts?.memories == null)
                    continue;
                witness.needs.mood.thoughts.memories.TryGainMemory(
                    MSSFPDefOf.MSS_FP_Event_WitnessedApparition
                );
            }
        }

        Messages.Message(
            "MSS_FP_Event_Manifestation_Msg".Translate(pawn.LabelShort),
            pawn,
            MessageTypeDefOf.NeutralEvent,
            false
        );
        return true;
    }
}
