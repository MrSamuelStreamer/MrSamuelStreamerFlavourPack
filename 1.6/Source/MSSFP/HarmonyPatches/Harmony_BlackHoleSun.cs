using HarmonyLib;
using MSSFP.Comps.World;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Manually-toggleable patches supporting the black hole feature. Enabled/disabled
/// from <see cref="MSSFPMod"/> and the Black Hole settings tab in the same style
/// as <see cref="MSSFPMod.ToggleSettlementDefeatPatch"/>.
///
/// Three effects while active:
///   1. Suppress vanilla sun quad when the black hole condition is running.
///   2. Clean up stale render helper on save-load (MonoBehaviour survives Game teardown).
///   3. Dim planet globe by scaling the sun-light direction magnitude.
/// </summary>
public static class Harmony_BlackHoleSun
{
    private static bool _patched;

    public static void Toggle(Harmony harmony, bool enable)
    {
        if (harmony == null) return;
        if (enable == _patched) return;

        var hideSunOriginal   = AccessTools.Method(typeof(WorldDrawLayerBase), "Render");
        var hideSunPrefix     = AccessTools.Method(typeof(Harmony_BlackHoleSun), nameof(HideSun_Prefix));

        var loadGameOriginal  = AccessTools.Method(typeof(Game), "LoadGame");
        var loadGamePrefix    = AccessTools.Method(typeof(Harmony_BlackHoleSun), nameof(LoadGame_Prefix));

        var dimGlobalOriginal = AccessTools.Method(typeof(WorldRendererUtility),
                                    nameof(WorldRendererUtility.UpdateGlobalShadersParams));
        var dimGlobalPostfix  = AccessTools.Method(typeof(Harmony_BlackHoleSun), nameof(DimPlanet_Postfix));

        if (enable)
        {
            if (hideSunOriginal   != null) harmony.Patch(hideSunOriginal,   prefix:  new HarmonyMethod(hideSunPrefix));
            if (loadGameOriginal  != null) harmony.Patch(loadGameOriginal,  prefix:  new HarmonyMethod(loadGamePrefix));
            if (dimGlobalOriginal != null) harmony.Patch(dimGlobalOriginal, postfix: new HarmonyMethod(dimGlobalPostfix));
        }
        else
        {
            if (hideSunOriginal   != null) harmony.Unpatch(hideSunOriginal,   hideSunPrefix);
            if (loadGameOriginal  != null) harmony.Unpatch(loadGameOriginal,  loadGamePrefix);
            if (dimGlobalOriginal != null) harmony.Unpatch(dimGlobalOriginal, dimGlobalPostfix);
        }

        _patched = enable;
    }

    public static bool HideSun_Prefix(WorldDrawLayerBase __instance)
    {
        if (__instance is GlobalDrawLayer_Sun && GameCondition_BlackHole.IsActive)
            return false;
        return true;
    }

    public static void LoadGame_Prefix() => GameCondition_BlackHole.DeactivateBlackHole();

    public static void DimPlanet_Postfix()
    {
        if (!GameCondition_BlackHole.IsActive) return;
        Vector3 dir = Shader.GetGlobalVector(ShaderPropertyIDs.PlanetSunLightDirection);
        Shader.SetGlobalVector(ShaderPropertyIDs.PlanetSunLightDirection,
                               dir * GameCondition_BlackHole.WorldLightFactor);
    }
}
