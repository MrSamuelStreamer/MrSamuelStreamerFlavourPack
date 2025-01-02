using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MSSFP.ModExtensions;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(PreceptWorker_Relic))]
public static class PreceptWorker_Relic_Patch
{
    [HarmonyPatch(nameof(PreceptWorker_Relic.ThingDefs), MethodType.Getter)]
    [HarmonyPrefix]
    public static bool ThingDefsGetter(ref IEnumerable<PreceptThingChance> __result)
    {
        if (!MSSFPMod.settings.overrideRelicPool) return true;

        __result = DefDatabase<ThingDef>.AllDefsListForReading.Where(thing => thing.HasModExtension<RelicModExtension>() && thing.GetModExtension<RelicModExtension>().chance > 0)
            .Select(thing => new PreceptThingChance { def = thing, chance = thing.GetModExtension<RelicModExtension>().chance });

        return false;
    }
}
