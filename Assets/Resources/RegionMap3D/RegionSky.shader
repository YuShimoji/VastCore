// 地域の 3D 表示の空。天頂から地平線へのグラデーションと、太陽の周りの明るさを描く。
// 地平線の色は、地形に重ねる霞（RegionAtmosphere.hlsl）と同じ式で出すので、遠くの地形が空へ溶けて見える。
// 地平線より下は霞の色のまま（地形の外側の切れ目を隠す）。色は線形の値（RegionAtmosphere.cs が SetVector で渡すので、色空間で変換されない）。
Shader "Vastcore/RegionSky"
{
    Properties
    {
        _ZenithColor ("Zenith (linear)", Color) = (0.08, 0.20, 0.52, 1)
        _SunGlowColor ("Sun glow (linear)", Color) = (1.0, 0.72, 0.42, 1)
        _HorizonSharpness ("Horizon band sharpness", Range(1, 12)) = 4.5
        _SunGlowSize ("Sun glow size", Range(1, 512)) = 24
        _SunGlowStrength ("Sun glow strength", Range(0, 8)) = 0.8
        _SunDiscStrength ("Sun disc strength", Range(0, 80)) = 30
        [HideInInspector] _RegionCloudMap ("Region cloud density", 2D) = "black" {}
        [HideInInspector] _RegionCloudTransform ("Region cloud map transform", Vector) = (0, 0, 0, 0)
        [HideInInspector] _RegionCloudView ("Region cloud heights and distance", Vector) = (0, 0, 0, 0)
        [HideInInspector] _RegionCloudNoiseOffset ("Cloud noise origin", Vector) = (0, 0, 0, 0)
        [HideInInspector] _RegionCloudDrift ("Cloud layer drift", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "RegionAtmosphere.hlsl"

            TEXTURE2D(_RegionCloudMap);
            SAMPLER(sampler_RegionCloudMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _SunGlowColor;
                half _HorizonSharpness;
                half _SunGlowSize;
                half _SunGlowStrength;
                half _SunDiscStrength;
                float4 _RegionCloudTransform; // 最小の X、Z、範囲の一辺の逆数、有効フラグ
                float4 _RegionCloudView;      // 低い雲の底・頂、高い雲の高さ、水平の描画距離（m）
                float4 _RegionCloudNoiseOffset;
                float4 _RegionCloudDrift;
            CBUFFER_END

            float RegionCloudDensity(float2 position, float mipLevel, bool high)
            {
                float2 uv = (position - _RegionCloudTransform.xy) * _RegionCloudTransform.z;
                float2 edge = min(uv, 1.0 - uv);
                float edgeFade = smoothstep(0.0, 0.014, min(edge.x, edge.y));
                // 上流の雲の地図は R が低い雲、G が高い雲。テクスチャの V は北へ増える。
                float2 drift = high ? _RegionCloudDrift.zw : _RegionCloudDrift.xy;
                float2 density = SAMPLE_TEXTURE2D_LOD(_RegionCloudMap, sampler_RegionCloudMap, frac(uv - drift), mipLevel).rg;
                return (high ? density.g : density.r) * edgeFade;
            }

            float RegionCloudNoise(float3 position)
            {
                float3 cell = floor(position);
                float3 f = frac(position);
                f = f * f * (3.0 - 2.0 * f);
                float4 corners = frac((dot(cell, float3(0.1031, 0.11369, 0.13787)) +
                    float4(0.0, 0.1031, 0.11369, 0.21679)) * 5.3983);
                float4 nextCorners = frac(corners + 0.13787 * 5.3983);
                corners = frac(corners * (corners + 33.33) * (corners + 0.17));
                nextCorners = frac(nextCorners * (nextCorners + 33.33) * (nextCorners + 0.17));
                float4 values = lerp(corners, nextCorners, f.z);
                float2 rows = lerp(values.xz, values.yw, f.x);
                return lerp(rows.x, rows.y, f.y);
            }

            float RegionCloudVisibility(float distanceXZ)
            {
                float reach = _RegionCloudView.w;
                return 1.0 - smoothstep(0.48 * reach, reach, distanceXZ);
            }

            float4 RegionLowCloud(float3 eye, float3 view, float3 sun, float3 horizon, float2 pixel)
            {
                float bottom = _RegionCloudView.x;
                float top = _RegionCloudView.y;
                float thickness = max(top - bottom, 1.0);
                float horizontalSpeed = length(view.xz);
                float nearDistance = 0.0;
                float farDistance = _RegionCloudView.w / max(horizontalSpeed, 0.0001);
                if (abs(view.y) > 0.0001)
                {
                    float bottomDistance = (bottom - eye.y) / view.y;
                    float topDistance = (top - eye.y) / view.y;
                    nearDistance = max(0.0, min(bottomDistance, topDistance));
                    farDistance = min(farDistance, max(bottomDistance, topDistance));
                }
                else if (eye.y < bottom || eye.y > top)
                {
                    return 0.0;
                }
                if (farDistance <= nearDistance) return 0.0;

                const int steps = 12;
                float stepLength = (farDistance - nearDistance) / steps;
                float mipLevel = min(3.0, log2(max(1.0, stepLength / 480.0)));
                float dither = frac(52.9829189 * frac(dot(floor(pixel), float2(0.06711056, 0.00583715))));
                float offset = lerp(0.5, lerp(0.16, 0.84, dither), smoothstep(350.0, 1700.0, stepLength));
                float3 lightColor = lerp(float3(0.92, 0.96, 1.0), _SunGlowColor.rgb, 0.46);
                float3 shadeColor = lerp(float3(0.28, 0.34, 0.45), horizon, 0.25);
                float forward = pow(saturate(dot(view, sun)), 8.0);
                float3 color = 0.0;
                float transmission = 1.0;

                [loop]
                for (int stepIndex = 0; stepIndex < steps; stepIndex++)
                {
                    float distanceAlongRay = nearDistance + (stepIndex + offset) * stepLength;
                    float3 p = eye + view * distanceAlongRay;
                    float footprint = RegionCloudDensity(p.xz, mipLevel, false);
                    if (footprint <= 0.01) continue;

                    // 地図の濃さで雲の頂の高さを変え、細部はその濃さと縁の中だけに入れる。
                    float height = saturate((p.y - bottom) / thickness);
                    float localTop = lerp(0.45, 1.0, smoothstep(0.04, 0.72, footprint));
                    float nearDetail = 1.0 - smoothstep(9000.0, 26000.0, distanceAlongRay);
                    float detail = 0.5;
                    if (nearDetail > 0.0)
                        detail = RegionCloudNoise(float3(p.x + _RegionCloudNoiseOffset.x, p.y, p.z + _RegionCloudNoiseOffset.y) / 820.0);
                    localTop -= (1.0 - detail) * nearDetail * 0.18;
                    float envelope = smoothstep(0.0, 0.19, height) * (1.0 - smoothstep(localTop - 0.24, localTop, height));
                    float density = footprint * envelope * lerp(0.5, 1.0, smoothstep(0.01, 0.23, footprint));
                    density *= lerp(1.0, lerp(0.66, 1.18, detail), nearDetail);
                    if (density <= 0.0) continue;

                    float distanceXZ = distanceAlongRay * horizontalSpeed;
                    float visibility = RegionCloudVisibility(distanceXZ);
                    float opacity = (1.0 - exp(-density * stepLength * 0.0045)) * visibility;
                    float sunFootprint = RegionCloudDensity(p.xz + sun.xz * 620.0, mipLevel, false);
                    float lit = saturate(0.48 + height * 0.48 + (footprint - sunFootprint) * 1.10 - density * 0.22);
                    float3 sampleColor = lerp(shadeColor, lightColor, lit) + lightColor * forward * (0.22 + 0.30 * lit);
                    float air = max(RegionTransmittance(p), 0.45 * visibility);
                    color += transmission * opacity * lerp(horizon, sampleColor, air);
                    transmission *= 1.0 - opacity;
                    if (transmission < 0.015) break;
                }

                return float4(color, 1.0 - transmission);
            }

            float4 RegionHighCloud(float3 eye, float3 view, float3 horizon)
            {
                if (abs(view.y) <= 0.0001) return 0.0;
                float distanceAlongRay = (_RegionCloudView.z - eye.y) / view.y;
                if (distanceAlongRay <= 0.0) return 0.0;

                float3 p = eye + view * distanceAlongRay;
                float visibility = RegionCloudVisibility(length(p.xz - eye.xz));
                if (visibility <= 0.0) return 0.0;

                float density = RegionCloudDensity(p.xz, 0.0, true);
                float opacity = (1.0 - exp(-1.9 * density / max(abs(view.y), 0.28))) * visibility;
                float3 color = lerp(horizon, float3(0.92, 0.95, 1.0), max(RegionTransmittance(p), 0.45 * visibility));
                return float4(color * opacity, opacity);
            }

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 view = normalize(input.direction);
                float3 sun = normalize(_MainLightPosition.xyz);
                half3 horizon = RegionFogColor(view, sun);
                // 地平線付近は厚い空気を通るので白っぽく、上ほど濃い青
                half up = saturate(view.y);
                half band = pow(1.0 - up, _HorizonSharpness);
                half3 sky = lerp(_ZenithColor.rgb, horizon, band);
                // 太陽の周りの明るさと、太陽の円盤
                half facing = saturate(dot(view, sun));
                sky += _SunGlowColor.rgb * pow(facing, _SunGlowSize) * _SunGlowStrength;
                sky += _SunGlowColor.rgb * smoothstep(0.99955, 0.99975, facing) * _SunDiscStrength;
                // 地平線より下は霞の色
                sky = lerp(sky, horizon, smoothstep(0.0, -0.03, view.y));
                [branch]
                if (_RegionCloudTransform.w > 0.5 && _RegionCloudTransform.z > 0.0 && _RegionCloudView.w > 0.0)
                {
                    float3 eye = _WorldSpaceCameraPos;
                    float4 highCloud = RegionHighCloud(eye, view, horizon);
                    float4 lowCloud = RegionLowCloud(eye, view, sun, horizon, input.positionCS.xy);
                    if (view.y >= 0.0)
                    {
                        sky = highCloud.rgb + sky * (1.0 - highCloud.a);
                        sky = lowCloud.rgb + sky * (1.0 - lowCloud.a);
                    }
                    else
                    {
                        sky = lowCloud.rgb + sky * (1.0 - lowCloud.a);
                        sky = highCloud.rgb + sky * (1.0 - highCloud.a);
                    }
                }
                return RegionOutput(sky, 1);
            }
            ENDHLSL
        }
    }
}
