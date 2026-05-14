// MSSFP HoloMono — holo pawn shader.
//
// Pipeline per fragment:
//   1. Sample _MainTex.
//   2. Convert RGB to luminance (Rec.601 weights).
//   3. Multiply luminance by tint (mat.color, baked in via Graphic).
//   4. Alpha = tex.a × (_BaseAlpha + sin(_Time.y × speed) × pulseAmp).
//   5. Optional outline (4-tap neighbor edge detect, keyword _OUTLINE_ON).
//   6. Optional glow (silhouette emission, keyword _GLOW_ON).
//
// Transparent blend, ZWrite Off, Cull Off. Matches vanilla cutout-transparent
// expectations so it slots into the apparel/hair/body render pipeline.
Shader "MSSFP/HoloMono"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0.4, 0.7, 1.0, 1.0)
        _BaseAlpha ("Base Alpha", Range(0,1)) = 0.7
        _PulseAmp ("Pulse Amplitude", Range(0,0.5)) = 0.15
        _PulseSpeed ("Pulse Speed", Range(0,10)) = 2.5
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineThickness ("Outline Thickness (UV)", Range(0,0.05)) = 0.01
        _OutlineMix ("Outline Tint Mix", Range(0,1)) = 0.4
        _GlowStrength ("Glow Strength", Range(0,2)) = 0.6
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // GLOBAL keywords (multi_compile, not _local) so C# can toggle them
            // application-wide via Shader.EnableKeyword from MSSFP settings — no
            // per-material set required.
            #pragma multi_compile _ _OUTLINE_ON
            #pragma multi_compile _ _GLOW_ON
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _BaseAlpha;
            float _PulseAmp;
            float _PulseSpeed;
            float4 _OutlineColor;
            float _OutlineThickness;
            float _OutlineMix;
            float _GlowStrength;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // Rec.601 luminance weights — matches old TV gamma, gives
            // perceptually-balanced grayscale for typical pawn art.
            inline float Luminance601(float3 c)
            {
                return dot(c, float3(0.299, 0.587, 0.114));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);

                // Mono × tint. Throw away source hue entirely (spec: mono colour appearance).
                float gray = Luminance601(tex.rgb);
                float3 mono = gray * _Color.rgb;

                // Alpha pulse. sin range [-1,1] -> [_BaseAlpha - _PulseAmp, _BaseAlpha + _PulseAmp].
                float pulse = _BaseAlpha + sin(_Time.y * _PulseSpeed) * _PulseAmp;
                float alpha = tex.a * pulse;

#if defined(_OUTLINE_ON) || defined(_GLOW_ON)
                // 4-tap neighbor sample for edge detect. Shared between outline + glow
                // to keep tex fetches at 5 total when both keywords on.
                float aL = tex2D(_MainTex, i.uv + float2(-_OutlineThickness, 0)).a;
                float aR = tex2D(_MainTex, i.uv + float2( _OutlineThickness, 0)).a;
                float aU = tex2D(_MainTex, i.uv + float2(0,  _OutlineThickness)).a;
                float aD = tex2D(_MainTex, i.uv + float2(0, -_OutlineThickness)).a;
                float neighborMax = max(max(aL, aR), max(aU, aD));
                // edgeMask high where a neighbor is opaque but center is transparent.
                float edgeMask = saturate(neighborMax - tex.a);
#endif

#ifdef _OUTLINE_ON
                // Soft outline ramp — avoids stair-step on diagonals.
                float outlineFactor = smoothstep(0.05, 0.5, edgeMask);
                // Bright-white-but-tinted: lerp pure white toward tint by _OutlineMix.
                float3 outlineRGB = lerp(_OutlineColor.rgb, _OutlineColor.rgb * _Color.rgb, _OutlineMix);
                mono = lerp(mono, outlineRGB, outlineFactor);
                alpha = max(alpha, outlineFactor * _OutlineColor.a * pulse);
#endif

#ifdef _GLOW_ON
                // Faint tint halo riding the silhouette. Falls off naturally with edgeMask.
                float glow = edgeMask * _GlowStrength;
                mono += _Color.rgb * glow;
                alpha = max(alpha, glow * pulse);
#endif

                return fixed4(mono, alpha);
            }
            ENDCG
        }
    }

    Fallback "Unlit/Transparent"
}
