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

// The meadow mesh uses the same field. The broad and fine groups turn in different directions.
float3 GrasslandTussockCanopy(float2 metres, float moisture)
{
    const float2 along = float2(0.8, 0.6), across = float2(-0.6, 0.8);
    float2 rotated = float2(dot(along, metres), dot(across, metres));
    float2 broadScale = float2(51.0, 37.0), fineScale = float2(12.0, 7.4);
    float3 broad = GrasslandNoiseAndSlope(metres / broadScale + float2(8.0, -4.0));
    float3 fine = GrasslandNoiseAndSlope(rotated / fineScale + float2(17.0, -9.0));
    float field = 0.74 * (0.63 * broad.x + 0.37 * fine.x) + 0.26 * moisture;
    float t = saturate((field - 0.18) / 0.62);
    float2 fineSlope = fine.yz / fineScale;
    float2 slope = 0.63 * broad.yz / broadScale + 0.37 * (fineSlope.x * along + fineSlope.y * across);
    slope *= (0.74 * 6.0 / 0.62) * t * (1.0 - t);
    return float3(t * t * (3.0 - 2.0 * t), slope);
}

float GrasslandColonyAmount(float eyeDistance)
{
    return smoothstep(28.0, 76.0, eyeDistance);
}

float2 GrasslandTerrainSlope(float3 normal)
{
    return normal.xz * float2(-1.0, 1.0) / max(0.36, normal.y);
}

// The terrain height bends the bands around the slopes. Broad patches break up bands of equal height.
// Return canopy growth and its east/south derivatives in metres for both the mesh and distant shading.
float3 GrasslandColony(float2 metres, float groundHeight, float2 terrainSlope, float moisture)
{
    const float2 along = float2(0.80, 0.60);
    const float2 drift = float2(-0.019, 0.023);
    const float2 broadScale = float2(62.0, 41.0);
    const float2 localScale = float2(10.0, 8.0);
    float3 broad = GrasslandNoiseAndSlope(metres / broadScale + float2(8.0, -4.0));
    float contour = groundHeight + 5.2 * (broad.x - 0.5) + dot(metres, drift);
    float3 band = GrasslandNoiseAndSlope(float2(dot(along, metres) / 42.0, contour / 3.6) + float2(12.4, -6.1));
    float3 local = GrasslandNoiseAndSlope(metres / localScale + float2(31.0, -16.0));
    float field = 0.32 * broad.x + 0.43 * band.x + 0.10 * local.x + 0.15 * moisture;
    float2 contourSlope = terrainSlope + 5.2 * broad.yz / broadScale + drift;
    float2 gradient = 0.32 * broad.yz / broadScale + 0.10 * local.yz / localScale
        + 0.43 * (band.y * along / 42.0 + band.z * contourSlope / 3.6);
    float t = saturate((field - 0.25) / 0.48);
    return float3(t * t * (3.0 - 2.0 * t), gradient * ((6.0 / 0.48) * t * (1.0 - t)));
}

float GrasslandColonyHeight(float growth)
{
    return 0.14 + 1.07 * growth * growth;
}

float3 GrasslandColonyNormal(float3 terrainNormal, float3 colony, float amount)
{
    float2 rise = (2.14 * colony.x * amount) * colony.yz;
    return normalize(terrainNormal + float3(-rise.x, 0.0, rise.y) * terrainNormal.y);
}

half3 GrasslandColonyColor(half growth)
{
    return lerp(half3(0.459h, 0.425h, 0.304h), half3(0.315h, 0.366h, 0.232h), growth);
}

half GrasslandColonyOcclusion(half growth)
{
    return lerp(0.82h, 0.62h, growth);
}

// Growth already varies smoothly. Treat it as optical depth so the shade does not acquire a second threshold.
half GrasslandColonyTransmission(half growth, half sunElevation)
{
    return exp2(-1.28h * growth * lerp(1.0h, 0.44h, saturate(sunElevation)));
}

half GrasslandColonyDiffuse(half facing, half transmission)
{
    return (0.10h + 0.90h * facing) * transmission;
}

// Directional foliage texture, filtered in screen space as the canopy recedes.
half GrasslandColonyGrain(float2 metres, half growth)
{
    float2 flow = float2(dot(metres, float2(0.80, 0.60)), dot(metres, float2(-0.60, 0.80)));
    float2 mid = flow * float2(0.24, 0.68);
    float2 fine = flow * float2(2.3, 7.2) + float2(0.0, 1.8 * growth);
    float midWeight = 1.0 - smoothstep(0.31, 1.36, max(length(ddx(mid)), length(ddy(mid))));
    float fineWeight = 1.0 - smoothstep(0.37, 1.34, max(length(ddx(fine)), length(ddy(fine))));
    half grain = 1.0h;
    [branch] if (midWeight > 0.001)
        grain += (GrasslandNoise(mid + float2(9.0, -2.0)) - 0.5) * midWeight * 0.43;
    [branch] if (fineWeight > 0.001)
        grain += (GrasslandNoise(fine + float2(-13.0, 6.0)) - 0.5) * fineWeight * 0.34;
    return grain;
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
