// ── BlackHoleRayMarch.shader ─────────────────────────────────────────────────
// Interstellar / Gargantua-style accretion disc in 2D viewport UV space.
//
// Key visual: the near-side accretion disc passes IN FRONT of the event horizon
// shadow, so a bright horizontal stripe is visible crossing the dark circle.
// The shader therefore does NOT do an early return for r < Rh; instead it blends
// between black (shadow) and disc emission based on how close the pixel is to
// the disc plane.
//
// Elements:
//   • Inside shadow (r < Rh): black except where disc plane crosses (inner crossing)
//   • Outside shadow (r > Rh): primary disc band + secondary lensed arc + photon ring
//   • Doppler: fixed 4:1 L/R asymmetry, no rotation

Shader "MSSFP/BlackHoleRayMarch"
{
    Properties
    {
        _SunVP           ("Sun Viewport Pos",              Vector) = (0.5, 0.5, 0, 0)
        _HorizonRadius   ("Horizon Radius (VP-Y frac)",    Float)  = 0.22
        _DiscOuterRadius ("Disc Outer Radius (VP-Y frac)", Float)  = 0.33
        _Aspect          ("Aspect Ratio",                  Float)  = 1.777
        _DiscSpeed       ("Disc Animation Speed",          Float)  = 3.5
        _DiscAngle       ("Disc Orientation Angle (rad)",  Float)  = 0.0
        _DiscTex         ("Disc Texture (unused)",         2D)     = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="Background+1" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0
            #include "UnityCG.cginc"

            float4    _SunVP;
            float     _HorizonRadius;
            float     _DiscOuterRadius;
            float     _Aspect;
            float     _DiscSpeed;
            float     _DiscAngle;
            sampler2D _DiscTex;

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 screenUV : TEXCOORD0;
            };

            v2f vert(float4 posOS : POSITION)
            {
                v2f o;
                o.pos      = UnityObjectToClipPos(posOS);
                o.screenUV = posOS.xy;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv  = (i.screenUV - _SunVP.xy) * float2(_Aspect, 1.0);
                float  r   = length(uv);

                float Rh = _HorizonRadius;
                float Ro = _DiscOuterRadius;

                if (r > Ro * 1.5) return float4(0.0, 0.0, 0.0, 0.0);

                // ── Disc plane: oriented in world space, rotated to camera ────
                // _DiscAngle is the screen-space angle of the disc's "right" axis
                // (computed from the projection of a fixed world-space vector).
                // Rotating uv by -_DiscAngle aligns uv.x with the disc midplane
                // (Doppler direction) and uv.y with the disc normal (band height).
                float  cosA  = cos(_DiscAngle);
                float  sinA  = sin(_DiscAngle);
                float2 uvDisc = float2( uv.x * cosA + uv.y * sinA,
                                       -uv.x * sinA + uv.y * cosA);
                float discV = uvDisc.y;
                float discH = uvDisc.x;

                // ── Doppler: ~1.5:1 ratio, left bright ───────────────────────
                float nx      = -discH / max(r, 0.001);
                float doppler = lerp(0.65, 1.0, saturate(0.5 + nx * 0.60));

                float3 cI = float3(1.00, 0.97, 0.90);
                float3 cM = float3(1.00, 0.60, 0.12);
                float3 cO = float3(0.90, 0.22, 0.02);

                // ── INSIDE SHADOW ─────────────────────────────────────────────
                // Near-side disc crosses in front of the shadow as a thin stripe.
                // Rest of shadow interior is opaque black.
                // crossThick kept thin so the crossing band looks sharp.
                // NOTE: u=0 (saturated) everywhere inside, so we don't use the
                // outer radial formula here — that would blow out the whole interior.
                if (r < Rh)
                {
                    float crossThick = Rh * 0.16;
                    float cross = exp(-(discV * discV) / (crossThick * crossThick));
                    float crossI = cross * doppler * 2.8;
                    float3 col = lerp(float3(0.0, 0.0, 0.0), cI, saturate(crossI));
                    return float4(col, 1.0);
                }

                // ── OUTSIDE SHADOW ────────────────────────────────────────────
                float u = saturate((r - Rh) / max(Ro - Rh, 0.001));

                float3 discCol = u < 0.5
                    ? lerp(cI, cM,  u * 2.0)
                    : lerp(cM, cO, (u - 0.5) * 2.0);

                float radial = (1.0 + 2.5 * exp(-u * 8.0)) * exp(-u * 2.5);
                float thick  = Rh * 0.20;   // thinner than before to keep disc sharp
                float band   = exp(-(discV * discV) / (thick * thick));
                float pulse  = 0.88 + 0.12 * sin(u * 8.0 + _Time.y * _DiscSpeed * 0.50);
                float discI  = band * radial * doppler * pulse * 4.5;

                float3 col = discCol * discI;

                // Secondary arc: thin lensed ring above disc plane (gravitational lensing)
                float arcR    = Rh * 1.18;
                float arcW    = Rh * 0.07;
                float arcShp  = exp(-abs(r - arcR) / arcW);
                float dVarc   = discV - Rh * 0.08;
                float arcBand = exp(-(dVarc * dVarc) / (Rh * Rh * 0.09));
                float dopArc  = lerp(0.65, 1.0, saturate(0.5 + nx * 0.50));
                col += cM * arcShp * arcBand * dopArc * 3.5;

                // Photon ring: thin bright ring at shadow edge, strongest at disc crossing
                float photon = exp(-abs(r - Rh * 1.065) / (Rh * 0.040)) * 5.5 * doppler;
                photon *= (0.25 + 0.75 * band);
                col += float3(1.0, 0.93, 0.78) * photon;

                // Luminance-based alpha — ensures arc + photon ring are visible
                float alpha = saturate(length(col) * 0.72);
                return float4(col, alpha);
            }
            ENDCG
        }
    }
}
