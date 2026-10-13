using HarmonyLib;
using MSSFP.Utils;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.HarmonyPatches;


// [HarmonyPatch(typeof(SettlementDefeatUtility))]
public static class SettlementDefeatUtility_Patch
{
    // Captures whether the faction was already defeated *before* vanilla's
    // CheckDefeated body runs. Vanilla only flips Faction.defeated to true inside
    // that body, so a prefix can never observe the transition — checking here and
    // comparing against the postfix's value is what lets us fire the signal exactly
    // once, on the call where the faction actually becomes defeated.
    // [HarmonyPatch(nameof(SettlementDefeatUtility.CheckDefeated))]
    // [HarmonyPrefix]
    public static void CheckDefeated_Prefix(Settlement factionBase, out bool __state)
    {
        __state = factionBase?.Faction?.defeated ?? false;
    }

    // [HarmonyPatch(nameof(SettlementDefeatUtility.CheckDefeated))]
    // [HarmonyPostfix]
    public static void CheckDefeated_Postfix(Settlement factionBase, bool __state)
    {
        if (factionBase?.Faction?.IsPlayer ?? false) return;

        bool wasDefeated = __state;
        bool isDefeated = factionBase?.Faction?.defeated ?? false;
        if (!wasDefeated && isDefeated)
        {
            Find.SignalManager.SendSignal(new Signal(Signals.MSS_FactionDefeated, factionBase?.Faction));
        }
    }
}
