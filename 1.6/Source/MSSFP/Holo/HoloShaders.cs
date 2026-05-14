using UnityEngine;
using Verse;

namespace MSSFP.Holo;

/// <summary>
/// Static cache for the HoloMono shader (built from UnityAssets/, shipped via the
/// per-platform AssetBundles at <c>Common/AssetBundles/mssfp_{linux,mac,win}</c>).
///
/// RimWorld 1.6 auto-loads those bundles at startup and indexes their contents under
/// <see cref="ContentFinder{T}"/>. The shader name in the AssetBundle is the
/// ShaderLab name declared in the .shader file (<c>"MSSFP/HoloMono"</c>), NOT the file name.
///
/// FAILURE HANDLING: when the bundle is missing or the lookup name is wrong,
/// <see cref="ContentFinder{T}.Get"/> returns null. We log loudly once and fall back
/// to <see cref="ShaderDatabase.Cutout"/>. Callers should still null-check
/// <see cref="HoloMono"/> against the fallback before using it as a "is holo shader
/// available" signal — every code path that reads it should handle the null/fallback
/// case gracefully.
///
/// KEYWORDS: the shader declares <c>_OUTLINE_ON</c> and <c>_GLOW_ON</c> as global
/// <c>multi_compile</c> keywords. We read MSSFP settings at startup and call
/// <see cref="Shader.EnableKeyword"/>/<see cref="Shader.DisableKeyword"/> once to set
/// the runtime variant. Settings changes require a game restart to take effect.
/// </summary>
[StaticConstructorOnStartup]
public static class HoloShaders
{
    // itemPath into ContentFinder. The full lookup path inside the bundle is
    //   Assets/Data/<ModFolderName>/Materials/<itemPath>.shader
    // ModFolderName for MSSFP is "MrSamuelStreamerFlavourPack". ContentFinder
    // handles that prefix automatically; we just provide the leaf name.
    private const string HoloMonoShaderName = "HoloMono";
    private const string OutlineKeyword = "_OUTLINE_ON";
    private const string GlowKeyword = "_GLOW_ON";

    public static readonly Shader HoloMono;

    static HoloShaders()
    {
        Shader loaded = ContentFinder<Shader>.Get(HoloMonoShaderName, reportFailure: false);
        if (loaded == null)
        {
            Log.Error(
                $"[MSSFP] HoloMono shader '{HoloMonoShaderName}' not found in AssetBundles. "
                    + "Holos will render with vanilla cutout shader. "
                    + "Verify Common/AssetBundles/mssfp_{linux,mac,win} exists and was built "
                    + "from UnityAssets/ with Unity 2022.3.35f1."
            );
            HoloMono = ShaderDatabase.Cutout;
        }
        else
        {
            HoloMono = loaded;
            ApplyKeywordsFromSettings();
        }
    }

    /// <summary>
    /// Push current MSSFP settings into the global shader keyword state. Idempotent —
    /// safe to call at any time. Only does anything when <see cref="HoloMono"/> is the
    /// real holo shader (not the cutout fallback).
    /// </summary>
    public static void ApplyKeywordsFromSettings()
    {
        Settings s = MSSFPMod.settings;
        if (s == null) return;

        if (s.EnableHoloOutline)
            Shader.EnableKeyword(OutlineKeyword);
        else
            Shader.DisableKeyword(OutlineKeyword);

        if (s.EnableHoloGlow)
            Shader.EnableKeyword(GlowKeyword);
        else
            Shader.DisableKeyword(GlowKeyword);
    }

    /// <summary>True iff the real holo shader loaded successfully (not the cutout fallback).</summary>
    public static bool IsLoaded => HoloMono != null && HoloMono != ShaderDatabase.Cutout;
}
