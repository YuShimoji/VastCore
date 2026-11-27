Shader "Vastcore/RegionPlants"
{
    Properties
    {
        [MainTexture] _BaseMap ("Plant atlas", 2D) = "white" {}
        [HideInInspector] _BaseColor ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Cutoff", Range(0,1)) = 0.34
        [HideInInspector] _MeadowTurf ("Meadow turf", Float) = 0
        [HideInInspector] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            AlphaToMask On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "RegionAtmosphere.hlsl"
            #include "RegionShadows.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
                half _MeadowTurf;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 light : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; half2 light : TEXCOORD2; };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.uv = input.uv;
                o.light = input.light.rg;
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                half4 leaf = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                clip(leaf.a - _Cutoff);
                half3 albedo = RegionToLinear(leaf.rgb * _BaseColor.rgb);
                if (_MeadowTurf > 0.5h)
                {
                    half luma = dot(albedo, half3(0.21h, 0.72h, 0.07h));
                    half3 litBlades = RegionDisplayToLinear(half3(0.43h, 0.51h, 0.29h)) * (0.83h + 1.8h * luma);
                    albedo = lerp(albedo * 1.15h, litBlades, 0.44h);
                }
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                AmbientOcclusionFactor occlusion = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(i.positionCS));
                half terrainSun = RegionShadowsEnabled() ? RegionSunVisibility(i.positionWS, float3(0, 1, 0), 0.0) : i.light.x;
                half sunlit = min(sun.shadowAttenuation, terrainSun);
                half direct = sunlit * saturate(0.30h + 0.55h * sun.direction.y);
                // 低い太陽を背にした葉は、光が透けて明るく見える
                float3 view = normalize(i.positionWS - _WorldSpaceCameraPos);
                half through = pow(saturate(dot(view, sun.direction)), 4.0) * 0.45h * sunlit;
                half3 tint = dot(_RegionShadowTint.rgb, 1.0) > 0.0 ? lerp(_RegionShadowTint.rgb, 1.0, sunlit) : 1.0;
                half3 sky = SampleSH(half3(0,1,0)) * (0.4h + 0.6h * i.light.y) * tint + 0.5h * _RegionBounceColor.rgb;
                half3 color = albedo * (sun.color * (direct + through) + sky * occlusion.indirectAmbientOcclusion);
                return RegionOutput(RegionApplyAir(color, i.positionWS), leaf.a);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
