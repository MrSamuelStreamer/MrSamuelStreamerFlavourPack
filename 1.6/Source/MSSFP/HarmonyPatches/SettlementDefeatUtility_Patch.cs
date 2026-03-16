using System;
using System.Reflection;
using HarmonyLib;
using MSSFP.Utils;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.HarmonyPatches;


// [HarmonyPatch(typeof(SettlementDefeatUtility))]
public static class SettlementDefeatUtility_Patch
{
    public static Lazy<MethodInfo> HasAnyOtherBase = new(() => AccessTools.Method(typeof(SettlementDefeatUtility), "HasAnyOtherBase"));

    // [HarmonyPatch(nameof(SettlementDefeatUtility.CheckDefeated))]
    // [HarmonyPrefix]
    public static void CheckDefeated_Prefix(Settlement factionBase)
    {
        if(factionBase?.Faction?.IsPlayer ?? false) return;
        bool retval = (bool)HasAnyOtherBase.Value.Invoke(null, [factionBase]);
        if (!retval)
        {
            Find.SignalManager.SendSignal(new Signal(Signals.MSS_FactionDefeated, factionBase?.Faction));
        }
    }
}
