using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.Comps.World;

/// <summary>
/// Game condition: the home system's star has been replaced by a black hole.
///
/// Two effects:
///   1. In-map: caps sunlight via SkyTarget.
///   2. World-map: replaces the vanilla sun disc with a ray-marched black hole
///      rendered by the MSSFP/BlackHoleRayMarch shader.
///
/// Ported from the CerebrexFlavourPack implementation. Growth uses Find.TickManager
/// ticks so the sim pauses when the game pauses.
/// </summary>
[StaticConstructorOnStartup]
public class GameCondition_BlackHole : GameCondition
{
    // ── Sky colours ──────────────────────────────────────────────────────────
    private static readonly Color SkyNearBlack     = new ColorInt(5, 5, 12).ToColor;
    private static readonly Color ShadowNearBlack  = new ColorInt(3, 3,  8).ToColor;
    private static readonly Color OverlayNearBlack = new ColorInt(2, 2,  6).ToColor;

    private static SkyColorSet ComputeColors(float cap)
    {
        return new SkyColorSet(
            Color.Lerp(SkyNearBlack, Color.white, cap),
            Color.Lerp(ShadowNearBlack, Color.white, cap),
            Color.Lerp(OverlayNearBlack, Color.white, cap),
            Mathf.Lerp(0.1f, 1f, cap)
        );
    }

    // ── Runtime state ─────────────────────────────────────────────────────────
    // 1.7 draws the black hole as a camera-facing quad on the world skybox layer, using the
    // MSSFP_BlackHole ShaderDef/MaterialDef. 1.6's GL + OnPostRender path does not exist in 1.7.
    private const string MaterialPath = "MSSFP/BlackHole";

    private static Material              bhMaterial;
    private static MaterialPropertyBlock bhBlock;
    private static bool                  bhWanted;
    private static bool                  bhMaterialFailed;

    /// <summary>True while the black hole is requested and its material has not failed to load.</summary>
    internal static bool IsActive => bhWanted && !bhMaterialFailed;

    /// <summary>
    /// Materials must be created on the main thread (MaterialAllocator), but activation can run from
    /// world generation on a worker thread. So the material is loaded lazily from the first draw.
    /// </summary>
    private static bool EnsureMaterial()
    {
        if (bhMaterial != null) return true;

        Material mat = MatLoader.LoadMat(MaterialPath);
        if (mat == null || mat == BaseContent.BadMat)
        {
            // IsActive turns false so Harmony_BlackHoleSun gives the vanilla sun back.
            bhMaterialFailed = true;
            ModLog.Warn($"GameCondition_BlackHole: material '{MaterialPath}' did not load — black hole inactive.");
            return false;
        }

        bhMaterial = mat;
        bhBlock    = new MaterialPropertyBlock();
        ModLog.Log($"Black hole material ready — shader={bhMaterial.shader?.name}");
        return true;
    }

    /// <summary>Fraction of normal sunlight the world-map planet receives while active.</summary>
    internal const float WorldLightFactor = 0.5f;

    // ── Growth ─────────────────────────────────────────────────────────────
    private static int bhStartTick;

    /// <summary>
    /// Radius multiplier applied to every black hole dimension, derived from the
    /// condition's age. blackHoleGrowthRate is in *area* doublings per in-game year,
    /// hence the 0.5 exponent (radius = sqrt(area)). Clamped to blackHoleGrowthMax.
    /// Reads game-time (TicksGame) so pauses actually pause growth.
    /// </summary>
    internal static float GrowthFactor
    {
        get
        {
            Settings s = MSSFPMod.settings;
            if (s == null || !s.BlackHoleGrowthEnabled || Find.TickManager == null) return 1f;
            float years = (Find.TickManager.TicksGame - bhStartTick) / (float)GenDate.TicksPerYear;
            if (years <= 0f) return 1f;
            return Mathf.Min(Mathf.Pow(2f, 0.5f * years * s.BlackHoleGrowthRate), s.BlackHoleGrowthMax);
        }
    }

    // ── SkyTarget ────────────────────────────────────────────────────────────
    public override SkyTarget? SkyTarget(Verse.Map map)
    {
        float cap = MSSFPMod.settings.BlackHoleLightCap;
        return new SkyTarget(cap, ComputeColors(cap), 0.5f, 0.3f);
    }

    public override float SkyTargetLerpFactor(Verse.Map map) =>
        GameConditionUtility.LerpInOutValue(this, 5000f);

    public override bool AllowEnjoyableOutsideNow(Verse.Map map) => false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    public override void Init()
    {
        base.Init();
        ActivateBlackHole(startTick);
    }

    public override void End()
    {
        base.End();
        DeactivateBlackHole();
    }

    // ── Sphere management ─────────────────────────────────────────────────────
    internal const float SphereWorldRadius = 150f;

    internal static void ActivateBlackHole(int startTick)
    {
        bhStartTick = startTick;

        // Setting sync and Init() can both activate; the second call only refreshes the start tick.
        bhWanted         = true;
        bhMaterialFailed = false;

        ModLog.Log("Black hole ACTIVATED.");
    }

    internal static void DeactivateBlackHole()
    {
        bhWanted   = false;
        bhMaterial = null; // owned by MatLoader's cache; do not destroy
        bhBlock    = null;
        ModLog.Log("Black hole deactivated.");
    }

    /// <summary>Called once per frame by <see cref="WorldDrawLayer_BlackHole"/> while the world view is drawn.</summary>
    internal static void SubmitWorldDrawCall(int renderLayer)
    {
        if (!IsActive || !EnsureMaterial()) return;

        Camera cam = WorldCameraManager.WorldSkyboxCamera;
        if (cam == null) return;

        Vector3 sunDir = GenCelestial.CurSunPositionInWorldSpace().normalized;
        Vector3 vp     = cam.WorldToViewportPoint(cam.transform.position + sunDir * 1000f);
        if (vp.z <= 0f) return;

        float aspect        = cam.aspect;
        float fovHalfTan    = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float sunVpRadius   = 7.5f / (2f * 20f * fovHalfTan);

        float cosAlpha      = Mathf.Max(vp.z / 1000f, 0.5f);
        float rawPerspScale = 1f / (cosAlpha * cosAlpha);

        float growth     = GrowthFactor;
        float dampen     = 1f / growth;
        float perspScale = Mathf.Lerp(1f, rawPerspScale, dampen);

        float discScale     = BHDiscScaleBase * growth;
        float scaledRadius  = sunVpRadius * discScale * perspScale;
        float horizonRadius = scaledRadius * 0.22f;
        float discOuter     = scaledRadius * 0.77f;

        // Quad half-extent in viewport-Y fractions: covers the shader's 1.5x outer cutoff plus the old GL margin.
        float halfExtentVp = discOuter * 1.5f + 0.03f * perspScale;
        float worldHalf    = halfExtentVp * 2f * vp.z * fovHalfTan;

        // plane10 lies in XZ with +Y normal. Local Z -> camera up, local Y -> towards camera,
        // so local X -> camera right. Skybox.vert adds the camera position, so place it camera-relative.
        Transform ct       = cam.transform;
        Quaternion rot     = Quaternion.LookRotation(ct.up, -ct.forward);
        Matrix4x4 matrix   = Matrix4x4.TRS(sunDir * 1000f, rot, new Vector3(worldHalf * 2f, 1f, worldHalf * 2f));

        // Disc orientation: screen-space angle of a fixed world-space "disc right" axis (as in 1.6).
        Vector3 rawUp    = Vector3.up;
        Vector3 discAxis = rawUp - Vector3.Dot(rawUp, sunDir) * sunDir;
        if (discAxis.sqrMagnitude < 0.01f)
        {
            rawUp    = Vector3.forward;
            discAxis = rawUp - Vector3.Dot(rawUp, sunDir) * sunDir;
        }
        discAxis = discAxis.normalized;

        Vector3 discRight   = Vector3.Cross(sunDir, discAxis).normalized;
        Vector3 vpDiscRight = cam.WorldToViewportPoint(cam.transform.position + sunDir * 1000f + discRight * 100f);
        float   discAngle   = Mathf.Atan2(vpDiscRight.y - vp.y, (vpDiscRight.x - vp.x) * aspect);

        bhBlock.Clear();
        bhBlock.SetVector("_Disc", new Vector4(discAngle, DiscSpeed, 0f, 0f));
        bhBlock.SetVector("_Params", new Vector4(Time.time, horizonRadius, discOuter, halfExtentVp));
        Graphics.DrawMesh(MeshPool.plane10, matrix, bhMaterial, renderLayer, null, 0, bhBlock);
    }

    /// <summary>Overall disc scale at year 0 (before growth applies). See ported source for tuning notes.</summary>
    internal const float BHDiscScaleBase = 0.25f;

    /// <summary>Accretion disc pulse speed fed to the shader (the 1.6 material used 3.5).</summary>
    private const float DiscSpeed = 3.5f;
}
