using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Chance for a raid after a skylantern
/// </summary>
[HarmonyPatch(typeof(RitualOutcomeEffectWorker_Skylantern))]
public static class RitualOutcomeEffectWorker_Skylantern_Patch
{
    [HarmonyPatch("ApplyExtraOutcome")]
    [HarmonyPostfix]
    public static void ApplyExtraOutcome_Patch(
        Dictionary<Pawn, int> totalPresence,
        LordJob_Ritual jobRitual,
        RitualOutcomePossibility outcome,
        ref string extraOutcomeDesc,
        ref LookTargets letterLookTargets
    )
    {
        if (outcome.Positive && Rand.Chance(0.1f))
            return;

        if (!outcome.Positive && Rand.Chance(0.5f))
            return;

        IncidentParms parms = new() { target = jobRitual.Map };

        if (!MSSFPDefOf.MSSFP_RaidEnemy_Skylantern.Worker.TryExecute(parms))
            return;

        string outstr = "MSS_FP_RitualOutcomeExtraDesc_SkylanternRaid".Translate(
            parms.faction.NameColored
        );

        extraOutcomeDesc = extraOutcomeDesc == null ? outstr : $"{extraOutcomeDesc}\n\n{outstr}";
    }
}
