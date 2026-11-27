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

half GrasslandTrailWear(float2 offset)
{
    float crossTrack = abs(offset.y - (2.0 - 0.48 * offset.x + 0.65 * sin(0.29 * offset.x)));
    crossTrack += (GrasslandNoise(offset * 1.3) - 0.5) * 0.27;
    return (1.0 - smoothstep(0.30, 0.85, crossTrack)) * (1.0 - smoothstep(14.0, 25.0, abs(offset.x)));
}

#endif
