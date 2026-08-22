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

    // ── Static assets ────────────────────────────────────────────────────────
    private static readonly Shader    RayMarchShader;
    private static readonly Texture2D DiscTexture;

    static GameCondition_BlackHole()
    {
        // ContentFinder<Shader> indexes bundled shaders by ShaderLab leaf name, not the
        // "MSSFP/" category prefix declared in the .shader file — matches the convention
        // used by HoloShaders/AICoreOrbShader elsewhere in this mod.
        RayMarchShader = ContentFinder<Shader>.Get("BlackHoleRayMarch", reportFailure: false);

        if (RayMarchShader != null)
            ModLog.Log($"BlackHoleRayMarch shader loaded OK. isSupported={RayMarchShader.isSupported}");
        else
            ModLog.Warn("BlackHoleRayMarch shader NOT FOUND in mod asset bundles.");

        DiscTexture = ContentFinder<Texture2D>.Get("AccretionDisc", reportFailure: false);
    }

    // ── Runtime state ─────────────────────────────────────────────────────────
    private static Material       bhMaterial;
    private static BHRenderHelper bhRenderHelper;

    /// <summary>True while the black hole is active and the render helper is attached.</summary>
    internal static bool IsActive => bhRenderHelper != null;

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

        if (RayMarchShader == null)
        {
            ModLog.Warn("GameCondition_BlackHole: shader not loaded — black hole inactive.");
            return;
        }
        if (bhRenderHelper != null)
        {
            ModLog.Warn("GameCondition_BlackHole.ActivateBlackHole: stale render helper — cleaning up before re-activation.");
            DeactivateBlackHole();
        }

        bhMaterial = new Material(RayMarchShader);
        bhMaterial.SetFloat("_DiscSpeed", 3.5f);
        if (DiscTexture != null)
            bhMaterial.SetTexture("_DiscTex", DiscTexture);

        Camera skyboxCam   = WorldCameraManager.WorldSkyboxCamera;
        bhRenderHelper     = skyboxCam.gameObject.AddComponent<BHRenderHelper>();
        bhRenderHelper.Mat = bhMaterial;

        ModLog.Log($"Black hole ACTIVATED — shader={RayMarchShader.name}");
    }

    internal static void DeactivateBlackHole()
    {
        if (bhRenderHelper != null)
        {
            Object.Destroy(bhRenderHelper);
            bhRenderHelper = null;
        }
        if (bhMaterial != null)
        {
            Object.Destroy(bhMaterial);
            bhMaterial = null;
        }
        ModLog.Log("Black hole deactivated.");
    }

    private static int _drawCallCount;

    internal static void SubmitWorldDrawCall(int renderLayer)
    {
        if (!IsActive) return;
        _drawCallCount++;
    }
}

// ── Render helper ─────────────────────────────────────────────────────────────

/// <summary>
/// MonoBehaviour attached to the WorldSkyboxCamera's GameObject.
///
/// Each frame OnPostRender fires: computes viewport-space disc geometry, uploads
/// per-frame material uniforms, and draws a GL disc in viewport UV space.
/// The fragment shader ray-marches the Schwarzschild metric to render the event
/// horizon and accretion disc.
/// </summary>
internal sealed class BHRenderHelper : MonoBehaviour
{
    internal Material Mat;

    /// <summary>Overall disc scale at year 0 (before growth applies). See ported source for tuning notes.</summary>
    internal const float BHDiscScaleBase = 0.25f;

    private int _frameCount;

    private void OnPostRender()
    {
        if (Mat == null) return;

        _frameCount++;
        Camera cam = Camera.current;
        if (cam == null) return;

        Vector3 sunDir   = GenCelestial.CurSunPositionInWorldSpace().normalized;
        Vector3 vp       = cam.WorldToViewportPoint(cam.transform.position + sunDir * 1000f);
        if (vp.z <= 0f) return;

        float aspect = (float)Screen.width / Screen.height;

        float fovHalfTan    = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float sunVpRadius   = 7.5f / (2f * 20f * fovHalfTan);

        float cosAlpha      = Mathf.Max(vp.z / 1000f, 0.5f);
        float rawPerspScale = 1f / (cosAlpha * cosAlpha);

        float dampen     = 1f / GameCondition_BlackHole.GrowthFactor;
        float perspScale = Mathf.Lerp(1f, rawPerspScale, dampen);

        float discScale     = BHDiscScaleBase * GameCondition_BlackHole.GrowthFactor;
        float scaledRadius  = sunVpRadius * discScale * perspScale;
        float horizonRadius = scaledRadius * 0.22f;
        float discOuter     = scaledRadius * 0.77f;

        Vector3 rawUp    = Vector3.up;
        float   upDotSun = Vector3.Dot(rawUp, sunDir);
        Vector3 discAxis = rawUp - upDotSun * sunDir;
        if (discAxis.sqrMagnitude < 0.01f)
        {
            rawUp    = Vector3.forward;
            upDotSun = Vector3.Dot(rawUp, sunDir);
            discAxis = rawUp - upDotSun * sunDir;
        }
        discAxis = discAxis.normalized;

        Vector3 discRight   = Vector3.Cross(sunDir, discAxis).normalized;
        Vector3 vpDiscRight = cam.WorldToViewportPoint(
                                  cam.transform.position + sunDir * 1000f + discRight * 100f);
        float   discAngle   = Mathf.Atan2(vpDiscRight.y - vp.y,
                                          (vpDiscRight.x - vp.x) * aspect);

        Mat.SetVector("_SunVP",           new Vector4(vp.x, vp.y, 0f, 0f));
        Mat.SetFloat ("_HorizonRadius",   horizonRadius);
        Mat.SetFloat ("_DiscOuterRadius", discOuter);
        Mat.SetFloat ("_Aspect",          aspect);
        Mat.SetFloat ("_DiscAngle",       discAngle);

        float glRadiusV = discOuter + 0.03f * perspScale;
        float glRadiusU = glRadiusV / aspect;

        Mat.SetPass(0);
        GL.PushMatrix();
        GL.LoadOrtho();
        GL.Begin(GL.TRIANGLES);
        DrawDisc(vp.x, vp.y, glRadiusU, glRadiusV);
        GL.End();
        GL.PopMatrix();
    }

    private static void DrawDisc(float cx, float cy, float rx, float ry, int segs = 48)
    {
        for (int i = 0; i < segs; i++)
        {
            float a0 = (float)i       / segs * 2f * Mathf.PI;
            float a1 = (float)(i + 1) / segs * 2f * Mathf.PI;
            GL.Vertex3(cx,                       cy,                       0f);
            GL.Vertex3(cx + Mathf.Cos(a0) * rx,  cy + Mathf.Sin(a0) * ry,  0f);
            GL.Vertex3(cx + Mathf.Cos(a1) * rx,  cy + Mathf.Sin(a1) * ry,  0f);
        }
    }
}
