using System.Collections.Generic;
using System.Linq;
using MSSFP.Hediffs;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Haunts;

/// <summary>
/// Shared cleanup logic for haunt deduplication and resurrection.
/// All removal goes through Pawn.health.RemoveHediff to trigger full comp lifecycle
/// (cache rebuild, thought removal, skill boost recalculation).
/// </summary>
public static class HauntCleanupUtility
{
    /// <summary>
    /// Returns true if the given spirit pawn already haunts any living pawn.
    /// O(1) via HauntsCache reverse lookup.
    /// </summary>
    public static bool IsAlreadyHaunting(Pawn spirit) =>
        spirit != null && HauntsCache.IsSpiritHaunting(spirit.thingIDNumber);

    /// <summary>
    /// Removes all haunts that reference the given spirit across all pawns.
    /// Used when a spirit is resurrected — they're alive now, no longer a ghost.
    /// </summary>
    public static void RemoveHauntsForSpirit(Pawn spirit)
    {
        if (spirit == null)
            return;

        HediffComp_Haunt comp = HauntsCache.GetHauntForSpirit(spirit.thingIDNumber);
        if (comp?.parent?.pawn == null)
            return;

        Pawn hauntedPawn = comp.parent.pawn;
        Hediff hediff = comp.parent;

        // RemoveHediff triggers CompPostPostRemoved → cache cleanup, thought removal,
        // skill boost rebuild. No manual cleanup needed.
        hauntedPawn.health.RemoveHediff(hediff);
    }

    /// <summary>
    /// Scans HauntsCache for haunt comps where pawnToDraw is null (cross-reference
    /// failed on load, e.g. haunted pawn was in a caravan while spirit was on the
    /// home map). Attempts recovery via the integer backup ID saved alongside the
    /// reference. Orphans that cannot be recovered are removed cleanly.
    /// Called once on game load, before DeduplicateHaunts.
    /// </summary>
    public static void RecoverOrphanedHaunts()
    {
        // Build a lookup of spirit thingIDNumber → Pawn from all current graves.
        var spiritById = new Dictionary<int, Pawn>();
        foreach (Map map in Find.Maps)
        {
            foreach (Building_Grave grave in map.listerBuildings.AllBuildingsColonistOfClass<Building_Grave>())
            {
                Pawn spirit = grave.Corpse?.InnerPawn;
                if (spirit != null)
                    spiritById[spirit.thingIDNumber] = spirit;
            }
        }

        // Snapshot orphaned comps from the cache — avoids mutating while iterating.
        List<HediffComp_Haunt> orphans = HauntsCache.Haunts
            .Values
            .SelectMany(list => list)
            .Where(comp => comp.pawnToDraw == null)
            .ToList();

        int recovered = 0;
        int removed = 0;

        foreach (HediffComp_Haunt comp in orphans)
        {
            Pawn owner = comp.parent?.pawn;
            if (owner == null)
                continue;

            if (comp.SpiritIdBackup >= 0 && spiritById.TryGetValue(comp.SpiritIdBackup, out Pawn spirit))
            {
                // Reference recovery succeeded — wire up and update reverse cache.
                comp.pawnToDraw = spirit;
                HauntsCache.SpiritToHaunt[spirit.thingIDNumber] = comp;
                recovered++;
            }
            else
            {
                // Spirit no longer exists or never had a backup ID — remove the orphan.
                // Dead/unspawned pawns can't safely go through RemoveHediff lifecycle.
                if (!owner.Dead)
                    owner.health.RemoveHediff(comp.parent);
                else
                    HauntsCache.RemoveHaunt(owner.thingIDNumber, comp);
                removed++;
            }
        }

        if (recovered > 0 || removed > 0)
            Log.Message($"[MSSFP] Haunt recovery: {recovered} recovered, {removed} orphaned haunt(s) removed.");
    }

    /// <summary>
    /// Scans all pawns across all maps for duplicate spirit haunts (same pawnToDraw
    /// on multiple living pawns). Keeps the oldest instance, removes the rest.
    /// Called once on game load to fix existing saves.
    /// </summary>
    public static void DeduplicateHaunts()
    {
        // spiritId → list of (haunt comp, hediff) across all pawns
        Dictionary<int, List<(HediffComp_Haunt comp, Hediff hediff, Pawn owner)>> spiritMap = new();

        void ScanPawn(Pawn pawn)
        {
            if (pawn?.health?.hediffSet?.hediffs == null)
                return;
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                HediffComp_Haunt hauntComp = hediff.TryGetComp<HediffComp_Haunt>();
                if (hauntComp?.pawnToDraw == null)
                    continue;

                int spiritId = hauntComp.pawnToDraw.thingIDNumber;
                if (!spiritMap.ContainsKey(spiritId))
                    spiritMap[spiritId] = new List<(HediffComp_Haunt, Hediff, Pawn)>();

                spiritMap[spiritId].Add((hauntComp, hediff, pawn));
            }
        }

        foreach (Map map in Find.Maps)
        {
            foreach (Pawn pawn in map.mapPawns.AllHumanlike)
                ScanPawn(pawn);
        }

        foreach (Caravan caravan in Find.WorldObjects.Caravans)
        {
            foreach (Pawn pawn in caravan.PawnsListForReading)
                ScanPawn(pawn);
        }

        int removed = 0;
        foreach (var (spiritId, entries) in spiritMap)
        {
            if (entries.Count <= 1)
                continue;

            // Keep oldest (first in list — hediff iteration order matches add order).
            // Remove all others.
            for (int i = 1; i < entries.Count; i++)
            {
                var (_, hediff, owner) = entries[i];
                if (!owner.health.hediffSet.hediffs.Contains(hediff))
                    continue;
                owner.health.RemoveHediff(hediff);
                removed++;
            }
        }

        if (removed > 0)
        {
            // Rebuild reverse cache — RemoveHediff during dedup may leave stale entries
            // because load-order overwrites can desync SpiritToHaunt from actual state.
            HauntsCache.SpiritToHaunt.Clear();
            foreach (var (spiritId, entries) in spiritMap)
            {
                if (entries.Count == 0)
                    continue;
                var (comp, _, _) = entries[0];
                if (comp.pawnToDraw != null)
                    HauntsCache.SpiritToHaunt[comp.pawnToDraw.thingIDNumber] = comp;
            }

            Log.Message($"[MSSFP] Haunt deduplication: removed {removed} duplicate haunt(s).");
        }
    }
}
