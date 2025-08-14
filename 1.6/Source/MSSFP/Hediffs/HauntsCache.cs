using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Hediffs;

public static class HauntsCache
{
    public static Dictionary<int, List<HediffComp_Haunt>> Haunts = new();

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
        if (!Cache.ContainsKey(p))
            return 0;
        return !Cache[p].ContainsKey(s) ? 0 : Cache[p][s];
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

        foreach (
            HediffComp_Haunt hediffCompHaunt in comps
                .SelectMany(hediff => hediff.comps)
                .OfType<HediffComp_Haunt>()
        )
        {
            if (!Cache[p].ContainsKey(hediffCompHaunt.skillToBoost))
                Cache[p][hediffCompHaunt.skillToBoost] = 0;
            Cache[p][hediffCompHaunt.skillToBoost] += hediffCompHaunt.SkillBoostLevel;
        }
    }
}
