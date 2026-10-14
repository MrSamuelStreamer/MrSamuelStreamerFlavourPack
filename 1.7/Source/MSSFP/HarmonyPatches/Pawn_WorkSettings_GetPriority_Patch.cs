using HarmonyLib;
using MSSFP.Holo;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Forces priority 0 for Hauling, Cleaning, and basic manual-labour
/// <see cref="WorkTypeDef"/>s on holo-projected pawns. This is the gate that
/// <see cref="Pawn_WorkSettings.CacheWorkGiversInOrder"/> uses (it skips work types
/// whose priority is 0), so patching <see cref="Pawn.WorkTypeIsDisabled"/> alone is
/// not enough — saved priorities from before this patch existed would still be
/// nonzero, and the scanner would still issue Hauling/Cleaning jobs.
///
/// Returning 0 here also makes <see cref="Pawn_WorkSettings.WorkIsActive"/> return
/// false (it is <c>GetPriority(w) &gt; 0</c>) and keeps the Work tab consistent.
/// Fast path: bitmask test on <c>w.workTags</c> short-circuits before reading the
/// private <c>pawn</c> field, so non-matching work types pay only one branch.
/// </summary>
[HarmonyPatch(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.GetPriority))]
public static class Pawn_WorkSettings_GetPriority_Patch
{
    private const WorkTags HoloDisabledTypeTags =
        WorkTags.Hauling | WorkTags.Cleaning | WorkTags.ManualDumb;

    private static readonly AccessTools.FieldRef<Pawn_WorkSettings, Pawn> PawnRef =
        AccessTools.FieldRefAccess<Pawn_WorkSettings, Pawn>("pawn");

    [HarmonyPostfix]
    public static void Postfix(Pawn_WorkSettings __instance, WorkTypeDef w, ref int __result)
    {
        if (__result <= 0)
            return;
        if (w == null)
            return;
        if ((w.workTags & HoloDisabledTypeTags) == 0)
            return;
        Pawn p = PawnRef(__instance);
        if (p == null)
            return;
        if (!MSSFPHoloUtil.IsHolo(p))
            return;
        __result = 0;
    }
}
