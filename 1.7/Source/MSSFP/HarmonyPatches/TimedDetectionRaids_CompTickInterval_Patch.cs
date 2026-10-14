using System.Linq;
using MSSFP.Questing;
using Verse;

namespace MSSFP.HarmonyPatches;

using HarmonyLib;
using RimWorld.Planet;

[HarmonyPatch(typeof(TimedDetectionRaids), nameof(TimedDetectionRaids.CompTickInterval))]
public static class TimedDetectionRaids_CompTickInterval_Patch
{
    [HarmonyPrefix]
    static bool Prefix(TimedDetectionRaids __instance)
    {
        if (__instance.parent is not MapParent { HasMap: true } mapParent) return true;
        foreach (ScenPart_Pursuers scenPart in Find.Scenario.AllParts.OfType<ScenPart_Pursuers>())
        {
            if (scenPart.MapIsSafeNow(mapParent.Map))
            {
                // Skip original method if any active Pursuers scenario part considers the map safe
                return false;
            }
        }
        return true;
    }
}
