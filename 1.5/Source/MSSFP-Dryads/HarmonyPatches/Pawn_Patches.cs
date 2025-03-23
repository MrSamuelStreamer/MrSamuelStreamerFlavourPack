using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.Dryads.HarmonyPatches;

[HarmonyPatch(typeof(Pawn))]
public static class Pawn_Patches
{
    /// <summary>
    /// Trigger froggomancer rescue randomly on pawn downed
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPatch(nameof(Pawn.Notify_Downed))]
    [HarmonyPostfix]
    public static void Pawn_Notify_Downed(Pawn __instance)
    {
        if (
            !__instance.Spawned
            || __instance.ageTracker.AgeBiologicalYears < 2
            || __instance.Map == null
            || __instance.Faction == null
            || __instance.RaceProps == null
            || !__instance.Faction.IsPlayer
            || !__instance.RaceProps.Humanlike
            || Rand.Chance(0.9f)
        )
            return;
        IncidentParms iParams = new() { target = __instance.Map };
        Find.Storyteller.TryFire(new FiringIncident(MSSFPDryadDefOf.MSS_FroggomancerRescue, null, iParams), false);
    }
}
