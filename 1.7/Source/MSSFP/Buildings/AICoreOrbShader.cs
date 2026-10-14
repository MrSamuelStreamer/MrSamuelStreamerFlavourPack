using UnityEngine;
using Verse;

namespace MSSFP.Buildings;

/// <summary>
/// AI core orb shader status. In 1.7 the orb is drawn with the <c>MSSFP_AICoreOrb</c>
/// ShaderDef (GLSL, <c>1.7/Shaders/MSSFP_AICoreOrb.frag.glsl</c>) through the
/// <c>MSSFP_AICoreOrb</c> MaterialDef, which also supplies the orb mask texture. The 1.6
/// build still uses the Unity AssetBundle shader; the two are kept in step by hand.
///
/// FAILURE HANDLING: if the material did not resolve (bad def, missing shader), render
/// code must stop drawing the orb: <see cref="BaseContent.BadMat"/> would draw a stray
/// pink quad over the building. <see cref="IsLoaded"/> = false suppresses the orb draw, so
/// the player sees just the pedestal, identical to the unpowered state.
/// </summary>
public static class AICoreOrbShader
{
    /// <summary>True iff the orb material loaded (not null and not the pink fallback).</summary>
    public static bool IsLoaded =>
        AICoreOrbRenderer.OrbMaterial != null
        && AICoreOrbRenderer.OrbMaterial != BaseContent.BadMat;
}
