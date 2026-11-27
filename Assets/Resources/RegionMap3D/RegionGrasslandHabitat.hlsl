#ifndef REGION_GRASSLAND_HABITAT_INCLUDED
#define REGION_GRASSLAND_HABITAT_INCLUDED

// Same hash and cubic interpolation as RegionGrasslandCover.
float GrasslandHash(int2 p)
{
    uint h = 617u ^ 0x9E3779B9u;
    h ^= (uint)p.x * 0x85EBCA6Bu;
    h = ((h << 13) | (h >> 19)) * 0xC2B2AE35u;
    h ^= (uint)p.y * 0x27D4EB2Fu;
    h = ((h << 15) | (h >> 17)) * 0x165667B1u;
    h = (h ^ (h >> 16)) * 0x85EBCA6Bu;
    h = (h ^ (h >> 13)) * 0xC2B2AE35u;
    h ^= h >> 16;
    return (h >> 8) * (1.0 / 16777216.0);
}

float GrasslandNoise(float2 p)
{
    int2 cell = (int2)floor(p);
    float2 f = smoothstep(0.0, 1.0, frac(p));
    return lerp(
        lerp(GrasslandHash(cell), GrasslandHash(cell + int2(1, 0)), f.x),
        lerp(GrasslandHash(cell + int2(0, 1)), GrasslandHash(cell + 1), f.x),
        f.y);
}

float GrasslandGrowth(float2 metres, float moisture)
{
    float patch = 0.64 * GrasslandNoise(metres / 37.0) + 0.36 * GrasslandNoise(metres / 8.0 + float2(17.0, -9.0));
    return smoothstep(0.16, 0.83, 0.78 * patch + 0.22 * moisture);
}

// Return the height and slope of the same elongated grass colonies used to place the meshes.
float3 GrasslandNoiseAndSlope(float2 p)
{
    int2 cell = (int2)floor(p);
    float2 t = frac(p);
    float2 f = t * t * (3.0 - 2.0 * t);
    float a = GrasslandHash(cell);
    float b = GrasslandHash(cell + int2(1, 0));
    float c = GrasslandHash(cell + int2(0, 1));
    float d = GrasslandHash(cell + 1);
    return float3(lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y),
        lerp(b - a, d - c, f.y) * 6.0 * t.x * (1.0 - t.x),
        lerp(c - a, d - b, f.x) * 6.0 * t.y * (1.0 - t.y));
}

float3 GrasslandCanopy(float2 metres, float moisture)
{
    float2 along = float2(0.8, 0.6), across = float2(-0.6, 0.8);
    float2 rotated = float2(dot(along, metres), dot(across, metres));
    float2 coarseScale = float2(62.0, 29.0);
    float2 fineScale = float2(15.0, 5.5);
    float3 coarse = GrasslandNoiseAndSlope(rotated / coarseScale);
    float3 fine = GrasslandNoiseAndSlope(rotated / fineScale + float2(17.0, -9.0));
    float field = 0.71 * (0.57 * coarse.x + 0.43 * fine.x) + 0.29 * moisture;
    float t = saturate((field - 0.23) / (0.76 - 0.23));
    float2 slope = 0.57 * coarse.yz / coarseScale + 0.43 * fine.yz / fineScale;
    slope *= 0.71 * 6.0 * t * (1.0 - t) / (0.76 - 0.23);
    return float3(t * t * (3.0 - 2.0 * t), slope.x * along + slope.y * across);
}

half3 GrasslandCrownColor(half growth)
{
    return lerp(half3(0.52h, 0.48h, 0.34h), half3(0.285h, 0.375h, 0.235h), growth);
}

half GrasslandTrailWear(float2 offset)
{
    float crossTrack = abs(offset.y - (2.0 - 0.48 * offset.x + 0.65 * sin(0.29 * offset.x)));
    crossTrack += (GrasslandNoise(offset * 1.3) - 0.5) * 0.27;
    return (1.0 - smoothstep(0.30, 0.85, crossTrack)) * (1.0 - smoothstep(14.0, 25.0, abs(offset.x)));
}

#endif
