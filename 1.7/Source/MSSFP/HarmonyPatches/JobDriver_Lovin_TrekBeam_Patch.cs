using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MSSFP.Comps.Map;
using RimWorld;
using Verse;
using Verse.AI;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// A pawn with the Trek adulthood backstory may be beamed away by "their people"
/// shortly after lovin' (TrekBeamerMapComponent does the delayed beam-out and letter).
/// Each partner runs their own lovin' job, so only the job's own pawn is checked.
/// </summary>
[HarmonyPatch(typeof(JobDriver_Lovin), "MakeNewToils")]
public static class JobDriver_Lovin_TrekBeam_Patch
{
    private const float BeamAwayChance = 0.25f;

    [HarmonyPostfix]
    public static void Postfix(JobDriver_Lovin __instance, ref IEnumerable<Toil> __result)
    {
        List<Toil> toils = __result.ToList();
        __result = toils;
        if (toils.NullOrEmpty())
            return;

        Pawn pawn = __instance.pawn;
        toils.Last().AddFinishAction(() => TryQueueBeamAway(pawn));
    }

    private static void TryQueueBeamAway(Pawn pawn)
    {
        if (!MSSFPMod.settings.EnableTrekBeamers)
            return;
        if (pawn?.story?.Adulthood != MSSFPDefOf.MSSFP_Trek || !pawn.Spawned)
            return;

        TrekBeamerMapComponent beamer = pawn.Map.GetComponent<TrekBeamerMapComponent>();
        if (beamer == null || beamer.PawnsToBeam.Any(b => b.pawn == pawn))
            return;

        if (Rand.Chance(BeamAwayChance))
            beamer.BeamAwayPawn(pawn);
    }
}
