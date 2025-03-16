using System.Linq;
using HarmonyLib;
using Outposts;
using RimWorld;
using Verse;

namespace MSSFP.VOE;

[HarmonyPatch(typeof(Outpost))]
public static class Outpost_Patch
{
    public static GeneDef AsexualReproduction => DefDatabase<GeneDef>.GetNamed("AG_AsexualFission");

    [HarmonyPatch(nameof(Outpost.AddPawn))]
    [HarmonyPostfix]
    public static void AddPawn(Outpost __instance, Pawn pawn, ref bool __result)
    {
        //Unassign roles from pawn when put into outpost
        if (!__result)
            return;

        foreach (Ideo ideo in Faction.OfPlayer.ideos.AllIdeos)
        {
            foreach (Precept_Role precept in ideo.PreceptsListForReading.OfType<Precept_Role>().Where(p => p.IsAssigned(pawn)))
            {
                precept.Unassign(pawn, true);
            }
        }

        OutpostReproWorldComponent comp = Find.World.GetComponent<OutpostReproWorldComponent>();

        if ((pawn.genes?.HasActiveGene(AsexualReproduction) ?? false))
        {
            comp.Notify_PawnAdded(__instance, pawn);
        }
    }

    [HarmonyPatch(nameof(Outpost.RemovePawn))]
    [HarmonyPostfix]
    public static void RemovePawn(Outpost __instance, Pawn p)
    {
        OutpostReproWorldComponent comp = Find.World.GetComponent<OutpostReproWorldComponent>();
        comp.Notify_PawnRemoved(p);
    }

    [HarmonyPatch(nameof(Outpost.PostRemove))]
    [HarmonyPostfix]
    public static void PostRemove(Outpost __instance)
    {
        OutpostReproWorldComponent comp = Find.World.GetComponent<OutpostReproWorldComponent>();
        comp.Notify_OutpostRemoved(__instance);
    }
}
