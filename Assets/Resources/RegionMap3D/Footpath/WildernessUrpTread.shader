Shader "Review/VastCore URP Footpath"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes { float3 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half alpha : TEXCOORD2;
            };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.alpha = v.color.a;
                return o;
            }
            float Grain(float2 p) { return frac(sin(dot(p,float2(43.53,91.17)))*26325.784); }
            float SmoothGrain(float2 p)
            {
                float2 c=floor(p), f=frac(p);
                f=f*f*(3-2*f);
                return lerp(lerp(Grain(c),Grain(c+float2(1,0)),f.x),
                            lerp(Grain(c+float2(0,1)),Grain(c+1),f.x),f.y);
            }
            half4 Frag(Varyings i) : SV_Target
            {
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half3 normal=normalize(i.normalWS);
                half3 albedo=lerp(half3(.29,.167,.111),half3(.34,.214,.153),SmoothGrain(i.positionWS.xz*31));
                half3 color=albedo*(SampleSH(normal)+sun.color*saturate(dot(normal,sun.direction))*sun.shadowAttenuation);
                return half4(color,i.alpha*(.43+SmoothGrain(i.positionWS.xz*5)*.28));
            }
            ENDHLSL
        }
    }
}
