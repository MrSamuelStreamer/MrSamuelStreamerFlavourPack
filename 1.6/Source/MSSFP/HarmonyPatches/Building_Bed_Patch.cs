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
        action.defaultDesc = "Upgrades for this bed";
        action.icon = Textures.MSSFP_OskarianBed_Upgrade;
        action.action = () =>
        {
            Find.WindowStack.Add(new Dialog_BedUpgrade(comp));
        };

        __result = __result.AddItem(action);

        if (DebugSettings.ShowDevGizmos)
        {
            Command_Action addLevel = new();
            action.defaultLabel = "Add Point";
            action.action = () =>
            {
                comp.AddExperience(CompUpgradableBed.PointsPerLevel);
            };

            __result = __result.AddItem(addLevel);

            Command_Action add10Level = new();
            action.defaultLabel = "Add 10 Points";
            action.action = () =>
            {
                comp.AddExperience(CompUpgradableBed.PointsPerLevel * 10);
            };

            __result = __result.AddItem(add10Level);

            Command_Action reset = new();
            action.defaultLabel = "Reset bed";
            action.defaultDesc = "Completely Reset bed to defaults";
            action.action = () =>
            {
                comp.Reset(true);
            };

            __result = __result.AddItem(reset);

            Command_Action resetUpgrades = new();
            action.defaultLabel = "Reset just the bed upgrades";
            action.action = () =>
            {
                comp.Reset();
            };

            __result = __result.AddItem(add10Level);
        }
    }
}
