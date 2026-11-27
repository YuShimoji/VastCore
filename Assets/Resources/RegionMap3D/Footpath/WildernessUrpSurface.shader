Shader "Review/VastCore URP GLB"
{
    Properties
    {
        _BaseMap ("Base texture", 2D) = "white" {}
        _BaseColor ("Linear base factor", Vector) = (1,1,1,1)
        _NormalTex ("glTF normal", 2D) = "bump" {}
        _MR ("Metal and roughness", 2D) = "white" {}
        _NormalScale ("Normal scale", Float) = 0
        _Roughness ("Roughness", Float) = 1
        _Metallic ("Metallic", Float) = 0
        _Cutoff ("Alpha cutoff", Float) = -1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Cull [_Cull]
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_NormalTex); SAMPLER(sampler_NormalTex);
        TEXTURE2D(_MR); SAMPLER(sampler_MR);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST, _BaseColor;
            half _NormalScale, _Roughness, _Metallic, _Cutoff;
        CBUFFER_END
        struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            half4 color : COLOR;
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            half3 tangentWS : TEXCOORD2;
            half3 bitangentWS : TEXCOORD3;
            float2 uv : TEXCOORD4;
            half4 color : COLOR;
        };
        Varyings Vert(Attributes v)
        {
            Varyings o;
            VertexPositionInputs p = GetVertexPositionInputs(v.positionOS);
            VertexNormalInputs n = GetVertexNormalInputs(v.normalOS, v.tangentOS);
            o.positionCS = p.positionCS;
            o.positionWS = p.positionWS;
            o.normalWS = n.normalWS;
            o.tangentWS = n.tangentWS;
            o.bitangentWS = n.bitangentWS;
            o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
            o.color = v.color;
            return o;
        }
        half4 Base(Varyings i)
        {
            #if defined(LOD_FADE_CROSSFADE)
            LODFadeCrossFade(i.positionCS);
            #endif
            half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor * i.color;
            clip(color.a - _Cutoff);
            return color;
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            half4 Frag(Varyings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                half4 color = Base(i);
                half3 map = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, i.uv).xyz * 2 - 1;
                half3 normal = normalize(i.normalWS);
                if (_NormalScale > 0)
                    normal = normalize(normal * max(.001h, map.z) + _NormalScale * (i.tangentWS * map.x + i.bitangentWS * map.y));
                normal *= IS_FRONT_VFACE(face, 1, -1);
                half3 mr = SAMPLE_TEXTURE2D(_MR, sampler_MR, i.uv).rgb;
                InputData lighting = (InputData)0;
                lighting.positionWS = i.positionWS;
                lighting.normalWS = normal;
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                lighting.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                lighting.bakedGI = SampleSH(normal);
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                lighting.shadowMask = 1;
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = color.rgb;
                surface.alpha = 1;
                surface.normalTS = half3(0,0,1);
                surface.metallic = _Metallic * mr.b;
                surface.smoothness = 1 - _Roughness * mr.g;
                surface.occlusion = 1;
                return UniversalFragmentPBR(lighting, surface);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            float3 _LightDirection, _LightPosition;
            Varyings ShadowVert(Attributes v)
            {
                Varyings o = Vert(v);
                float3 direction = _LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                direction = normalize(_LightPosition - o.positionWS);
                #endif
                o.positionCS = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(o.positionWS, o.normalWS, direction)));
                return o;
            }
            half4 ShadowFrag(Varyings i) : SV_Target { Base(i); return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DepthFrag
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            half4 DepthFrag(Varyings i) : SV_Target { Base(i); return i.positionCS.z; }
            ENDHLSL
        }
    }
}
