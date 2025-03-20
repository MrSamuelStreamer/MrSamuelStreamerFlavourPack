using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.Dryads.HarmonyPatches;

[HarmonyPatch(typeof(Dialog_ChangeDryadCaste))]
public static class Dialog_ChangeDryadCaste_Patch
{
    public static Lazy<FieldInfo> treeConnection = new Lazy<FieldInfo>(() => AccessTools.Field(typeof(Dialog_ChangeDryadCaste), "treeConnection"));
    public static Lazy<FieldInfo> allDryadModes = new Lazy<FieldInfo>(() => AccessTools.Field(typeof(Dialog_ChangeDryadCaste), "allDryadModes"));

    [HarmonyPatch(nameof(Dialog_ChangeDryadCaste.PreOpen))]
    [HarmonyPrefix]
    public static void Dialog_ChangeDryadCaste_PreOpen(Dialog_ChangeDryadCaste __instance)
    {
        CompTreeConnection comp = (CompTreeConnection)treeConnection.Value.GetValue(__instance);
        List<GauranlenTreeModeDef> dryads = (List<GauranlenTreeModeDef>)allDryadModes.Value.GetValue(__instance);

        if (comp is null || dryads.NullOrEmpty())
            return;

        if (comp.parent.def.defName != "MSSFP_Plant_TreeFroganlen")
        {
            dryads = dryads.Where(d => d.defName != "MSS_FP_Froggeling").ToList();
        }

        allDryadModes.Value.SetValue(__instance, dryads);
    }
}
