using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.IS.HarmonyPatches;

/// <summary>
/// Injects the biocoded implant variants during implied-def generation, the same road
/// vanilla drives for corpse, meat, techprint and neurotrainer defs.
/// </summary>
[HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
public static class DefGenerator_Patch
{
    [HarmonyPostfix]
    public static void Postfix(bool hotReload)
    {
        foreach (ThingDef d in BiocodedImplantDefs.ImpliedDefs(hotReload))
        {
            DefGenerator.AddImpliedDef(d, hotReload);
        }
    }
}
