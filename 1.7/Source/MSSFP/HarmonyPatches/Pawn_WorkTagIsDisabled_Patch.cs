using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Holos cannot perform Violent, Hunting, Hauling, Cleaning, or basic manual labour
/// (ManualDumb). Projected pawns have no physical body, so menial physical work is
/// meaningless and produces job thrash (racing real haulers for the same target) and
/// range-check oscillation at door boundaries (Goto succeeds, work-toil range fails,
/// loop). Postfix on <see cref="Pawn.WorkTagIsDisabled"/> short-circuits these tags
/// to disabled. Skilled work (ManualSkilled — sculpting, crafting, etc.) and pure
/// intellectual / social work remain enabled.
/// </summary>
[HarmonyPatch(typeof(Pawn), nameof(Pawn.WorkTagIsDisabled))]
public static class Pawn_WorkTagIsDisabled_Patch
{
    private const WorkTags HoloDisabledTags =
        WorkTags.Violent
        | WorkTags.Hunting
        | WorkTags.Hauling
        | WorkTags.Cleaning
        | WorkTags.ManualDumb;

    [HarmonyPostfix]
    public static void Postfix(Pawn __instance, WorkTags w, ref bool __result)
    {
        if (__result)
            return;
        if (!MSSFPHoloUtil.IsHolo(__instance))
            return;
        if ((w & HoloDisabledTags) != 0)
            __result = true;
    }
}
