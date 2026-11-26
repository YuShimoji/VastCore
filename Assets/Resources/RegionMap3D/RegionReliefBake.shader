// 表面の凹凸を、場面を作る時に GPU で一度だけ画像へ焼く（RegionSurfaceRelief.cs が呼ぶ）。
// パス 0: 地層の柱の高さ（m）。横 1 列で、x が「帽子の岩の下端からの高さ」。色の縞と同じ式（RegionSandstone.hlsl）から作る。
// パス 1: 岩肌の細かな起伏の高さ（m）。一辺 _BakeParams.x m で繰り返す。写真の素材を選ぶまでの仮。
// パス 2: 高さの画像から傾き（高さの変化 m/m）を Sobel で出す。どの高さの画像にも使える（テクスチャ → 凹凸）。
Shader "Hidden/Vastcore/RegionReliefBake"
{
    SubShader
    {
        ZTest Always ZWrite Off Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "RegionSandstone.hlsl"

        float4 _BakeParams;     // パス 0: x 最低の高さ m、y 高さの幅 m。パス 1: x 繰り返しの一辺 m
        float4 _BakeTexel;      // パス 2: x,y 1 画素の uv、z,w 1 画素の m
        TEXTURE2D(_BakeSource);
        SAMPLER(sampler_BakeSource);

        struct BakeVaryings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        BakeVaryings BakeVert(float4 positionOS : POSITION, float2 uv : TEXCOORD0)
        {
            BakeVaryings output;
            output.positionCS = TransformObjectToHClip(positionOS.xyz);
            output.uv = uv;
            return output;
        }

        // 周期 period（格子の数）で繰り返す値のノイズ
        float PeriodicNoise(float2 uv, float period, float salt)
        {
            float2 cell = floor(uv);
            float2 f = frac(uv);
            f = f * f * (3.0 - 2.0 * f);
            float a = SandHash(fmod(cell, period) + salt);
            float b = SandHash(fmod(cell + float2(1, 0), period) + salt);
            float c = SandHash(fmod(cell + float2(0, 1), period) + salt);
            float d = SandHash(fmod(cell + 1.0, period) + salt);
            return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
        }
        ENDHLSL

        Pass
        {
            Name "StrataHeight"
            HLSLPROGRAM
            #pragma vertex BakeVert
            #pragma fragment Frag
            float4 Frag(BakeVaryings input) : SV_Target
            {
                float bedHeight = _BakeParams.x + input.uv.x * _BakeParams.y;
                return float4(SandstoneRelief(bedHeight), 0, 0, 0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "RockHeight"
            HLSLPROGRAM
            #pragma vertex BakeVert
            #pragma fragment Frag
            float4 Frag(BakeVaryings input) : SV_Target
            {
                // 大きい順に 1 m・0.5 m・25 cm・12.5 cm・6 cm の起伏。
                // 値のノイズの等高線で作る割れ目は、閉じた丸い線の網（敷石の目地のよう）に見えたので入れない（2026-11-26）
                float tile = _BakeParams.x;
                float h = 0.0;
                float amplitude = 0.035;
                float cells = tile;
                for (int i = 0; i < 5; i++)
                {
                    h += amplitude * (PeriodicNoise(input.uv * cells, cells, 11.0 * i) - 0.5);
                    amplitude *= 0.5;
                    cells *= 2.0;
                }
                return float4(h, 0, 0, 0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Slope"
            HLSLPROGRAM
            #pragma vertex BakeVert
            #pragma fragment Frag
            float H(float2 uv, float dx, float dy)
            {
                return SAMPLE_TEXTURE2D_LOD(_BakeSource, sampler_BakeSource, uv + float2(dx, dy) * _BakeTexel.xy, 0).r;
            }
            float4 Frag(BakeVaryings input) : SV_Target
            {
                float2 uv = input.uv;
                float gx = (H(uv, 1, -1) + 2.0 * H(uv, 1, 0) + H(uv, 1, 1)) - (H(uv, -1, -1) + 2.0 * H(uv, -1, 0) + H(uv, -1, 1));
                float gy = (H(uv, -1, 1) + 2.0 * H(uv, 0, 1) + H(uv, 1, 1)) - (H(uv, -1, -1) + 2.0 * H(uv, 0, -1) + H(uv, 1, -1));
                return float4(gx / (8.0 * _BakeTexel.z), gy / (8.0 * _BakeTexel.w), 0, 0);
            }
            ENDHLSL
        }
    }
}
