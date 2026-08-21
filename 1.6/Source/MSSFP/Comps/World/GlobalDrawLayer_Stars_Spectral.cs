using System.Collections;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.Comps.World;

/// <summary>
/// Drop-in replacement for <see cref="GlobalDrawLayer_Stars"/> that renders each star
/// with a colour drawn from a realistic stellar spectral-type distribution and, while
/// the black hole condition is active, applies gravitational lensing.
///
/// When <see cref="Settings.BlackHoleEnabled"/> is false, falls back to
/// vanilla-identical behaviour (flat white stars, no lensing, sun-proximity shrinkage).
/// </summary>
[StaticConstructorOnStartup]
public class GlobalDrawLayer_Stars_Spectral : WorldDrawLayerBase
{
    private bool         calculatedForStaticRotation;
    private PlanetTile   calculatedForStartingTile = PlanetTile.Invalid;
    private bool         calculatedForBlackHoleActive;
    private bool         calculatedForFeatureEnabled;

    private const float            DistanceToStars = 20f;
    private static readonly FloatRange StarsDrawSize = new FloatRange(1f, 3.8f);

    private const int StarsCount = 2500;

    protected override int       RenderLayer => WorldCameraManager.WorldSkyboxLayer;
    private           bool       UseStaticRotation => Current.ProgramState == ProgramState.Entry;

    protected override Quaternion Rotation
    {
        get
        {
            if (UseStaticRotation)
                return Quaternion.identity;
            return Quaternion.LookRotation(GenCelestial.CurSunPositionInWorldSpace());
        }
    }

    public override bool ShouldRegenerate
    {
        get
        {
            bool featureEnabled = MSSFPMod.settings != null && MSSFPMod.settings.BlackHoleEnabled;
            if (featureEnabled != calculatedForFeatureEnabled)
                return true;
            if (GameCondition_BlackHole.IsActive != calculatedForBlackHoleActive)
                return true;
            if (GameCondition_BlackHole.IsActive && GrowthBucket != calculatedForGrowthBucket)
                return true;
            if (!base.ShouldRegenerate &&
                (Find.GameInitData == null ||
                 !(Find.GameInitData.startingTile != calculatedForStartingTile)))
                return UseStaticRotation != calculatedForStaticRotation;
            return true;
        }
    }

    // ── Spectral material cache ──────────────────────────────────────────
    private static Material[] spectralMats;

    private static readonly (Color col, float cdf)[] SpectralCDF =
    {
        (new Color(0.64f, 0.74f, 1.00f), 0.001f),
        (new Color(0.72f, 0.84f, 1.00f), 0.005f),
        (new Color(0.93f, 0.95f, 1.00f), 0.020f),
        (new Color(1.00f, 0.98f, 0.84f), 0.070f),
        (new Color(1.00f, 0.94f, 0.68f), 0.190f),
        (new Color(1.00f, 0.76f, 0.42f), 0.430f),
        (new Color(1.00f, 0.54f, 0.22f), 1.000f),
    };

    static GlobalDrawLayer_Stars_Spectral()
    {
        GetOrBuildSpectralMats();
    }

    private static Material[] GetOrBuildSpectralMats()
    {
        if (spectralMats != null) return spectralMats;

        spectralMats = new Material[SpectralCDF.Length];
        for (int i = 0; i < SpectralCDF.Length; i++)
        {
            var mat = new Material(WorldMaterials.Stars);
            mat.color = SpectralCDF[i].col;
            spectralMats[i] = mat;
        }
        return spectralMats;
    }

    private static int PickSpectralIndex(float t)
    {
        for (int i = 0; i < SpectralCDF.Length; i++)
            if (t <= SpectralCDF[i].cdf) return i;
        return SpectralCDF.Length - 1;
    }

    // ── Lensing constants ────────────────────────────────────────────────
    private const float VpRadiusToTheta = 1.1547f;
    private const float SunVpRadius     = 0.3248f;
    private const float LensRadiusBase  = 0.20f;
    private const float LensStrength    = 2.8f;
    private const float LensGrowthCap   = 1.5f;

    private static int GrowthBucket =>
        Mathf.RoundToInt(Mathf.Log(GameCondition_BlackHole.GrowthFactor) / Mathf.Log(1.05f));

    private int calculatedForGrowthBucket = -1;

    // ── Regenerate ───────────────────────────────────────────────────────
    public override IEnumerable Regenerate()
    {
        foreach (object o in base.Regenerate()) yield return o;

        bool featureEnabled = MSSFPMod.settings != null && MSSFPMod.settings.BlackHoleEnabled;

        Material[] mats;
        if (featureEnabled)
        {
            mats = GetOrBuildSpectralMats();
        }
        else
        {
            // Vanilla-identical fallback — flat white material, no lensing, sun-proximity shrinkage.
            mats = new[] { WorldMaterials.Stars };
        }

        float growthFactor  = featureEnabled ? GameCondition_BlackHole.GrowthFactor : 1f;
        float lensDiscScale = BHRenderHelper.BHDiscScaleBase * Mathf.Min(growthFactor, LensGrowthCap);
        float horizonTheta  = SunVpRadius * lensDiscScale * 0.22f * VpRadiusToTheta;
        float shadowTheta   = horizonTheta * 0.87f;
        float lensRadius    = LensRadiusBase * (lensDiscScale / BHRenderHelper.BHDiscScaleBase);

        bool bhActive = featureEnabled && GameCondition_BlackHole.IsActive;

        Rand.PushState();
        Rand.Seed = Find.World.info.Seed;

        for (int i = 0; i < StarsCount; i++)
        {
            Vector3 unitVector = Rand.UnitVector3;
            int     specIdx    = featureEnabled ? PickSpectralIndex(Rand.Value) : 0;
            float   size       = StarsDrawSize.RandomInRange;
            float   quadAngle  = Rand.Range(0f, 360f);

            // Ensure we always draw *something*; if we're in vanilla-fallback mode use the sole material.
            if (!featureEnabled) specIdx = 0;

            if (!bhActive)
            {
                Vector3 sunRef = UseStaticRotation
                    ? GenCelestial.CurSunPositionInWorldSpace().normalized
                    : Vector3.forward;
                float sunDot = Vector3.Dot(unitVector, sunRef);
                if (sunDot > 0.8f)
                    size *= GenMath.LerpDouble(0.8f, 1f, 1f, 0.35f, sunDot);
            }

            if (bhActive && !UseStaticRotation)
            {
                float dot   = unitVector.z;
                float sinTh = Mathf.Sqrt(Mathf.Max(0f, 1f - dot * dot));
                float theta = Mathf.Atan2(sinTh, dot);

                if (theta < shadowTheta)
                    continue;

                if (theta < lensRadius && sinTh > 1e-4f)
                {
                    float raw   = LensStrength * (horizonTheta * horizonTheta) / theta;
                    float fade  = Mathf.SmoothStep(lensRadius, lensRadius * 0.25f, theta);
                    float delta = Mathf.Min(raw * fade, Mathf.PI * 0.3f);

                    float   thetaN = theta + delta;
                    Vector3 perp   = new Vector3(unitVector.x, unitVector.y, 0f) / sinTh;
                    unitVector     = Mathf.Cos(thetaN) * Vector3.forward
                                   + Mathf.Sin(thetaN) * perp;

                    float magFrac = Mathf.Clamp01(1f - theta / (horizonTheta * 4f));
                    size *= 1f + magFrac * 1.5f;
                }
            }

            Vector3      pos     = unitVector * DistanceToStars;
            LayerSubMesh subMesh = GetSubMesh(mats[specIdx]);
            WorldRendererUtility.PrintQuadTangentialToPlanet(
                pos, size, 0f, subMesh, counterClockwise: true, quadAngle);
        }

        calculatedForStartingTile    = Find.GameInitData?.startingTile ?? PlanetTile.Invalid;
        calculatedForStaticRotation  = UseStaticRotation;
        calculatedForBlackHoleActive = GameCondition_BlackHole.IsActive;
        calculatedForGrowthBucket    = GrowthBucket;
        calculatedForFeatureEnabled  = featureEnabled;

        Rand.PopState();
        FinalizeMesh(MeshParts.All);
    }
}

/// <summary>
/// Darkens the world-view sky background while the black hole feature is
/// enabled; restores vanilla's blue-grey background when it's disabled.
/// Applied once at startup via the static ctor, and re-applied from the
/// settings-toggle handler so a mid-session enable/disable takes effect
/// immediately instead of leaving the sky stuck until relaunch.
/// </summary>
[StaticConstructorOnStartup]
static class SkyBackgroundFix
{
    private static readonly Color VanillaBackground = new Color(0.063f, 0.090f, 0.118f, 1f);
    private static readonly Color BlackHoleBackground = new Color(0.008f, 0.008f, 0.014f, 1f);

    static SkyBackgroundFix()
    {
        Apply(MSSFPMod.settings != null && MSSFPMod.settings.BlackHoleEnabled);
    }

    /// <summary>Sets the world skybox background to the black-hole or vanilla colour.</summary>
    public static void Apply(bool blackHoleEnabled)
    {
        var cam = WorldCameraManager.WorldSkyboxCamera;
        if (cam != null)
            cam.backgroundColor = blackHoleEnabled ? BlackHoleBackground : VanillaBackground;
    }
}
