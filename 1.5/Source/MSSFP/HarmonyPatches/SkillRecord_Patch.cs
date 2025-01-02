using HarmonyLib;
using RimWorld;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(SkillRecord))]
public static class SkillRecord_Patch
{
    [HarmonyPatch(nameof(SkillRecord.Aptitude), MethodType.Getter)]
    [HarmonyPostfix]
    public static void AptitudeFor(SkillRecord __instance, ref int __result)
    {
        // __result += __instance.Pawn.health.hediffSet.hediffs.OfType<HediffWithComps>().SelectMany(hediff => hediff.comps).OfType<HediffComp_Haunt>().Where(comp=>comp.Pawn != null).Sum(comp => comp.AptitudeFor(__instance.def));
    }
}
