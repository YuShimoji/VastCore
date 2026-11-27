#ifndef VASTCORE_SHORE_FLOW_INCLUDED
#define VASTCORE_SHORE_FLOW_INCLUDED

float ShorePhase(float2 localXZ, float waveNumber, float2 direction, float elapsedRadians)
{
    float z = localXZ.y;
    float variation = 0.31 * sin(z * 0.136) + 0.16 * sin(z * 0.43 + 1.5);
    return waveNumber * dot(direction, localXZ) - elapsedRadians + variation;
}

float ShoreImpactGroup(float z)
{
    return 0.66 + 0.22 * sin(z * 0.194 + sin(z * 0.091) * 2.0) + 0.12 * sin(z * 0.71);
}

float OvertoppingAt(float z, float amplitude, float crest)
{
    float group = 1.16 + 0.34 * sin(z * 0.145) + 0.12 * sin(z * 0.51 + 1.1);
    return saturate((amplitude * group - crest) / (0.38 + amplitude * 0.34));
}

float MaximumRunup(float z, float amplitude, float4 bank, float4 shore)
{
    float group = ShoreImpactGroup(z);
    if (bank.x > 0.5)
        return shore.z * OvertoppingAt(z, amplitude, bank.z) * lerp(0.52, 1.0, group);
    return min(shore.z, amplitude * 0.63 / bank.y) * lerp(0.62, 1.0, group);
}

float RunupPosition(float age, bool barrier)
{
    float rise = smoothstep(barrier ? 0.088 : 0.085, barrier ? 0.34 : 0.40, age);
    float fall = smoothstep(barrier ? 0.45 : 0.49, barrier ? 0.81 : 0.94, age);
    return rise * (1.0 - fall);
}

float ReceivingWetness(float distance, float2 position, float4 wave, float2 direction,
    float4 bank, float4 shore, float seconds, float wetHoldSeconds)
{
    if (wave.x < 0.002 || wetHoldSeconds < 0.01) return 0.0;
    bool barrier = bank.x > 0.5;
    float inland = distance - (barrier ? shore.y : 0.0);
    float extent = MaximumRunup(position.y, wave.x, bank, shore);
    if (inland < 0.0 || inland > extent || extent < 0.01) return 0.0;
    float contactX = position.x - distance - (barrier ? 0.0 : shore.w);
    float age = frac(-ShorePhase(float2(contactX, position.y), wave.y, direction, wave.z * seconds) * 0.15915494);
    float arrival = lerp(barrier ? 0.088 : 0.085, barrier ? 0.34 : 0.40, saturate(inland / extent));
    float remaining = 1.0 - smoothstep(extent * 0.77, extent, inland);
    float lifetime = min(1.75, (barrier ? 0.81 : 0.94) + wetHoldSeconds * wave.z * 0.15915494);
    // The later of the two usable strikes supplies the stain before the next wave arrives.
    float wetNow = smoothstep(arrival - 0.018, arrival + 0.049, age) * (1.0 - smoothstep(lifetime - 0.22, lifetime, age));
    float previous = 1.0 - smoothstep(lifetime - 0.22, lifetime, age + 1.0);
    return remaining * max(wetNow, previous);
}

#endif
