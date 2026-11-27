#ifndef VASTCORE_WATER_LIGHTING_INCLUDED
#define VASTCORE_WATER_LIGHTING_INCLUDED

float WaterHash(float2 cell)
{
    float3 p = frac(cell.xyx * 0.1031);
    p += dot(p, p.yzx + 33.33);
    return frac((p.x + p.y) * p.z);
}

float WaterNoise(float2 p)
{
    float2 cell = floor(p);
    float2 blend = frac(p);
    blend = blend * blend * (3.0 - 2.0 * blend);
    return lerp(lerp(WaterHash(cell), WaterHash(cell + float2(1, 0)), blend.x),
                lerp(WaterHash(cell + float2(0, 1)), WaterHash(cell + 1), blend.x), blend.y);
}

float3 WaterSky(float3 direction)
{
    float height = saturate(direction.y);
    float3 horizon = float3(0.33, 0.40, 0.45);
    float3 zenith = float3(0.115, 0.185, 0.27);
    float2 dome = direction.xz * (3.4 / (height + 0.38));
    float cloud = WaterNoise(dome * float2(0.62, 0.88)) * 0.51 + WaterNoise(dome * 1.39 + 5) * 0.29
        + WaterNoise(dome * 3.15 - 3) * 0.13 + WaterNoise(dome * 6.7 + 19) * 0.07;
    float cover = smoothstep(0.32, 0.69, cloud);
    float3 layer = lerp(float3(0.10, 0.142, 0.199), float3(0.38, 0.42, 0.46), cover);
    float light = pow(saturate(dot(direction, normalize(float3(-0.65, 0.20, -0.56)))), 5.0);
    return lerp(horizon, lerp(zenith, layer, 0.83), smoothstep(0.006, 0.29, height))
        + float3(0.11, 0.081, 0.052) * light;
}

float3 WaterDistanceFog(float3 color, float3 worldPosition)
{
    float3 ray = worldPosition - GetCameraPositionWS();
    float distance = length(ray);
    float3 horizon = float3(0.33, 0.40, 0.45);
    return lerp(horizon, color, exp2(-distance * 0.0021));
}

#endif
