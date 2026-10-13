using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Per raider in a hostile raid, roll <c>GoldenCubeImplantRaidChance</c> to grant
/// the <c>MSSFP_GoldenCubeImplant</c> hediff. Carrier acts as a walking golden
/// cube — colonists develop CubeInterest while the carrier is on the map.
///
/// Hooks the same <c>PostProcessSpawnedPawns</c> seam used by IncidentWorker_Raid_Patch.
/// </summary>
[HarmonyPatch(typeof(IncidentWorker_Raid))]
public static class IncidentWorker_Raid_GoldenCubeInjection_Patch
{
    [HarmonyPatch("PostProcessSpawnedPawns")]
    [HarmonyPostfix]
    private static void InjectGoldenCubeImplant(IncidentParms parms, List<Pawn> pawns)
    {
        if (!ModsConfig.AnomalyActive) return;
        if (!MSSFPMod.settings.EnableGoldenCubeImplant) return;
        if (pawns == null || pawns.Count == 0) return;

        HediffDef cubeDef = MSSFPDefOf.MSSFP_GoldenCubeImplant;
        if (cubeDef == null) return;

        // Hostile raids only — friendlies can't carry the joke.
        if (parms?.faction == null) return;
        if (!parms.faction.HostileTo(Faction.OfPlayer)) return;

        float chance = MSSFPMod.settings.GoldenCubeImplantRaidChance;
        if (chance <= 0f) return;

        foreach (Pawn pawn in pawns)
        {
            if (pawn == null || pawn.Dead) continue;
            if (!pawn.RaceProps.Humanlike) continue;
            if (pawn.health == null) continue;
            if (pawn.health.hediffSet.HasHediff(cubeDef)) continue;

            if (!Rand.Chance(chance)) continue;

            BodyPartRecord torso = pawn.health.hediffSet
                .GetNotMissingParts()
                .FirstOrDefault(p => p.def == BodyPartDefOf.Torso);
            pawn.health.AddHediff(cubeDef, torso);
            ModLog.Debug($"[GoldenCube] {pawn.LabelShort} arrived carrying a golden cube implant.");
        }
    }
}
