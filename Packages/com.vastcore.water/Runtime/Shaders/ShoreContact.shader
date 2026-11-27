Shader "Vastcore/Water/Shore Contact"
{
    Properties
    {
        _DeepWater("Deep Water", Color) = (0.10, 0.24, 0.28, 1)
        _ShallowWater("Shallow Water", Color) = (0.24, 0.44, 0.41, 1)
        [NoScaleOffset] _RippleNormal("Ripple Normal", 2D) = "bump" {}
        [NoScaleOffset] _FoamMask("Foam Mask", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }
        Pass
        {
            Name "ShoreContact"
            Tags { "LightMode" = "UniversalForwardOnly" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WaterLighting.hlsl"
            #include "WaterWaves.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 part : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float4 flow : TEXCOORD2; // distance, along, time since arrival, opacity
                nointerpolation float part : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float z = input.positionOS.z;
                float u = input.uv.x;
                float mode = input.part.x;
                bool barrier = _Bank.x > 0.5;
                float edge = barrier ? 0.0 : -_Shore.w;
                float age = frac(-MainWavePhase(input.positionOS.xz + float2(edge, 0)) * 0.15915494);
                float alongFade = 1.0 - smoothstep(124.0, 273.0, abs(z));
                float group = (0.66 + 0.22 * sin(z * 0.194 + sin(z * 0.091) * 2.0) + 0.12 * sin(z * 0.71));
                float over = ShoreOvertopping(z);
                float amp = _Wave.x;
                float d;
                float y;
                float live;
                float burst = pow(saturate(group * 1.03), 2.6);

                if (mode < 0.5)
                {
                    float advance = smoothstep(barrier ? 0.055 : 0.09, barrier ? 0.30 : 0.38, age);
                    float back = smoothstep(0.44, 0.97, age);
                    float reach = advance * (1.0 - 0.88 * back);
                    float limit = barrier ? _Shore.z * over * lerp(0.52, 1.0, group)
                        : min(_Shore.z, amp * 0.54 / _Bank.y) * lerp(0.62, 1.0, group);
                    float start = barrier ? _Shore.y : 0.0;
                    d = start + u * reach * limit;
                    float runupThickness = 0.007 + 0.029 * (1.0 - u) * reach;
                    y = (barrier ? _Bank.w : d * _Bank.y) + runupThickness;
                    live = alongFade * advance * (1.0 - back) * (barrier ? over : smoothstep(0.045, 0.5, amp));
                }
                else if (mode < 1.5)
                {
                    if (barrier)
                    {
                        float hit = smoothstep(0.006, 0.063, age) * (1.0 - smoothstep(0.16, 0.30, age));
                        float rise = min(amp * 1.88, 3.85) * burst * hit * over;
                        float arc = sin(u * 2.14);
                        float twist = sin(z * 1.7 + u * 3.3) * 0.19 + sin(z * 0.68 - u * 3.4) * 0.22;
                        d = -0.24 + u * u * (0.47 + (2.9 + twist) * hit * over) + u * twist;
                        float baseHeight = lerp(min(amp * 0.74, _Bank.z - 0.24), _Bank.z - 0.23, over);
                        y = baseHeight + rise * arc * (1.0 + twist * u * 0.64) - pow(u, 4.0) * (0.46 + hit);
                        z += _Motion.y * u * rise * 0.48;
                        live = alongFade * hit * over * saturate(burst * 1.73);
                    }
                    else
                    {
                        float roll = smoothstep(0.0, 0.065, age) * (1.0 - smoothstep(0.18, 0.38, age));
                        float lift = min(1.85, amp * 1.18) * roll * (0.57 + group * 0.51);
                        float travel = smoothstep(0.0, 0.31, age);
                        d = edge * (1.0 - 0.82 * travel) + (u - 0.37) * (0.8 + 2.2 * travel);
                        d += sin(u * 2.5) * lift * 0.78 + sin(2.7 * z + u * 2.3) * u * lift * 0.16;
                        y = -0.095 + lift * pow(saturate(sin(u * 3.14159)), 0.81)
                            + amp * 0.33 * (1.0 - travel);
                        live = alongFade * roll * (0.52 + group * 0.48) * smoothstep(0.12, 0.71, amp);
                    }
                }
                else if (mode > 2.5)
                {
                    float falling = smoothstep(0.09, 0.21, age) * (1.0 - smoothstep(0.31, 0.46, age));
                    d = _Shore.y - 0.055 + u * (0.19 + burst * falling * 0.54);
                    y = lerp(_Bank.z + 0.051, _Bank.w + 0.038, u * u);
                    live = alongFade * falling * over * (0.38 + burst * 0.62);
                }
                else
                {
                    float3 seed = input.part.yzw;
                    float start = lerp(0.008, 0.104, seed.x);
                    float lifetime = lerp(0.105, 0.265, seed.y);
                    float flight = saturate((age - start) / lifetime);
                    float fade = smoothstep(0.0, 0.14, flight) * (1.0 - smoothstep(0.51, 1.0, flight));
                    float energy = (0.32 + burst * 0.68) * (barrier ? over : smoothstep(0.15, 1.08, amp) * 0.56);
                    float arch = 4.0 * flight * (1.0 - flight);
                    d = edge + seed.y * 0.47 + flight * lerp(1.3, 6.2, seed.z) * energy;
                    bool mist = seed.z > 0.78;
                    float lift = min(amp * 2.19, 5.2) * lerp(0.36, 1.0, seed.x) * (mist ? 0.69 : 1.0);
                    y = (barrier ? _Bank.z - 0.17 : 0.16) + arch * energy * lift
                        - flight * flight * (barrier ? 0.75 : 0.19);
                    z += (_Motion.y * 0.85 + seed.x - 0.41) * flight * 5.4;
                    float radius = (mist ? lerp(0.087, 0.26, seed.y) : lerp(0.008, 0.059, seed.z))
                        * (barrier ? 1.0 : 0.68);
                    radius *= 0.66 + flight * (mist ? 0.82 : 0.41);
                    float2 corner = input.uv * 2.0 - 1.0;
                    float3 center = TransformObjectToWorld(float3(input.positionOS.x + d, _Shore.x + y, z));
                    float3 right = UNITY_MATRIX_I_V._m00_m10_m20;
                    float3 up = UNITY_MATRIX_I_V._m01_m11_m21;
                    output.positionWS = center + radius * (right * corner.x + up * corner.y * (1.15 + (1.0 - arch) * 1.28));
                    output.positionCS = TransformWorldToHClip(output.positionWS);
                    output.uv = input.uv;
                    output.flow = float4(d, z, mist ? 1.0 : 0.0, fade * energy * (mist ? 0.62 : 0.95));
                    output.part = mode;
                    return output;
                }

                output.positionWS = TransformObjectToWorld(float3(input.positionOS.x + d, _Shore.x + y, z));
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                output.flow = float4(d, z, age, live);
                output.part = mode;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float u = input.uv.x;
                float strength = input.flow.w;
                float phase = input.flow.z;
                float2 p = input.flow.xy;
                float3 white = float3(0.73, 0.80, 0.81);
                float3 color;
                float alpha;

                if (input.part > 2.5)
                {
                    float strand = WaterNoise(float2(p.y * 4.6, p.x * 0.42)) * 0.49
                        + WaterNoise(float2(p.y * 0.72, p.x * 0.88)) * 0.51;
                    float rivulet = ShoreFoam(float2(u * 0.69 + phase * 3.8, p.y));
                    float ends = smoothstep(0.0, 0.11, u) * (1.0 - smoothstep(0.89, 1.0, u));
                    color = lerp(float3(0.33, 0.45, 0.46), white, rivulet);
                    alpha = strength * ends * smoothstep(0.26, 0.76, strand) * (0.29 + rivulet * 0.76);
                }
                else if (input.part > 1.5)
                {
                    float2 c = input.uv * 2.0 - 1.0;
                    float breakup = phase < 0.5 ? 1.0 : lerp(0.18, 0.83, ShoreFoam(input.uv * 1.9 + p.y * 0.32));
                    float closeFade = smoothstep(2.0, 6.0, length(input.positionWS - GetCameraPositionWS()));
                    alpha = (1.0 - smoothstep(0.14, 0.98, dot(c, c))) * strength * breakup * closeFade;
                    color = white;
                }
                else if (input.part < 0.5)
                {
                    float2 flow = p * float2(0.91, 1.38) - float2(5.8 * phase, _Motion.y * 2.4 * phase);
                    float cluster = WaterNoise(flow * 0.40) * 0.73 + WaterNoise(flow * 1.2) * 0.27;
                    float frontAt = 0.78 + 0.18 * cluster + 0.037 * sin(p.y * 3.1);
                    float front = 1.0 - smoothstep(frontAt - 0.045, frontAt + 0.085, u);
                    float edge = smoothstep(frontAt - 0.095, frontAt - 0.035, u) * (1.0 - smoothstep(frontAt - 0.016, frontAt + 0.035, u));
                    float textureFoam = ShoreFoam(flow + float2(cluster * 0.47, 0.0));
                    float patches = smoothstep(0.20, 0.69, cluster);
                    float foam = saturate(textureFoam * patches * 0.96 + edge * (textureFoam * 0.85 + patches * 0.38));
                    foam *= lerp(1.0, 0.65 + edge * 0.27, _Bank.x) * _Motion.w;
                    float2 ripples = UnpackNormal(SAMPLE_TEXTURE2D(_RippleNormal, sampler_RippleNormal, flow * 0.44)).xy * 0.51;
                    float3 normal = TransformObjectToWorldNormal(normalize(float3(ripples.x, 1.0, ripples.y)));
                    float3 reflection = reflect(-GetWorldSpaceNormalizeViewDir(input.positionWS), normal);
                    color = lerp(WaterSky(reflection) * (0.56 + 0.20 * patches), white, saturate(foam * 1.45));
                    alpha = strength * front * (0.11 + 0.21 * patches + foam * 0.78);
                }
                else
                {
                    float2 streak = float2(p.y * 1.30, u * 7.6 - phase * 19.0);
                    float n = WaterNoise(streak) * 0.55 + WaterNoise(streak * 3.4 + 4.0) * 0.45;
                    float rim = smoothstep(0.015, 0.23, u) * (1.0 - smoothstep(0.64, 0.97, u));
                    float grains = ShoreFoam(streak * 0.34 + float2(2.2, -0.37));
                    float droplets = smoothstep(0.20 + 0.17 * u, 0.78, n * 0.73 + grains * 0.60);
                    float surfacing = smoothstep(0.29, 0.77, u + n * 0.19);
                    color = lerp(_ShallowWater.rgb * 0.94, white, saturate(surfacing * (0.78 + droplets * 0.27) + grains * 0.21));
                    float rippedEdge = smoothstep(0.22, 0.69, droplets + 0.25 * grains);
                    float sheet = lerp(0.47 + 0.32 * droplets, rippedEdge * 0.86,
                        _Bank.x * smoothstep(0.29, 0.71, u));
                    alpha = strength * rim * sheet * (0.54 + _Motion.w * 0.54);
                }
                return half4(WaterDistanceFog(color, input.positionWS), saturate(alpha));
            }
            ENDHLSL
        }
    }
}
