using HarmonyLib;
using MSSFP.Utils;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(LordJob_Ritual_ChildBirth))]
public static class LordJob_Ritual_ChildBirth_Patch
{
    [HarmonyPatch("RitualFinished")]
    [HarmonyPostfix]
    public static void RitualFinished_Patch()
    {
        Find.SignalManager.SendSignal(new Signal(Signals.MSS_BabyAddedToFaction));
    }
}
