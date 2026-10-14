#version 450

// Holo pawn shader: GLSL port of UnityAssets/.../HoloMono.shader (the 1.6 Unity shader); keep the two in step.
//
//   1. Sample _MainTex, convert RGB to luminance (Rec.601), multiply by the tint (_Color).
//   2. Alpha = tex.a * (base alpha + sin(clock * speed) * pulse amplitude).
//   3. Outline: 4-tap neighbour edge detect.   4. Glow: tint halo on the silhouette.
//
// 1.6 toggled the outline and glow through global shader keywords. 1.7 builds one variant, so they are
// material flags here (_Flags.x outline, _Flags.y glow), both on by default as in 1.6.
// The clock is the engine's global _GameSeconds, which is game time: the pulse pauses with the game.

// Registers the set2/binding2 SSBO slot in the material layout so the INSTANCED vertex variant's binding
// matches (the layout is parsed from this fragment source). The fragment never samples it.
#ifdef INSTANCED
#include "InstanceTransforms.glsl"
#endif

layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color;         // @color @default = vec4(0.4, 0.7, 1.0, 1.0)
    vec4 _MainTex_ST;    // @default = vec4(0.0, 0.0, 1.0, 1.0)
    vec4 _Pulse;         // @default = vec4(0.7, 0.15, 2.5, 0.0) ; x = base alpha, y = pulse amplitude, z = pulse speed
    vec4 _OutlineColor;  // @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _EdgeParams;    // @default = vec4(0.01, 0.4, 0.6, 0.0) ; x = edge thickness (UV), y = outline tint mix, z = glow strength
    vec4 _Flags;         // @default = vec4(1.0, 1.0, 0.0, 0.0) ; x = outline on, y = glow on
    float _GameSeconds;  // global
} uMaterial;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;

float luminance601(vec3 c)
{
    return dot(c, vec3(0.299, 0.587, 0.114));
}

void main()
{
    vec2 uv = vUv * uMaterial._MainTex_ST.zw + uMaterial._MainTex_ST.xy;
    vec4 tex = texture(_MainTex, uv);

    // Mono x tint: throw the source hue away entirely.
    vec3 mono = luminance601(tex.rgb) * uMaterial._Color.rgb;

    float pulse = uMaterial._Pulse.x + sin(uMaterial._GameSeconds * uMaterial._Pulse.z) * uMaterial._Pulse.y;
    float alpha = tex.a * pulse;

    // Shared edge detect for outline and glow: high where a neighbour is opaque but the centre is not.
    float th = uMaterial._EdgeParams.x;
    float aL = texture(_MainTex, uv + vec2(-th, 0.0)).a;
    float aR = texture(_MainTex, uv + vec2( th, 0.0)).a;
    float aU = texture(_MainTex, uv + vec2(0.0,  th)).a;
    float aD = texture(_MainTex, uv + vec2(0.0, -th)).a;
    float edgeMask = clamp(max(max(aL, aR), max(aU, aD)) - tex.a, 0.0, 1.0);

    if (uMaterial._Flags.x > 0.5)
    {
        float outlineFactor = smoothstep(0.05, 0.5, edgeMask);
        vec3 outlineRGB = mix(uMaterial._OutlineColor.rgb, uMaterial._OutlineColor.rgb * uMaterial._Color.rgb, uMaterial._EdgeParams.y);
        mono = mix(mono, outlineRGB, outlineFactor);
        alpha = max(alpha, outlineFactor * uMaterial._OutlineColor.a * pulse);
    }

    if (uMaterial._Flags.y > 0.5)
    {
        float glow = edgeMask * uMaterial._EdgeParams.z;
        mono += uMaterial._Color.rgb * glow;
        alpha = max(alpha, glow * pulse);
    }

    outColor = vec4(mono, alpha);
}
