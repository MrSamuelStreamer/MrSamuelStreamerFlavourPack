using System.Collections.Generic;
using HarmonyLib;
using MSSFP.Comps;
using MSSFP.Dialogs;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(Building_Bed))]
public static class Building_Bed_Patch
{
    [HarmonyPatch(nameof(Building_Bed.GetGizmos))]
    [HarmonyPostfix]
    public static void GetGizmos_Postfix(Building_Bed __instance, ref IEnumerable<Gizmo> __result)
    {
        CompUpgradableBed comp = __instance.GetComp<CompUpgradableBed>();
        if (comp == null)
            return;

        Command_Action action = new();
        action.defaultLabel = "Upgrades";
        action.action = () =>
        {
            Find.WindowStack.Add(new Dialog_BedUpgrade(comp));
        };

        __result = __result.AddItem(action);
    }
}
