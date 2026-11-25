// Coordinates are metres in the common frame of the inner terrain, outer terrain and fallen blocks.
#ifndef REGION_SANDSTONE_INCLUDED
#define REGION_SANDSTONE_INCLUDED

float SandHash(float2 cell)
{
    float3 p = frac(cell.xyx * float3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yzx + 33.33);
    return frac((p.x + p.y) * p.z);
}

float SandNoise(float2 uv)
{
    float2 cell = floor(uv);
    float2 f = frac(uv);
    f = f * f * (3.0 - 2.0 * f);
    float n = lerp(lerp(SandHash(cell), SandHash(cell + float2(1, 0)), f.x),
                   lerp(SandHash(cell + float2(0, 1)), SandHash(cell + 1.0), f.x), f.y);
    float2 footprint = fwidth(uv);
    return lerp(n, 0.5, smoothstep(0.35, 1.2, max(footprint.x, footprint.y)));
}

float SandTriplanar(float3 p, float3 weights)
{
    return dot(weights, float3(SandNoise(p.zy), SandNoise(p.xz + 7.31), SandNoise(p.xy - 2.19)));
}

// A broken bedding plane, fading to its mean when several laminae fit inside a pixel.
float SandSeam(float phase, float width)
{
    float footprint = max(fwidth(phase), 0.002);
    float dist = abs(frac(phase + 0.5) - 0.5);
    float edge = min(0.23, 0.5 * footprint);
    float coverage = 1.0 - smoothstep(width - edge, width + edge, dist);
    return lerp(coverage, 2.0 * width, smoothstep(0.20, 0.65, footprint));
}

// Angular chips in size- and position-jittered cells. Opposite facets pick up light and shade.
float SandChips(float2 uv)
{
    float2 id = floor(uv);
    float choice = SandHash(id);
    float radius = 0.05 + 0.22 * choice * choice;
    float2 center = lerp(radius, 1.0 - radius, float2(SandHash(id + 13.3), SandHash(id - 7.1)));
    float2 offset = (frac(uv) - center) / radius;
    float angle = SandHash(id + 3.8) * 6.283185;
    float2 axis = float2(cos(angle), sin(angle));
    float2 q = float2(dot(offset, axis), dot(offset, float2(-axis.y, axis.x)));
    float outline = max(abs(q.x) + 0.42 * abs(q.y), abs(q.y * 1.45 + 0.32 * q.x));
    float2 footprint = fwidth(uv);
    float mask = (1.0 - smoothstep(0.80, 1.0, outline))
               * (1.0 - smoothstep(0.10, 0.46, max(footprint.x, footprint.y)));
    float facet = smoothstep(-0.50, 0.36, offset.x - 0.7 * offset.y);
    return mask * (facet * 0.31 - 0.18 + 0.28 * (SandHash(id - 2.4) - 0.5));
}

// Four surfaces: exposed bedrock, talus, sand/soil and patchy dry ground cover.
// surface = talus density, metres above the caprock contact, moisture for cover, dry-land coverage.
half3 SandstoneSurface(float3 p, float3 n, float4 surface, half tint, half looseRock)
{
    float3 weights = n * n;
    weights *= weights;
    weights /= max(dot(weights, float3(1, 1, 1)), 0.001);
    float mottling = SandTriplanar(p * 0.22, weights);
    float grains = SandTriplanar(p * 9.7, weights);
    float broad = SandNoise(p.xz * 0.023);
    float broken = SandTriplanar(p * 0.065 + 3.5, weights);
    float mineral = 0.63 * mottling + 0.37 * grains;
    if (looseRock > 0.5)
    {
        float3 block = lerp(float3(0.56, 0.37, 0.28), float3(0.85, 0.62, 0.44), broken);
        return block * (0.79 + 0.43 * mineral) * (0.74 + 0.36 * tint);
    }

    float bedHeight = surface.y + 1.8 * (broad - 0.5) + 0.28 * (mottling - 0.5);
    float bed = bedHeight * 0.12 + 0.78 * SandNoise(float2(bedHeight * 0.072, 5.4));
    float unit = SandNoise(float2(bedHeight * 0.28, 3.7));
    float3 rock = lerp(float3(0.64, 0.31, 0.20), float3(0.91, 0.64, 0.40), 0.62 * unit + 0.38 * broken);
    float mud = (1.0 - smoothstep(-6.0, 1.5, bedHeight)) * smoothstep(-38.0, -29.0, bedHeight);
    float3 mudstone = lerp(float3(0.42, 0.32, 0.35), float3(0.73, 0.58, 0.54), unit);
    rock = lerp(rock, mudstone, mud);

    // Finer wind-deposited sets change direction at the erosional boundary between beds.
    float set = floor(bed);
    float2 dip = lerp(float2(0.29, 0.37), float2(0.48, 0.63), SandHash(float2(set, 17.0)));
    dip.y *= SandHash(float2(set, 1.3)) > 0.5 ? 1.0 : -1.0;
    float lamina = (bedHeight + dot(dip, p.xz)) * 0.93 + 2.1 * broken + 0.38 * mottling;
    lamina += 0.42 * SandNoise(float2(lamina * 0.47, 3.2));
    float crossBed = SandSeam(lamina, 0.13) * smoothstep(0.03, 0.22, frac(bed))
                   * (1.0 - smoothstep(0.68, 0.98, frac(bed))) * smoothstep(0.20, 0.68, broken);
    rock += float3(0.22, 0.18, 0.12) * crossBed;
    float seam = (0.06 + 0.17 * broken) * SandSeam(bed, 0.018 + 0.020 * mottling);
    float pores = SandTriplanar(p * 1.82 + mottling, weights);
    rock *= 0.76 + 0.40 * mineral + 0.13 * (pores - 0.5) - seam;

    // Runoff follows gravity on each cliff face. Its extent breaks up vertically and between outcrops.
    float3 streakPos = p * float3(0.52, 0.027, 0.52);
    streakPos.xz += 0.34 * mottling;
    float streak = SandTriplanar(streakPos, weights);
    float patches = SandTriplanar(p * float3(0.085, 0.033, 0.085) + 21.3, weights);
    float varnish = smoothstep(0.54, 0.79, streak) * smoothstep(0.37, 0.67, patches)
                  * (1.0 - smoothstep(0.38, 0.73, n.y));
    rock = lerp(rock, float3(0.27, 0.20, 0.18) * (0.78 + 0.50 * mineral), varnish * 0.88);

    float sandPatch = 0.52 * broad + 0.30 * mottling + 0.18 * grains;
    float3 soil = lerp(float3(0.56, 0.40, 0.29), float3(0.87, 0.71, 0.51), sandPatch);
    float cover = smoothstep(0.60, 0.78, 0.56 * broad + 0.44 * mottling)
                * surface.z * smoothstep(0.78, 0.97, n.y);
    soil = lerp(soil, float3(0.43, 0.41, 0.28) * (0.65 + 0.70 * grains), cover);
    float2 ground = float2(0.8 * p.x + 0.6 * p.z, -0.6 * p.x + 0.8 * p.z);
    float2 drift = float2(mottling, pores);
    float gravel = SandChips(ground * 4.6 + drift) + 0.75 * SandChips(p.xz * 13.3 - drift);
    float3 rubble = lerp(float3(0.54, 0.40, 0.35), float3(0.77, 0.61, 0.50), sandPatch);
    rubble = lerp(rubble, mudstone, mud * 0.68);
    float talus = smoothstep(0.04, 0.82, surface.x);
    float driftSand = smoothstep(0.47, 0.80, 0.62 * mottling + 0.38 * broken);
    float3 groundColor = lerp(soil, rubble, talus * (1.0 - 0.56 * driftSand));
    groundColor *= 0.82 + 0.24 * grains + 0.14 * pores + (0.55 + talus) * gravel;
    float rockFace = 1.0 - smoothstep(0.52, 0.82, n.y);
    return lerp(groundColor, rock, rockFace);
}

#endif
