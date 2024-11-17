using System.Collections.Generic;

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
        if(!Haunts.TryGetValue(thingIdNumber, out List<HediffComp_Haunt> mods))
        {
            return;
        }

        mods.Remove(haunt);

        if (mods.Count == 0)
        {
            Haunts.Remove(thingIdNumber);
        }
    }
}
