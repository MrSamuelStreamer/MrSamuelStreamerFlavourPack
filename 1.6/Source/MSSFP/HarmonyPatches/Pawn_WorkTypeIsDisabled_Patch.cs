using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Disables Hauling, Cleaning, and basic manual-labour <see cref="WorkTypeDef"/>s for
/// holo-projected pawns. Vanilla <see cref="Pawn.WorkTypeIsDisabled"/> reads a cached list
/// populated from backstory/traits/genes/etc. and does NOT consult
/// <see cref="Pawn.WorkTagIsDisabled"/>, so the sibling tag patch alone is not enough to
/// stop the work-giver scanner from offering Hauling/Cleaning jobs to holos.
///
/// Postfix matches any <see cref="WorkTypeDef"/> whose <c>workTags</c> overlap
/// <c>Hauling | Cleaning | ManualDumb</c> (in 1.6 vanilla, those tags appear on the
/// Hauling and Cleaning work types only). Skilled physical work (Construction, Mining,
/// Smithing, etc., tagged <see cref="WorkTags.ManualSkilled"/>) is unaffected.
/// </summary>
[HarmonyPatch(typeof(Pawn), nameof(Pawn.WorkTypeIsDisabled))]
public static class Pawn_WorkTypeIsDisabled_Patch
{
    private const WorkTags HoloDisabledTypeTags =
        WorkTags.Hauling | WorkTags.Cleaning | WorkTags.ManualDumb;

    [HarmonyPostfix]
    public static void Postfix(Pawn __instance, WorkTypeDef w, ref bool __result)
    {
        if (__result)
            return;
        if (w == null)
            return;
        if (!MSSFPHoloUtil.IsHolo(__instance))
            return;
        if ((w.workTags & HoloDisabledTypeTags) != 0)
            __result = true;
    }
}
