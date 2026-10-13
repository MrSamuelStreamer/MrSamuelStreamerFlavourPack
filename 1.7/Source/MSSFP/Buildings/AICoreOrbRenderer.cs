using UnityEngine;
using Verse;

namespace MSSFP.Buildings;

/// <summary>
/// Static cache for the AI Core orb mesh + material + tuning constants. One shared mesh and
/// one shared material — per-instance variation goes through <see cref="MaterialPropertyBlock"/>
/// at the call site so we never clone materials.
///
/// All geometry constants are derived from the SVG source
/// (<c>Common/Textures/Things/Building/MSSFP_AI_Core.svg</c>):
///   - canvas:           64 × 64 units (matches a 2-cell ThingDef <c>drawSize</c>)
///   - orb circle:       cx=34.57, cy=24.08, r=19.81
///   - SVG gradient ctr: (35.56, 21.59) → +0.99, −2.49 from orb centre
///     (SVG y axis points down; in Unity UV y points up, so flip the sign).
/// </summary>
[StaticConstructorOnStartup]
public static class AICoreOrbRenderer
{
    /// <summary>Orb diameter in world cells. SVG orb r=19.81 / canvas 32 (half-canvas) × 2 cells.</summary>
    public const float OrbWorldSize = 1.24f;

    /// <summary>Orb centre Z offset from building draw origin in world cells.
    /// SVG canvas mid-y = 32, orb cy = 24.08 → orb is 7.92 units above centre → 7.92/32 = 0.25.</summary>
    public const float OrbCenterZOffset = 0.25f;

    /// <summary>Rest-position gradient offset from UV centre. Derived from SVG
    /// gradient-centre vs orb-centre, normalised by orb radius, y-flipped to Unity UV space.</summary>
    public static readonly Vector2 BaseGradientOffset = new Vector2(0.050f, 0.126f);

    /// <summary>How much cursor displacement (in orb-radius units) maps to UV displacement.
    /// 0.30 keeps the highlight inside the orb for any cursor position within MaxFollowRadius.</summary>
    public const float GradientFollowScale = 0.30f;

    /// <summary>Maximum cursor distance to consider (in orb-radius units). Cursor beyond is
    /// clamped to this radius — gradient sits at the orb edge in the cursor direction.</summary>
    public const float MaxFollowRadius = 1.0f;

    /// <summary>Bob cycle length in ticks. 60 ticks/s at Normal speed × 4 s = 240.</summary>
    public const int BobCycleTicks = 240;

    /// <summary>Bob amplitude as a fraction of orb height (peak-to-zero).</summary>
    public const float BobAmplitudeFraction = 0.25f;

    /// <summary>Shader property id for the vec4 (xy used) gradient-centre uniform.</summary>
    public static readonly int GradientCenterID = Shader.PropertyToID("_GradientCenter");

    public static readonly Mesh OrbMesh;
    public static readonly Material OrbMaterial;

    static AICoreOrbRenderer()
    {
        OrbMesh = MeshPool.GridPlane(new Vector2(OrbWorldSize, OrbWorldSize));

        Texture2D mask = ContentFinder<Texture2D>.Get(
            "Things/Building/MSSFP_AI_Core_Orb",
            reportFailure: false
        );
        if (mask == null)
        {
            Log.Error(
                "[MSSFP] AI Core orb mask texture missing at "
                    + "Things/Building/MSSFP_AI_Core_Orb. Orb will not render."
            );
        }

        // One shared material. Per-instance _GradientCenter goes through MaterialPropertyBlock
        // at draw time so multiple AI cores can track the cursor independently without cloning
        // the material (which would defeat batching and bloat memory).
        OrbMaterial = new Material(AICoreOrbShader.Shader);
        if (mask != null)
        {
            OrbMaterial.mainTexture = mask;
        }
    }
}
