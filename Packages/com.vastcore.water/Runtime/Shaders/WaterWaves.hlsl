#ifndef VASTCORE_WATER_WAVES_INCLUDED
#define VASTCORE_WATER_WAVES_INCLUDED

TEXTURE2D(_RippleNormal); SAMPLER(sampler_RippleNormal);
TEXTURE2D(_FoamMask); SAMPLER(sampler_FoamMask);

CBUFFER_START(UnityPerMaterial)
    float4 _Wave;      // amplitude, wave number, angular frequency, steepness
    float4 _Motion;    // direction XZ, chop, foam
    float4 _Bank;      // barrier flag, beach slope, barrier height, receiving height
    float4 _Shore;     // water elevation, barrier width, receiving width, breaker distance
    float4 _DeepWater;
    float4 _ShallowWater;
    float4 _BedTint;
    float _WaterTime;
CBUFFER_END

float ShoreFoam(float2 metres)
{
    float a = SAMPLE_TEXTURE2D(_FoamMask, sampler_FoamMask, metres / 2.4).r;
    float2 rotated = float2(metres.y - metres.x * 0.21, -metres.x - metres.y * 0.21);
    float b = SAMPLE_TEXTURE2D(_FoamMask, sampler_FoamMask, rotated * 0.21 + 0.45).r;
    return saturate(a * 0.76 + b * 0.47);
}

float MainWavePhase(float2 localXZ)
{
    float along = localXZ.y;
    float variation = 0.31 * sin(along * 0.136) + 0.16 * sin(along * 0.43 + 1.5);
    return _Wave.y * dot(_Motion.xy, localXZ) - _Wave.z * _WaterTime + variation;
}

float ShoreDepth(float distance)
{
    return lerp(max(0.0, -distance * _Bank.y), 2.5 - distance * 0.095, _Bank.x);
}

float ShoreOvertopping(float z)
{
    float group = 1.16 + 0.34 * sin(z * 0.145) + 0.12 * sin(z * 0.51 + 1.1);
    return saturate((_Wave.x * group - _Bank.z) / (0.38 + _Wave.x * 0.34));
}

void AddWave(float2 direction, float angle, float k, float amplitude, float slope,
    inout float3 displacement, inout float3 dx, inout float3 dz)
{
    float sn, cs;
    sincos(angle, sn, cs);
    float horizontalAmplitude = min(amplitude * 0.86, slope / k);
    float3 shift = float3(-horizontalAmplitude * sn * direction.x, amplitude * cs,
                         -horizontalAmplitude * sn * direction.y);
    float3 derivative = k * float3(-horizontalAmplitude * cs * direction.x, -amplitude * sn,
                                   -horizontalAmplitude * cs * direction.y);
    displacement += shift;
    dx += derivative * direction.x;
    dz += derivative * direction.y;
}

float3 SeaDisplacement(float2 p, float distance, out float3 normal)
{
    float3 move = 0;
    float3 dx = float3(1, 0, 0);
    float3 dz = float3(0, 0, 1);
    // Fade the displaced mesh before the coarse outer grid and lock the shoreline's horizontal edge.
    float detail = smoothstep(-280, -98, distance) * (1.0 - 0.92 * smoothstep(165, 370, abs(p.y)));
    float beach = 0.58 + 0.28 * exp(-abs(distance + _Shore.w) * 0.13);
    beach *= smoothstep(0.0, min(_Shore.w, 3.7), -distance);
    float amplitude = _Wave.x * detail * lerp(beach, 1.0, _Bank.x);
    float mainPhase = MainWavePhase(p);
    AddWave(_Motion.xy, mainPhase, _Wave.y, amplitude * 0.84, _Wave.w * 0.75, move, dx, dz);
    float2 second = normalize(float2(_Motion.x + 0.15, _Motion.y - 0.47));
    float k2 = _Wave.y * 1.89;
    AddWave(second, dot(second, p) * k2 - _WaterTime * _Wave.z * 1.47 + 1.3,
        k2, amplitude * 0.25 * _Motion.z, _Wave.w * 0.17, move, dx, dz);
    float2 third = normalize(float2(_Motion.x, _Motion.y + 0.81));
    float k3 = _Wave.y * 4.35;
    AddWave(third, dot(third, p) * k3 - _WaterTime * _Wave.z * 1.95,
        k3, amplitude * 0.065 * _Motion.z, _Wave.w * 0.08, move, dx, dz);
    move.xz *= smoothstep(0.0, 6.0, -distance);
    normal = normalize(cross(dz, dx));
    return move;
}

#endif
