using HarmonyLib;
using RimWorld;
using VEE.PurpleEvents;
using Verse;

namespace MSSFP.VEE;

[HarmonyPatch(typeof(PlanetEvent))]
public static class PlanetEvent_Patch
{

    [HarmonyPatch(nameof(PlanetEvent.End))]
    [HarmonyPostfix]
    public static void Postfix(PlanetEvent __instance)
    {
        if(__instance is not IceAge iceAge) return;
        DelayedIncidentGameComponent gc = Current.Game.GetComponent<DelayedIncidentGameComponent>();

        gc?.AddNewDelayedIncidentDef(MSSFVEEPDefOf.MSS_NuclearFallout, null, GenDate.TicksPerDay);
    }
}
