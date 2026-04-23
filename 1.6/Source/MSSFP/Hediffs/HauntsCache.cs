using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Hediffs;

public static class HauntsCache
{
    public static Dictionary<int, List<HediffComp_Haunt>> Haunts = new();

    /// <summary>
    /// O(1) lookup for the stat patch hot path. Keyed by pawn thingIDNumber.
    /// Only populated for pawns that have a HediffComp_DynamicHaunt sibling.
    /// </summary>
    public static Dictionary<int, HediffComp_DynamicHaunt> DynamicHaunts = new();

    /// <summary>
    /// Reverse lookup: spirit thingIDNumber → the haunt comp that references it.
    /// Enforces one-haunt-per-spirit invariant and enables O(1) resurrection cleanup.
    /// </summary>
    public static Dictionary<int, HediffComp_Haunt> SpiritToHaunt = new();

    public static void AddHaunt(int thingIdNumber, HediffComp_Haunt haunt)
    {
        if (Haunts.TryGetValue(thingIdNumber, out List<HediffComp_Haunt> mods))
        {
            if (!mods.Contains(haunt))
            {
                mods.Add(haunt);
            }
        }
        else
        {
            Haunts[thingIdNumber] = [haunt];
        }

        // Cache sibling DynamicHaunt comp for the stat patch hot path.
        if (haunt.parent is HediffWithComps hwc)
        {
            HediffComp_DynamicHaunt dynComp = hwc.TryGetComp<HediffComp_DynamicHaunt>();
            if (dynComp != null)
                DynamicHaunts[thingIdNumber] = dynComp;
        }

        // Maintain reverse lookup: spirit → haunt comp.
        if (haunt.pawnToDraw != null)
            SpiritToHaunt[haunt.pawnToDraw.thingIDNumber] = haunt;
    }

    public static void RemoveHaunt(int thingIdNumber, HediffComp_Haunt haunt)
    {
        if (!Haunts.TryGetValue(thingIdNumber, out List<HediffComp_Haunt> mods))
        {
            return;
        }

        mods.Remove(haunt);

        if (mods.Count == 0)
        {
            Haunts.Remove(thingIdNumber);
        }

        // Remove DynamicHaunt cache if this was the haunt that carried it.
        if (haunt.parent is HediffWithComps hwc
            && hwc.TryGetComp<HediffComp_DynamicHaunt>() != null)
        {
            DynamicHaunts.Remove(thingIdNumber);
        }

        // Remove reverse lookup entry if this haunt owned it.
        if (haunt.pawnToDraw != null
            && SpiritToHaunt.TryGetValue(haunt.pawnToDraw.thingIDNumber, out HediffComp_Haunt cached)
            && cached == haunt)
        {
            SpiritToHaunt.Remove(haunt.pawnToDraw.thingIDNumber);
        }
    }

    public static void TryUpdateWander(int id, Vector3 drawPos)
    {
        if (Haunts.TryGetValue(id, out List<HediffComp_Haunt> haunts))
            foreach (HediffComp_Haunt haunt in haunts)
                haunt.UpdateWander(drawPos);
    }

    public static void TryDrawAt(int id, Vector3 drawPos)
    {
        if (Haunts.TryGetValue(id, out List<HediffComp_Haunt> haunts))
        {
            foreach (HediffComp_Haunt haunt in haunts)
            {
                haunt.DrawAt(drawPos);
            }
        }
    }

    public static Dictionary<Pawn, Dictionary<SkillDef, int>> Cache = new();

    public static int BoostForPawnAndSkill(Pawn p, SkillDef s)
    {
        if (!Cache.TryGetValue(p, out Dictionary<SkillDef, int> skills))
            return 0;
        return skills.TryGetValue(s, out int boost) ? boost : 0;
    }

    public static void AddToCache(Pawn p, SkillDef s, int level)
    {
        if (!Cache.ContainsKey(p))
            Cache[p] = new Dictionary<SkillDef, int>();
        Cache[p][s] = level;
    }

    public static void ClearCacheForPawn(Pawn p)
    {
        Cache.Remove(p);
    }

    public static void RebuildCacheForPawn(Pawn p)
    {
        ClearCacheForPawn(p);

        List<HediffWithComps> comps = p.health.hediffSet.hediffs.OfType<HediffWithComps>().ToList();

        if (comps.NullOrEmpty())
            return;

        Dictionary<SkillDef, int> boosts = new();

        foreach (
            HediffComp_Haunt hauntComp in comps
                .SelectMany(hediff => hediff.comps)
                .OfType<HediffComp_Haunt>()
        )
        {
            // GetSkillBoosts() is virtual — HediffComp_Echo returns
            // per-pawn entries from the echo history list.
            foreach ((SkillDef skill, int boost) in hauntComp.GetSkillBoosts())
            {
                if (!boosts.ContainsKey(skill))
                    boosts[skill] = 0;
                boosts[skill] += boost;
            }
        }

        if (boosts.Count > 0)
            Cache[p] = boosts;
    }

    /// <summary>
    /// Returns true if the given spirit pawn is already haunting any living pawn.
    /// </summary>
    public static bool IsSpiritHaunting(int spiritThingId) =>
        SpiritToHaunt.ContainsKey(spiritThingId);

    /// <summary>
    /// Returns the haunt comp for a given spirit, or null if not haunting anyone.
    /// </summary>
    public static HediffComp_Haunt GetHauntForSpirit(int spiritThingId) =>
        SpiritToHaunt.TryGetValue(spiritThingId, out HediffComp_Haunt comp) ? comp : null;

    public static void Clear()
    {
        Haunts.Clear();
        DynamicHaunts.Clear();
        SpiritToHaunt.Clear();
        Cache.Clear();
    }
}
