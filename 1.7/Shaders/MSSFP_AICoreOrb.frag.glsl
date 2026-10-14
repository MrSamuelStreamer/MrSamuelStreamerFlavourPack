#version 450

// AI core orb: GLSL port of UnityAssets/.../AICoreOrb.shader (the 1.6 Unity shader); keep the two in step.
// Radial colour ramp around a cursor-following centre, masked by the orb texture's alpha.

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
    vec4 _GradientCenter; // @default = vec4(0.55, 0.626, 0.0, 0.0) ; xy = gradient centre in UV, set per draw
    vec4 _Orb;            // @default = vec4(0.7, 0.75, 0.0, 0.0) ; x = gradient radius (UV), y = opacity
    vec4 _MainTex_ST;     // @default = vec4(0.0, 0.0, 1.0, 1.0)
} uMaterial;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;

// SVG radialGradient2 stops: #1deeff -> #e0f5df (pale mint) -> #1deeff -> #1deeff.
const vec3 STOP0 = vec3(0.114, 0.933, 1.000);
const vec3 STOP1 = vec3(0.878, 0.961, 0.875);
const vec3 STOP2 = vec3(0.114, 0.933, 1.000);
const vec3 STOP3 = vec3(0.114, 0.933, 1.000);
const float OFFSET1 = 0.30462432;
const float OFFSET2 = 0.6950779;
const float OFFSET3 = 1.0;

// 4-stop piecewise linear ramp on t in [0,1].
vec3 sampleRamp(float t)
{
    if (t < OFFSET1)
        return mix(STOP0, STOP1, t / OFFSET1);
    if (t < OFFSET2)
        return mix(STOP1, STOP2, (t - OFFSET1) / (OFFSET2 - OFFSET1));
    return mix(STOP2, STOP3, clamp((t - OFFSET2) / (OFFSET3 - OFFSET2), 0.0, 1.0));
}

void main()
{
    vec2 uv = vUv * uMaterial._MainTex_ST.zw + uMaterial._MainTex_ST.xy;
    vec4 tex = texture(_MainTex, uv);
    float t = clamp(length(uv - uMaterial._GradientCenter.xy) / uMaterial._Orb.x, 0.0, 1.0);
    outColor = vec4(sampleRamp(t), tex.a * uMaterial._Orb.y);
}
