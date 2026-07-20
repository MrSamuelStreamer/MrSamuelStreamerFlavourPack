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

        if (!ViewerStructureSelector.Roll(__instance.ID, ViewerStructureSelector.SettlementSeedSalt))
            return;

        __result = MSSFPStructureDefOf.MSSFP_ViewerStructureSettlement;
        ModLog.Log($"Settlement {__instance.Label} will generate a viewer structure instead of a standard base");
    }
}
