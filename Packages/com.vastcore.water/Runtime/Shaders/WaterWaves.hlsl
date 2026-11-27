#ifndef VASTCORE_WATER_WAVES_INCLUDED
#define VASTCORE_WATER_WAVES_INCLUDED

#include "ShoreFlow.hlsl"

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
    float4 _Breaking;  // curl, spray
    float _FilmFoam;
    float4 _SwashShape; // active footprint count, deflection strength
    float4 _SwashObstacles[8];
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
    return ShorePhase(localXZ, _Wave.y, _Motion.xy, _Wave.z * _WaterTime);
}

float ShoreDepth(float distance)
{
    return lerp(max(0.0, -distance * _Bank.y), 2.5 - distance * 0.095, _Bank.x);
}

float ShoreOvertopping(float z)
{
    return OvertoppingAt(z, _Wave.x, _Bank.z);
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

float SeaAmplitude(float2 p, float distance)
{
    // Fade the displaced mesh before the coarse outer grid and lock the shoreline's horizontal edge.
    float detail = smoothstep(-280, -98, distance) * (1.0 - 0.92 * smoothstep(165, 370, abs(p.y)));
    float beach = 0.58 + 0.28 * exp(-abs(distance + _Shore.w) * 0.13);
    beach *= smoothstep(0.0, min(_Shore.w, 3.7), -distance);
    return _Wave.x * detail * lerp(beach, 1.0, _Bank.x);
}

float3 WavePhases(float2 p)
{
    float2 second = normalize(float2(_Motion.x + 0.15, _Motion.y - 0.47));
    float2 third = normalize(float2(_Motion.x, _Motion.y + 0.81));
    return float3(MainWavePhase(p),
        dot(second, p) * (_Wave.y * 1.89) - _WaterTime * _Wave.z * 1.47 + 1.3,
        dot(third, p) * (_Wave.y * 4.35) - _WaterTime * _Wave.z * 1.95);
}

float SeaHeight(float2 p, float distance)
{
    float height = SeaAmplitude(p, distance) * dot(cos(WavePhases(p)), float3(0.84, 0.25 * _Motion.z, 0.065 * _Motion.z));
    if (_Bank.x < 0.5 && distance > -21.0)
        height = max(height, distance * _Bank.y + 0.034);
    return height;
}

float3 SeaDisplacement(float2 p, float distance, out float3 normal)
{
    float3 move = 0;
    float3 dx = float3(1, 0, 0);
    float3 dz = float3(0, 0, 1);
    float amplitude = SeaAmplitude(p, distance);
    float3 phase = WavePhases(p);
    AddWave(_Motion.xy, phase.x, _Wave.y, amplitude * 0.84, _Wave.w * 0.75, move, dx, dz);
    AddWave(normalize(float2(_Motion.x + 0.15, _Motion.y - 0.47)), phase.y, _Wave.y * 1.89,
        amplitude * 0.25 * _Motion.z, _Wave.w * 0.17, move, dx, dz);
    AddWave(normalize(float2(_Motion.x, _Motion.y + 0.81)), phase.z, _Wave.y * 4.35,
        amplitude * 0.065 * _Motion.z, _Wave.w * 0.08, move, dx, dz);
    move.xz *= smoothstep(0.0, 6.0, -distance);
    normal = normalize(cross(dz, dx));
    if (_Bank.x < 0.5 && _Wave.x > 0.002 && distance > -21.0)
    {
        // Keep the draw surface above the same beach bed sampled by the environment.
        float bed = (distance + move.x) * _Bank.y;
        float shallow = saturate((bed + 0.034 - move.y) / 0.16);
        move.y = max(move.y, bed + 0.034);
        normal = normalize(lerp(normal, normalize(float3(-_Bank.y, 1.0, 0.0)), shallow));
    }
    return move;
}

#endif
