using HarmonyLib;
using MSSFP.Needs;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(Pawn_GeneTracker))]
public static class Pawn_GeneTracker_Patch
{
    [HarmonyPatch(nameof(Pawn_GeneTracker.AddGene), [typeof(Gene), typeof(bool)])]
    [HarmonyPostfix]
    public static void AddGene_Postfix(Pawn_GeneTracker __instance)
    {
        Need_GeneStealer need = __instance.pawn.needs.TryGetNeed<Need_GeneStealer>();

        need?.Notify_GeneGained();
    }

    // [HarmonyPatch(nameof(Pawn_GeneTracker.AddGene), [typeof(GeneDef), typeof(bool)])]
    // [HarmonyPrefix]
    // public static bool AddGene1_Prefix(Pawn_GeneTracker __instance)
    // {
    //     if ((__instance.pawn.health?.hediffSet?.TryGetHediff(MSSFPDefOf.MSSFP_Hediff_DRM, out Hediff hediff) ?? false) && !PawnGenerator.IsBeingGenerated(__instance.pawn))
    //     {
    //         DamageInfo dinfo = new();
    //         __instance.pawn.Kill(dinfo, hediff);
    //         return false;
    //     }
    //     return true;
    // }
    //
    // [HarmonyPatch(nameof(Pawn_GeneTracker.AddGene), [typeof(Gene), typeof(bool)])]
    // [HarmonyPrefix]
    // public static bool AddGene2_Prefix(Pawn_GeneTracker __instance)
    // {
    //     if ((__instance.pawn.health?.hediffSet?.TryGetHediff(MSSFPDefOf.MSSFP_Hediff_DRM, out Hediff hediff) ?? false) && !PawnGenerator.IsBeingGenerated(__instance.pawn))
    //     {
    //         DamageInfo dinfo = new();
    //         __instance.pawn.Kill(dinfo, hediff);
    //         return false;
    //     }
    //     return true;
    // }
    //
    // [HarmonyPatch(nameof(Pawn_GeneTracker.RemoveGene))]
    // [HarmonyPrefix]
    // public static bool RemoveGene_Prefix(Pawn_GeneTracker __instance)
    // {
    //     if ((__instance.pawn.health?.hediffSet?.TryGetHediff(MSSFPDefOf.MSSFP_Hediff_DRM, out Hediff hediff) ?? false) && !PawnGenerator.IsBeingGenerated(__instance.pawn))
    //     {
    //         __instance.pawn.Kill(null, null);
    //         return false;
    //     }
    //     return true;
    // }
}
