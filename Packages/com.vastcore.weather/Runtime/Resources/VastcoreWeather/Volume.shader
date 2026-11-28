Shader "Vastcore/Weather/Volume"
{
    Properties
    {
        _Shape ("Shape", Float) = 0
        _Extinction ("Extinction per metre", Float) = 0.002
        _VolumeSteps ("Steps", Float) = 32
        _SunlitColor ("Sunlit", Color) = (1,1,1,1)
        _ShadeColor ("Shade", Color) = (0.45,0.55,0.65,1)
        _AerialHaze ("Aerial Haze", Color) = (0.65,0.72,0.77,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-50" "RenderType"="Transparent" "DisableBatching"="True" }
        ZWrite Off
        ZTest LEqual
        Cull Front
        Blend One OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _SunlitColor;
                float4 _ShadeColor;
                float4 _AerialHaze;
                float4 _VolumeDetail;
                float3 _WeatherSun;
                float _Shape;
                float _Extinction;
                float _VolumeSteps;
                float _HazeExtinction;
                float _Flash;
                float _WeatherSeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            struct Fragment
            {
                half4 color : SV_Target;
                float depth : SV_Depth;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            float WeatherNoise(float3 position)
            {
                float3 cell = floor(position);
                float3 blend = frac(position);
                blend = blend * blend * (3.0 - 2.0 * blend);
                float3 increments = float3(0.1031, 0.11369, 0.13787);
                float4 corners = (dot(cell, increments) + float4(0.0, increments.x, increments.y, increments.x + increments.y)) * 5.3983;
                float4 upper = frac(corners + increments.z * 5.3983);
                corners = frac(corners);
                corners = frac(corners * (corners + 33.33) * (corners + 0.17));
                upper = frac(upper * (upper + 33.33) * (upper + 0.17));
                float4 values = lerp(corners, upper, blend.z);
                float2 rows = lerp(values.xz, values.yw, blend.x);
                return lerp(rows.x, rows.y, blend.y);
            }

            float Ellipsoid(float3 p, float3 center, float3 radius)
            {
                return length((p - center) / radius) - 1.0;
            }

            float Tower(float3 p)
            {
                float shape = Ellipsoid(p, float3(-0.12, -0.43, 0), float3(0.76, 0.46, 0.75));
                shape = min(shape, Ellipsoid(p, float3(-0.03, -0.06, 0.02), float3(0.67, 0.64, 0.69)));
                shape = min(shape, Ellipsoid(p, float3(0.15, 0.48, -0.08), float3(0.52, 0.44, 0.55)));
                shape = min(shape, Ellipsoid(p, float3(-0.48, -0.14, 0.15), float3(0.40, 0.39, 0.46)));
                shape = min(shape, Ellipsoid(p, float3(-0.27, 0.37, 0.13), float3(0.46, 0.39, 0.45)));
                shape = min(shape, Ellipsoid(p, float3(0.45, -0.25, -0.14), float3(0.42, 0.41, 0.49)));
                return max(shape - 0.07, (-p.y - 0.81) * 3.7);
            }

            float Density(float3 p, int shape)
            {
                float3 noiseSpace = p * _VolumeDetail.xyz * 0.5 + _WeatherSeed * float3(0.071, 0.113, 0.043);
                float flow = _VolumeDetail.w;
                float envelope;

                if (shape == 7)
                {
                    float bend = 0.16 * sin(p.z * 4.2 + 0.7) + 0.085 * sin(p.z * 9.0 - 1.2);
                    float edge = 1.0 - smoothstep(0.39, 0.94, abs(p.x - bend));
                    edge *= 1.0 - smoothstep(0.82, 1.0, max(abs(p.x), abs(p.z)));
                    float2 air = noiseSpace.xz + float2(flow * 0.41, -flow * 0.21);
                    float patch = WeatherNoise(float3(air.x * 0.34, 9.2, air.y * 0.43));
                    float wisp = WeatherNoise(float3(air.x * 0.79, 2.3 + p.y * 0.65, air.y * 0.87));
                    float height = p.y - (-0.24 + 0.32 * p.z + (patch - 0.5) * 0.12);
                    float lowLayer = exp(-max(0.0, height) * 4.8) * (0.30 + 0.36 * patch + 0.19 * wisp);
                    float crest = 0.39 + 0.25 * (patch - 0.5) + 0.19 * (wisp - 0.5);
                    float upperWisps = smoothstep(0.12, 0.28, height) *
                        (1.0 - smoothstep(crest, crest + 0.23, height));
                    upperWisps *= smoothstep(0.19, 0.80, patch * 0.62 + wisp * 0.38) * 0.37;
                    float floor = smoothstep(-1.0, -0.83, p.y);
                    return edge * floor * (lowLayer + upperWisps);
                }

                if (shape == 4)
                {
                    float shore = 1.0 - smoothstep(0.34, 0.99, length(p.xz * float2(1.0, 1.10)));
                    float drift = WeatherNoise(float3(noiseSpace.x + flow * 0.35, 3.7, noiseSpace.z)) - 0.5;
                    float basin = WeatherNoise(float3(noiseSpace.x * 0.36, 1.8, noiseSpace.z * 0.43));
                    float lid = 1.0 - smoothstep(-0.26, 0.71, p.y - drift * 0.82);
                    return shore * lid * smoothstep(-1.0, -0.85, p.y) * (0.40 + basin * 0.71 + drift * 0.42);
                }

                if (shape == 5)
                {
                    float horizontal = 1.0 - smoothstep(0.35, 0.91, length(p.xz + float2((p.y + 1.0) * 0.10, p.y * 0.04)));
                    float threads = WeatherNoise(float3(noiseSpace.x * 1.9 + p.y * 0.65,
                        noiseSpace.y * 0.06 - flow, noiseSpace.z * 0.93));
                    float pockets = WeatherNoise(float3(noiseSpace.x * 0.30, 9.1, noiseSpace.z * 0.48));
                    return horizontal * smoothstep(-1.0, -0.92, p.y) *
                        (1.0 - smoothstep(0.39, 0.99, p.y - (pockets - 0.5) * 0.36)) *
                        (0.16 + 0.53 * threads + 0.47 * pockets);
                }

                if (shape == 2)
                {
                    float along = p.x;
                    p.y += 0.07 * sin(30.0 * along) + 0.05 * sin(9.0 * along);
                    float theta = atan2(p.z, p.y);
                    float crossSection = 0.81 + 0.075 * cos(theta * 2.0 + along * 42.0 - flow);
                    envelope = length(p.zy / float2(crossSection, 0.83)) - 1.0;
                    envelope = max(envelope, (abs(along) - 0.97) * 9.0);
                    noiseSpace.x *= 0.32;
                    noiseSpace.y += 0.69 * sin(theta * 2.0 + along * 58.0 - flow);
                    noiseSpace.z += 0.55 * cos(theta + along * 31.0 - flow);
                }
                else if (shape == 3)
                {
                    // Bent axis with a layered, wider base and translating helical detail.
                    float3 original = p;
                    float y = saturate(p.y * 0.5 + 0.5);
                    p.x -= (y - 0.43) * 0.89;
                    p.z -= y * 0.14;
                    float angle = atan2(p.z, p.x);
                    float radius = length(p.xz * float2(1.0, 1.15));
                    float lobes = 0.62 + 0.14 * cos(y * 5.2) + 0.10 * sin(y * 36.0 - angle * 2.0 - flow);
                    envelope = max(radius / lobes - 1.0, max((-p.y - 0.78) * 3.5, (p.y - 0.81) * 3.1));
                    envelope = min(envelope, Ellipsoid(original, float3(-0.43, -0.76, 0.05), float3(0.38, 0.18, 0.42)));
                    float sine, cosine;
                    sincos(flow + y * 1.17, sine, cosine);
                    noiseSpace.xz = mul(float2x2(cosine, -sine, sine, cosine), noiseSpace.xz - _WeatherSeed * float2(0.071, 0.043));
                    noiseSpace.xz += _WeatherSeed * float2(0.071, 0.043);
                    noiseSpace.y += 0.36 * sin(angle * 2.0 - y * 34.0 - flow);
                }
                else if (shape == 1)
                {
                    envelope = min(Ellipsoid(p, float3(-0.11, -0.08, 0.0), float3(0.65, 0.62, 0.80)),
                        Ellipsoid(p, float3(0.28, 0.04, -0.05), float3(0.68, 0.33, 0.64)));
                    noiseSpace.x *= 0.42;
                    noiseSpace.y *= 1.65;
                    noiseSpace.x -= flow;
                }
                else if (shape == 6)
                {
                    p.y -= 0.18 * p.x + 0.08 * sin(p.x * 3.3);
                    envelope = Ellipsoid(p, float3(0, 0, 0), float3(0.95, 0.70, 0.82));
                    noiseSpace.x *= 0.55;
                    noiseSpace.y *= 1.75;
                    noiseSpace.x -= flow;
                }
                else
                {
                    envelope = Tower(p);
                    noiseSpace.y -= flow;
                }

                if (envelope > 0.23)
                    return 0.0;
                float broad = WeatherNoise(noiseSpace);
                float detail = WeatherNoise(noiseSpace * 2.43 + 7.2);
                float erosion = (broad - 0.52) * (shape == 2 ? 0.53 : 0.32) + (detail - 0.55) * 0.14;
                float density = saturate((erosion - envelope) * 2.65);
                float boxFade = saturate((1.0 - max(abs(p.x), max(abs(p.y), abs(p.z)))) * 14.0);
                return density * boxFade * (0.58 + 0.57 * broad);
            }

            float2 IntersectBounds(float3 eye, float3 direction)
            {
                float3 safeDirection = lerp(-1.0, 1.0, step(0.0, direction)) * max(abs(direction), 0.00000001);
                float3 a = (-0.5 - eye) / safeDirection;
                float3 b = (0.5 - eye) / safeDirection;
                float3 nearPlane = min(a, b);
                float3 farPlane = max(a, b);
                return float2(max(0.0, max(nearPlane.x, max(nearPlane.y, nearPlane.z))),
                    min(farPlane.x, min(farPlane.y, farPlane.z)));
            }

            Fragment Frag(Varyings input)
            {
                float2 uv = input.positionCS.xy / _ScaledScreenParams.xy;
                float3 eye = GetCameraPositionWS();
                float3 direction = normalize(input.positionWS - eye);
                bool orthographic = unity_OrthoParams.w > 0.5;
                float3 forwardWS = GetViewForwardDir();
                if (orthographic)
                {
                    #if UNITY_REVERSED_Z
                        float nearDepth = 1.0;
                    #else
                        float nearDepth = UNITY_NEAR_CLIP_VALUE;
                    #endif
                    eye = ComputeWorldSpacePosition(uv, nearDepth, UNITY_MATRIX_I_VP);
                    direction = forwardWS;
                }

                float3 eyeOS = TransformWorldToObject(eye);
                float3 rayOS = mul((float3x3)GetWorldToObjectMatrix(), direction);
                float2 limits = IntersectBounds(eyeOS, rayOS);
                float eyeDirection = max(dot(direction, forwardWS), 0.0001);
                if (!orthographic)
                    limits.x = max(limits.x, _ProjectionParams.y / eyeDirection);
                clip(limits.y - limits.x - 0.00001);

                float rawDepth = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    bool sceneHit = rawDepth > 0.0000001;
                #else
                    bool sceneHit = rawDepth < 0.9999999;
                #endif
                if (sceneHit)
                {
                    float surfaceDistance = LinearEyeDepth(rawDepth, _ZBufferParams) / eyeDirection;
                    if (orthographic)
                    {
                        #if !UNITY_REVERSED_Z
                            rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
                        #endif
                        float3 surface = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                        surfaceDistance = dot(surface - eye, direction);
                    }
                    limits.y = min(limits.y, surfaceDistance);
                }
                clip(limits.y - limits.x - 0.00001);

                // Rasterize the back of the box so the volume also renders from inside.
                // Test its entry depth against opaque geometry, rather than its back face.
                float4 entry = TransformWorldToHClip(eye + direction * limits.x);
                float entryDepth = entry.z / entry.w;
                #if !UNITY_REVERSED_Z
                    entryDepth = (entryDepth - UNITY_NEAR_CLIP_VALUE) / (1.0 - UNITY_NEAR_CLIP_VALUE);
                #endif

                int shape = (int)_Shape;
                int steps = (int)_VolumeSteps;
                // A rain curtain varies much less vertically than a convective cloud.
                if (shape == 5)
                    steps = min(steps, (limits.y - limits.x) < 320.0 ? 8 : 16);
                float stride = (limits.y - limits.x) / steps;
                float dither = frac(52.9829189 * frac(dot(floor(input.positionCS.xy), float2(0.06711056, 0.00583715))));
                float along = limits.x + stride * lerp(0.30, 0.70, dither);
                float3 sunOS = normalize(mul((float3x3)GetWorldToObjectMatrix(), _WeatherSun));
                float forward = pow(saturate(dot(direction, _WeatherSun)), 6.0);
                float transmittance = 1.0;
                float3 radiance = 0.0;
                [loop]
                for (int index = 0; index < steps; ++index, along += stride)
                {
                    float3 p = 2.0 * (eyeOS + rayOS * along);
                    float density = Density(p, shape);
                    if (density <= 0.002)
                        continue;
                    float3 color = _ShadeColor.rgb;
                    if (shape == 7)
                    {
                        float height = saturate(p.y * 0.5 + 0.5);
                        float light = saturate(0.18 + height * 0.65 + forward * 0.23);
                        color = lerp(_ShadeColor.rgb, _SunlitColor.rgb, light);
                    }
                    else if (shape != 5)
                    {
                        float exposed = density - Density(p + sunOS * 0.15, shape);
                        float height = saturate(p.y * 0.5 + 0.5);
                        float light = saturate(0.28 + height * 0.43 + exposed * 1.25 - density * 0.12);
                        color = lerp(_ShadeColor.rgb, _SunlitColor.rgb, light);
                        color += _SunlitColor.rgb * (forward * 0.14 + _Flash * 0.25);
                    }
                    float air = exp(-along * _HazeExtinction);
                    color = lerp(_AerialHaze.rgb, color, air);
                    float opacity = 1.0 - exp(-density * _Extinction * stride);
                    radiance += transmittance * opacity * color;
                    transmittance *= 1.0 - opacity;
                    if (transmittance < 0.015)
                        break;
                }
                Fragment output;
                output.color = half4(radiance, 1.0 - transmittance);
                output.depth = saturate(entryDepth);
                return output;
            }
            ENDHLSL
        }
    }
}
