using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MSSFP.ModExtensions;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(AggressiveAnimalIncidentUtility))]
public static class AggressiveAnimalIncidentUtility_Patch
{
    [HarmonyPatch("TryGetAnimalFromList")]
    [HarmonyPrefix]
    public static void TryGetAnimalFromList_Patch(ref List<PawnKindDef> animals)
    {
        if (MSSFPMod.settings.EnableFroggeIncidents)
            return;
        animals = animals.Except(animals.Where(p => p.HasModExtension<FroggeModExtension>())).ToList();
    }
}
