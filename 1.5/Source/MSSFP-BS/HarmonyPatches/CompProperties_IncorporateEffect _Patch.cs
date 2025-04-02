using BigAndSmall;
using HarmonyLib;
using Verse;

namespace MSSFP.BS.HarmonyPatches;

[HarmonyPatch(typeof(CompProperties_IncorporateEffect))]
public static class CompProperties_IncorporateEffect__Patch
{
    public static HediffDef hediff => DefDatabase<HediffDef>.GetNamed("MSS_Bigger");
    public static FloatRange bodySizeMultiplierRange => new(.05f, .2f);

    [HarmonyPatch(nameof(CompProperties_IncorporateEffect.RemoveGenesOverLimit))]
    [HarmonyPrefix]
    public static bool RemoveGenesOverLimit(CompProperties_IncorporateEffect __instance, ref bool __result)
    {
        __result = false;
        return !MSSFPMod.settings.DisableBSIncorporateGeneLimit;
    }

    [HarmonyPatch(nameof(CompProperties_IncorporateEffect.IncorporateGenes))]
    [HarmonyPostfix]
    public static void IncorporateGenes_Patch(Pawn pawn)
    {
        Hediff_Bigger h = (Hediff_Bigger)pawn.health.GetOrAddHediff(hediff);
        if (h == null)
            return;
        h.BodySizeMultiplier += bodySizeMultiplierRange.RandomInRange;
        h.SizeChanged = true;
        HumanoidPawnScaler.GetCache(pawn, forceRefresh: true, scheduleForce: 10);
    }
}
