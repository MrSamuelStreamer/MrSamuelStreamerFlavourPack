using HarmonyLib;
using KCSG;
using MSSFP.VFE.Structures;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.VFE.HarmonyPatches;

/// <summary>
/// Occasionally swaps a faction settlement's map generator for one that builds a viewer-submitted
/// structure instead of a procedural base.
///
/// This targets the same getter KCSG patches, and runs after it, so a faction that genuinely opts
/// into KCSG generation via CustomGenOption keeps that behaviour untouched.
/// </summary>
[HarmonyPatch(typeof(Settlement), nameof(Settlement.MapGeneratorDef), MethodType.Getter)]
[HarmonyAfter("Kikohi.KCSG")]
public static class Settlement_MapGeneratorDef_Patch
{
    public static void Postfix(Settlement __instance, ref MapGeneratorDef __result)
    {
        if (__instance?.Faction == null || __instance.Faction == Faction.OfPlayer)
            return;

        // Never override a deliberate KCSG faction, or anything that already diverged from
        // vanilla settlement generation. If another mod got there first, defer to it.
        if (__instance.Faction.def.HasModExtension<CustomGenOption>())
            return;
        if (__result != MSSFPStructureDefOf.Base_Faction)
            return;

        // Settlement replacement only ever draws from standalone (whole-map) layouts, and those are
        // paused (see ViewerStructureSelector.Eligible) after a settlement generated with zero
        // defenders — its faction had no PawnGroupMakers for groupKind=Settlement, so the base read
        // as already-defeated the moment the caravan entered. TrySwapToViewerStructure below is kept
        // intact but unused so re-enabling later, once that gap is fixed, is a one-line call.
    }

    /// <summary>
    /// Currently unused — see the pause note in Postfix. Left in place, not deleted, so re-enabling
    /// is a one-line call once standalone layouts have a defender fallback.
    /// </summary>
    private static void TrySwapToViewerStructure(Settlement settlement, ref MapGeneratorDef result)
    {
        if (!ViewerStructureSelector.Roll(settlement.ID, ViewerStructureSelector.SettlementSeedSalt))
            return;

        result = MSSFPStructureDefOf.MSSFP_ViewerStructureSettlement;
        ModLog.Log($"Settlement {settlement.Label} will generate a viewer structure instead of a standard base");
    }
}
