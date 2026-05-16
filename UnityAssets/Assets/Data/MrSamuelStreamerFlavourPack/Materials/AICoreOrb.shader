// MSSFP AICoreOrb — radial gradient orb shader for the AI Core building's floating sphere.
//
// Pipeline per fragment:
//   1. Sample _MainTex.a as the orb silhouette mask (white-filled circle on transparent).
//   2. Distance(uv, _GradientCenter.xy) / _GradientRadius -> normalized radial t in [0,1].
//   3. Lerp through 4 colour stops (matches the SVG source radialGradient2):
//        stop 0   #1deeff
//        stop 0.30 #c2edc1
//        stop 0.70 #1deeff
//        stop 1   #1deeff
//      Stops are hard-coded — the SVG art direction is fixed; no per-material colour wiring
//      needed. If a future persona-coloured variant is wanted, expose them as Properties.
//   4. RGB = ramped colour, alpha = ramped alpha × tex.a × _Opacity.
//
// _GradientCenter (Vector) is set per-frame by Building_AICore.DrawAt via
// MaterialPropertyBlock — that's the xeyes mouse-tracking hook. Default (0.55, 0.39) places
// the highlight on the upper-right at rest, matching the SVG specular.
//
// Transparent blend so the orb composites cleanly over the building's static pedestal sprite
// drawn underneath by the map-mesh batch. ZWrite Off + Cull Off for the same reasons HoloMono
// uses them.
Shader "MSSFP/AICoreOrb"
{
    Properties
    {
        _MainTex ("Orb Mask", 2D) = "white" {}
        // Default sits at SVG rest position: (0.5,0.5) + baseOffset (+0.05,+0.126 in UV-up space)
        // with the gradient-follow scale baked in is 0 (no cursor) -> centre = (0.55,0.626).
        // Wait — base only without follow displacement; that's what we want at "no cursor"
        // (which the C# side never hits — DrawAt always pushes a real value).
        _GradientCenter ("Gradient Centre (UV)", Vector) = (0.55, 0.626, 0, 0)
        _GradientRadius ("Gradient Radius (UV)", Range(0.05, 1.5)) = 0.7
        _Opacity ("Opacity Multiplier", Range(0, 1)) = 0.75
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
            float4 _GradientCenter;
            float _GradientRadius;
            float _Opacity;

            // SVG radialGradient2 stops, matching #1deeff -> #c2edc1 -> #1deeff -> #1deeff.
            // Hex -> linear (gamma 1.0 input; Unity will linearise if project is linear).
            static const float3 STOP0 = float3(0.114, 0.933, 1.000); // #1deeff
            static const float3 STOP1 = float3(0.878, 0.961, 0.875); // #e0f5df — pale mint, lighter than original #c2edc1
            static const float3 STOP2 = float3(0.114, 0.933, 1.000); // #1deeff
            static const float3 STOP3 = float3(0.114, 0.933, 1.000); // #1deeff
            static const float OFFSET1 = 0.30462432;
            static const float OFFSET2 = 0.6950779;
            static const float OFFSET3 = 1.0;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            // 4-stop piecewise linear ramp on t in [0,1].
            float3 SampleRamp(float t)
            {
                if (t < OFFSET1)
                {
                    float k = t / OFFSET1;
                    return lerp(STOP0, STOP1, k);
                }
                if (t < OFFSET2)
                {
                    float k = (t - OFFSET1) / (OFFSET2 - OFFSET1);
                    return lerp(STOP1, STOP2, k);
                }
                float k = saturate((t - OFFSET2) / (OFFSET3 - OFFSET2));
                return lerp(STOP2, STOP3, k);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                float2 d = i.uv - _GradientCenter.xy;
                float t = saturate(length(d) / _GradientRadius);
                float3 rgb = SampleRamp(t);
                float alpha = tex.a * _Opacity;
                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }

    Fallback "Unlit/Transparent"
}
