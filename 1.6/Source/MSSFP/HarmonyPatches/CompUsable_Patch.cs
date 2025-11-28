using HarmonyLib;
using MSSFP.ModExtensions;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

[HarmonyPatch(typeof(CompUsable))]
public static class CompUsable_Patch
{

    [HarmonyPatch(nameof(CompUsable.UsedBy))]
    [HarmonyPostfix]
    public static void UsedBy_Patch(CompUsable __instance, Pawn p)
    {
        if (!(__instance?.parent?.def?.HasModExtension<OnUseDefModExtension>() ?? false))
        {
            return;
        }

        OnUseDefModExtension ext = __instance.parent.def.GetModExtension<OnUseDefModExtension>();
        ext.TrySpawnThing(p, out _);
        ext.TryGiveHediff(p, out _);
    }
}
