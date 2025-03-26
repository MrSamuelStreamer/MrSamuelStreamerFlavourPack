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
}
