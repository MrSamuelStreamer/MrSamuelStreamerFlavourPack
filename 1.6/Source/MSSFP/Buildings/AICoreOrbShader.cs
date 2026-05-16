using UnityEngine;
using Verse;

namespace MSSFP.Buildings;

/// <summary>
/// AICoreOrb shader loader. Mirrors <see cref="MSSFP.Holo.HoloShaders"/> structure: the
/// shader ships inside the per-platform <c>mssfp_{linux,mac,win}</c> AssetBundle built from
/// <c>UnityAssets/Assets/Data/MrSamuelStreamerFlavourPack/Materials/AICoreOrb.shader</c>.
///
/// <see cref="ContentFinder{T}"/> auto-indexes bundle contents at game startup. We look the
/// shader up by ShaderLab name leaf (<c>"AICoreOrb"</c>); the full asset path inside the
/// bundle is <c>Assets/Data/MrSamuelStreamerFlavourPack/Materials/AICoreOrb.shader</c>.
///
/// FAILURE HANDLING: on lookup miss, log a loud one-shot error and fall back to
/// <see cref="ShaderDatabase.Cutout"/>. Render code must consult <see cref="IsLoaded"/>
/// before drawing — Cutout has no <c>_GradientCenter</c> and would render a stray flat
/// white disc on top of the building. <see cref="IsLoaded"/> = false suppresses the orb
/// draw entirely; the player sees just the pedestal, identical to the unpowered state.
/// </summary>
[StaticConstructorOnStartup]
public static class AICoreOrbShader
{
    private const string ShaderName = "AICoreOrb";

    public static readonly Shader Shader;

    static AICoreOrbShader()
    {
        Shader loaded = ContentFinder<Shader>.Get(ShaderName, reportFailure: false);
        if (loaded == null)
        {
            Log.Error(
                $"[MSSFP] AICoreOrb shader '{ShaderName}' not found in AssetBundles. "
                    + "AI core orb will not render. Verify Common/AssetBundles/mssfp_{linux,mac,win} "
                    + "was rebuilt from UnityAssets/ after the shader was added."
            );
            Shader = ShaderDatabase.Cutout;
        }
        else
        {
            Shader = loaded;
        }
    }

    /// <summary>True iff the real AICoreOrb shader loaded (not the cutout fallback).</summary>
    public static bool IsLoaded => Shader != null && Shader != ShaderDatabase.Cutout;
}
