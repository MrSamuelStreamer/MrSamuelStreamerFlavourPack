using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Holos cannot perform Violent or Hunting work (no combat, no killing). Postfix on
/// <see cref="Pawn.WorkTagIsDisabled"/> short-circuits these tags to disabled.
/// </summary>
[HarmonyPatch(typeof(Pawn), nameof(Pawn.WorkTagIsDisabled))]
public static class Pawn_WorkTagIsDisabled_Patch
{
    [HarmonyPostfix]
    public static void Postfix(Pawn __instance, WorkTags w, ref bool __result)
    {
        if (__result)
            return;
        if (!MSSFPHoloUtil.IsHolo(__instance))
            return;
        if ((w & (WorkTags.Violent | WorkTags.Hunting)) != 0)
            __result = true;
    }
}
