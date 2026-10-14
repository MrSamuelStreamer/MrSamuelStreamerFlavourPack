using UnityEngine;
using Verse;

namespace MSSFP.Holo;

/// <summary>
/// Static cache for the holo shader. In 1.7 it is the <c>MSSFP_HoloMono</c> ShaderDef
/// (GLSL, <c>1.7/Shaders/MSSFP_HoloMono.frag.glsl</c>). The 1.6 build still uses the Unity
/// AssetBundle shader; the two are kept in step by hand.
///
/// The 1.6 shader toggled its outline and glow through global shader keywords, which 1.7
/// cannot do (it builds one variant). They are material flags in the GLSL shader instead
/// (<c>_Flags</c>, both on by default, as 1.6 always enabled both).
///
/// FAILURE HANDLING: when the ShaderDef is missing we log loudly once and fall back to
/// <see cref="ShaderDatabase.Cutout"/>. Callers should still compare <see cref="HoloMono"/>
/// against the fallback (see <see cref="IsLoaded"/>) before relying on holo visuals.
/// </summary>
[StaticConstructorOnStartup]
public static class HoloShaders
{
    private const string HoloMonoShaderDefName = "MSSFP_HoloMono";

    public static readonly Shader HoloMono;

    static HoloShaders()
    {
        if (DefDatabase<ShaderDef>.GetNamedSilentFail(HoloMonoShaderDefName) == null)
        {
            Log.Error(
                $"[MSSFP] ShaderDef '{HoloMonoShaderDefName}' not found. "
                    + "Holos will render with vanilla cutout shader. "
                    + "Check 1.7/Defs/ShaderDefs/MSSFP_ShaderDefs.xml is in the load folders."
            );
            HoloMono = ShaderDatabase.Cutout;
            return;
        }

        HoloMono = ShaderDatabase.LoadShader(HoloMonoShaderDefName);
    }

    /// <summary>True iff the real holo shader loaded (not the cutout fallback).</summary>
    public static bool IsLoaded => HoloMono != null && HoloMono != ShaderDatabase.Cutout;
}
