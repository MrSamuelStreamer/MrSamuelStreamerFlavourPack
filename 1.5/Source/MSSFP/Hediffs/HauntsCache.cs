using System.Collections.Generic;
using UnityEngine;

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
}
