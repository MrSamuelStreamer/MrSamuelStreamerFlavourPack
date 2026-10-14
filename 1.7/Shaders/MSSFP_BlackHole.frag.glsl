#version 450

// Interstellar / Gargantua-style accretion disc in 2D screen space.
// GLSL port of UnityAssets/.../BlackHoleRayMarch.shader (the 1.6 Unity shader); keep the two in step.
//
// The near-side accretion disc passes in front of the event horizon shadow, so a bright
// stripe crosses the dark circle. All radii are in "viewport-Y fraction" units, the same
// units the 1.6 shader used; the quad's UV is mapped back into those units below.

layout(location = 1) in vec2 vUv;

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Params; // @default = vec4(0.0, 0.22, 0.33, 0.5) ; x = time (s), y = horizon radius, z = disc outer radius, w = quad half extent
    vec4 _Disc;   // @default = vec4(0.0, 3.5, 0.0, 0.0) ; x = disc angle (rad), y = disc animation speed
} uMaterial;

void main()
{
    float time = uMaterial._Params.x;
    float Rh = uMaterial._Params.y;
    float Ro = uMaterial._Params.z;
    float halfExtent = uMaterial._Params.w;
    float discAngle = uMaterial._Disc.x;
    float discSpeed = uMaterial._Disc.y;

    // Quad UV (0..1, u = screen right, v = screen up) -> isotropic screen units centred on the sun.
    vec2 uv = (vUv - 0.5) * 2.0 * halfExtent;
    float r = length(uv);

    if (r > Ro * 1.5)
    {
        outColor = vec4(0.0);
        return;
    }

    // Disc plane: rotate uv so uvDisc.x runs along the disc midplane (Doppler direction)
    // and uvDisc.y along the disc normal (band height).
    float cosA = cos(discAngle);
    float sinA = sin(discAngle);
    vec2 uvDisc = vec2(uv.x * cosA + uv.y * sinA,
                      -uv.x * sinA + uv.y * cosA);
    float discV = uvDisc.y;
    float discH = uvDisc.x;

    // Doppler: ~1.5:1 ratio, left side bright.
    float nx = -discH / max(r, 0.001);
    float doppler = mix(0.65, 1.0, clamp(0.5 + nx * 0.60, 0.0, 1.0));

    vec3 cI = vec3(1.00, 0.97, 0.90);
    vec3 cM = vec3(1.00, 0.60, 0.12);
    vec3 cO = vec3(0.90, 0.22, 0.02);

    // Inside the shadow: opaque black, except where the near-side disc crosses in front.
    if (r < Rh)
    {
        float crossThick = Rh * 0.16;
        float crossBand = exp(-(discV * discV) / (crossThick * crossThick));
        float crossI = crossBand * doppler * 2.8;
        vec3 inner = mix(vec3(0.0), cI, clamp(crossI, 0.0, 1.0));
        outColor = vec4(inner, 1.0);
        return;
    }

    // Outside the shadow.
    float u = clamp((r - Rh) / max(Ro - Rh, 0.001), 0.0, 1.0);

    vec3 discCol = u < 0.5
        ? mix(cI, cM, u * 2.0)
        : mix(cM, cO, (u - 0.5) * 2.0);

    float radial = (1.0 + 2.5 * exp(-u * 8.0)) * exp(-u * 2.5);
    float thick = Rh * 0.20;
    float band = exp(-(discV * discV) / (thick * thick));
    float pulse = 0.88 + 0.12 * sin(u * 8.0 + time * discSpeed * 0.50);
    float discI = band * radial * doppler * pulse * 4.5;

    vec3 col = discCol * discI;

    // Secondary arc: thin lensed ring above the disc plane.
    float arcR = Rh * 1.18;
    float arcW = Rh * 0.07;
    float arcShp = exp(-abs(r - arcR) / arcW);
    float dVarc = discV - Rh * 0.08;
    float arcBand = exp(-(dVarc * dVarc) / (Rh * Rh * 0.09));
    float dopArc = mix(0.65, 1.0, clamp(0.5 + nx * 0.50, 0.0, 1.0));
    col += cM * arcShp * arcBand * dopArc * 3.5;

    // Photon ring: thin bright ring at the shadow edge, strongest at the disc crossing.
    float photon = exp(-abs(r - Rh * 1.065) / (Rh * 0.040)) * 5.5 * doppler;
    photon *= (0.25 + 0.75 * band);
    col += vec3(1.0, 0.93, 0.78) * photon;

    // Luminance-based alpha so the arc and photon ring stay visible.
    float alpha = clamp(length(col) * 0.72, 0.0, 1.0);
    outColor = vec4(col, alpha);
}
