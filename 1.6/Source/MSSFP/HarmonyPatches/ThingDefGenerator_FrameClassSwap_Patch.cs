using HarmonyLib;
using MSSFP.Buildings;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Swap the generated Frame def's thingClass to <see cref="Frame_AICoreLoaded"/> for
/// <c>MSSFP_AICore_LoadedVariant</c>. Vanilla
/// <see cref="ThingDefGenerator_Buildings.NewFrameDef_Thing"/> hard-codes
/// <c>thingClass = typeof(Verse.Frame)</c> on the per-building Frame def — no XML knob
/// exists to override it.
///
/// Postfix rewrites the field for our one specific buildable. All other buildings keep the
/// vanilla Frame class.
/// </summary>
[HarmonyPatch(typeof(ThingDefGenerator_Buildings), "NewFrameDef_Thing")]
public static class ThingDefGenerator_FrameClassSwap_Patch
{
    [HarmonyPostfix]
    public static void Postfix(ThingDef def, ThingDef __result)
    {
        if (def == null || __result == null) return;
        if (def.defName != "MSSFP_AICore_LoadedVariant") return;
        __result.thingClass = typeof(Frame_AICoreLoaded);
    }
}
