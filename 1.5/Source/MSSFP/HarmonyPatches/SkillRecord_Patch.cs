using System.Linq;
using HarmonyLib;
using MSSFP.Hediffs;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Aptitudes from haunts - currently not working
/// </summary>
[HarmonyPatch(typeof(SkillRecord))]
public static class SkillRecord_Patch
{
    // [HarmonyPatch(nameof(SkillRecord.Aptitude), MethodType.Getter)]
    // [HarmonyPostfix]
    // public static void AptitudeFor(SkillRecord __instance, ref int __result)
    // {
    //     return; // TODO: Fix it
    //     if (!__instance.Pawn.health.hediffSet.hediffs.Any(h => h is HediffWithComps hwc && hwc.comps.Any(c => c is HediffComp_Haunt)))
    //         return;
    //
    //     __result += __instance
    //         .Pawn.health.hediffSet.hediffs.OfType<HediffWithComps>()
    //         .SelectMany(hediff => hediff.comps)
    //         .OfType<HediffComp_Haunt>()
    //         .Where(comp => comp.skillToBoost == __instance.def)
    //         .Sum(comp => comp.SkillBoostLevel);
    // }
}
