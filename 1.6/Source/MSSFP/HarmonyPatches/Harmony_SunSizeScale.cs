using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MSSFP.Comps.World;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// Independent sun-size-scale feature: gradually enlarges the vanilla sun disc over
/// in-game time using the same growth-curve shape as
/// <see cref="GameCondition_BlackHole.GrowthFactor"/> but with its own settings.
///
/// Intercepts <see cref="WorldDrawLayerBase.Render"/> for <see cref="GlobalDrawLayer_Sun"/>
/// and re-issues the draw calls with a scaled world matrix. The black hole sun-hide
/// patch always wins: if <see cref="GameCondition_BlackHole.IsActive"/> is true this
/// patch no-ops so the vanilla layer's draw is fully suppressed rather than both
/// features fighting over the same frame.
/// </summary>
public static class Harmony_SunSizeScale
{
    private static bool _patched;
    private static int _startTick = -1;

    private static FieldInfo _subMeshesField;

    /// <summary>
    /// Clears the growth anchor so the sun starts over at 1x on the next render.
    /// _startTick is a plain static, not scribed, so it must be reset explicitly on
    /// every load/new-game (see <see cref="GameLoad_Patch"/>) — otherwise switching
    /// saves in the same session carries the previous colony's elapsed-time anchor
    /// into the new one, and the sun can render already scaled up on day one.
    /// </summary>
    public static void ResetStartTick() => _startTick = -1;

    public static void Toggle(Harmony harmony, bool enable)
    {
        if (harmony == null) return;
        if (enable == _patched) return;

        var renderOriginal = AccessTools.Method(typeof(WorldDrawLayerBase), "Render");
        var renderPrefix   = AccessTools.Method(typeof(Harmony_SunSizeScale), nameof(Render_Prefix));

        if (enable)
        {
            if (renderOriginal != null)
                harmony.Patch(renderOriginal, prefix: new HarmonyMethod(renderPrefix));
        }
        else
        {
            if (renderOriginal != null)
                harmony.Unpatch(renderOriginal, renderPrefix);
        }

        _patched = enable;
    }

    /// <summary>
    /// Same shape as <see cref="GameCondition_BlackHole.GrowthFactor"/>: area doublings
    /// per year → radius multiplied by sqrt(2) per year at rate=1. Uses TicksGame so
    /// growth pauses when the game pauses.
    /// </summary>
    public static float GrowthFactor
    {
        get
        {
            Settings s = MSSFPMod.settings;
            if (s == null || !s.SunSizeScaleEnabled || Find.TickManager == null) return 1f;
            if (_startTick < 0) _startTick = Find.TickManager.TicksGame;
            float years = (Find.TickManager.TicksGame - _startTick) / (float)GenDate.TicksPerYear;
            if (years <= 0f) return 1f;
            return Mathf.Min(Mathf.Pow(2f, 0.5f * years * s.SunSizeScaleRate), s.SunSizeScaleMax);
        }
    }

    public static bool Render_Prefix(WorldDrawLayerBase __instance)
    {
        if (!(__instance is GlobalDrawLayer_Sun)) return true;

        Settings s = MSSFPMod.settings;
        if (s == null || !s.SunSizeScaleEnabled) return true;

        // Black hole hide patch always wins.
        if (GameCondition_BlackHole.IsActive) return true;

        float scale = GrowthFactor;
        if (scale <= 1.0001f) return true;

        _subMeshesField ??= AccessTools.Field(typeof(WorldDrawLayerBase), "subMeshes");
        if (_subMeshesField == null)
        {
            // Field shape unknown on this RimWorld build — let vanilla draw at native size.
            return true;
        }

        var subMeshes = _subMeshesField.GetValue(__instance) as List<LayerSubMesh>;
        if (subMeshes == null || subMeshes.Count == 0) return true;

        // Scale around origin. The sun quad lives at sunDir * 20f from origin, so a
        // uniform origin-scale scales both size and radial position — visually reads
        // as the sun growing (and drifting slightly closer), which is the intended effect.
        Matrix4x4 matrix = Matrix4x4.Scale(new Vector3(scale, scale, scale));
        int layer = WorldCameraManager.WorldSkyboxLayer;

        foreach (LayerSubMesh sm in subMeshes)
        {
            if (sm?.mesh == null || sm.material == null) continue;
            Graphics.DrawMesh(sm.mesh, matrix, sm.material, layer);
        }

        return false;
    }
}
