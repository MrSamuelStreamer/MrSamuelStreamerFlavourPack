using System.Linq;
using HarmonyLib;
using Outposts;
using RimWorld;
using Verse;

namespace MSSFP.VOE;

[HarmonyPatch(typeof(Outpost))]
public static class Outpost_Patch
{
    [HarmonyPatch(nameof(Outpost.AddPawn))]
    [HarmonyPostfix]
    public static void AddPawn(Outpost __instance, Pawn pawn, ref bool __result)
    {
        //Unassign roles from pawn when put into outpost
        if(!__result) return;

        foreach (Ideo ideo in Faction.OfPlayer.ideos.AllIdeos)
        {
            foreach (Precept_Role precept in ideo.PreceptsListForReading.OfType<Precept_Role>().Where(p=>p.IsAssigned(pawn)))
            {
                precept.Unassign(pawn, true);
            }
        }
    }
}
