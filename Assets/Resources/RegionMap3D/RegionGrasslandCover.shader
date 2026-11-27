Shader "Vastcore/RegionGrasslandCover"
{
    Properties
    {
        [HideInInspector] _GrasslandContinuous ("Continuous cover", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "RegionAtmosphere.hlsl"
            #include "RegionGrasslandHabitat.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _GrasslandContinuous;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 rootAndLayer : TEXCOORD0;
                half4 plant : COLOR; // growth, variation, terrain sun, dry stem
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half4 leaf : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                float3 root = input.rootAndLayer.xyz;
                float3 offset = input.positionOS.xyz - root;
                float eyeDistance = distance(TransformObjectToWorld(root), _WorldSpaceCameraPos);
                float layer = input.rootAndLayer.w;
                float2 band = layer > 1.5 ? float2(8.0, 16.0) : layer > 0.5 ? float2(18.0, 33.0) : float2(38.0, 86.0);
                float reach = lerp(0.74, 1.0, input.plant.g);
                if (_GrasslandContinuous > 0.5)
                {
                    band = layer > 2.5 ? float2(155.0, 301.0) : layer > 1.5 ? float2(13.0, 22.0)
                        : layer > 0.5 ? float2(23.0, 42.0) : float2(67.0, 124.0);
                    reach = lerp(0.84, 1.0, input.plant.g);
                }
                half fade = 1.0h - smoothstep(band.x * reach, band.y * reach, eyeDistance);
                // Close to the camera the fine leaves carry the canopy, while the wider silhouettes take over farther up the slope.
                if (layer < 0.5) fade *= lerp(0.53h, 1.0h, smoothstep(8.0, 24.0, eyeDistance));
                if (layer > 2.5) fade *= smoothstep(40.0, 94.0, eyeDistance);
                Varyings o;
                o.positionWS = TransformObjectToWorld(root + offset * fade);
                o.positionCS = TransformWorldToHClip(o.positionWS);

                half3 leaf = lerp(half3(0.44h, 0.43h, 0.31h), half3(0.30h, 0.39h, 0.23h), input.plant.r);
                leaf *= lerp(0.74h, 1.20h, input.plant.g);
                leaf = lerp(leaf, half3(0.57h, 0.51h, 0.34h), input.plant.a);
                // The leaves shade their bases. The clustered shadow is visible even at a low camera angle.
                leaf *= lerp(0.59h, 1.03h, smoothstep(0.0h, layer > 1.5 ? 0.12h : 0.22h, offset.y));
                half3 distant = lerp(half3(0.41h, 0.42h, 0.31h), half3(0.32h, 0.38h, 0.25h), input.plant.r);
                leaf = lerp(leaf, distant, smoothstep(25.0, 79.0, eyeDistance));
                if (_GrasslandContinuous > 0.5)
                {
                    half3 crown = GrasslandCrownColor(input.plant.r);
                    half3 dryStem = half3(0.57h, 0.50h, 0.32h);
                    bool basal = layer > 1.5 && layer < 2.5;
                    half shade = smoothstep(0.0h, basal ? 0.14h : 0.33h, offset.y);
                    half3 tips = lerp(crown, dryStem, input.plant.a) * lerp(0.76h, 1.19h, input.plant.g);
                    leaf = tips * lerp(0.48h, 1.08h, shade);
                    if (basal) leaf *= half3(0.91h, 0.85h, 0.78h);
                    leaf = lerp(leaf, crown * (0.91h + 0.14h * input.plant.g), smoothstep(54.0, 205.0, eyeDistance));
                }
                o.leaf = half4(RegionDisplayToLinear(leaf), input.plant.b);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half sunlit = min(sun.shadowAttenuation, i.leaf.a);
                half direct = 0.29h + 0.60h * saturate(sun.direction.y);
                if (_GrasslandContinuous > 0.5)
                {
                    // Light both sides of the curved ribbons, with the diffuse response of leaves rather than a flat tint.
                    float3 face = cross(ddx(i.positionWS), ddy(i.positionWS));
                    half facing = saturate(abs(dot(face, sun.direction)) * rsqrt(max(1e-12, dot(face, face))));
                    direct *= lerp(0.54h, 1.10h, facing);
                }
                half transmission = 0.22h * pow(saturate(dot(normalize(i.positionWS - _WorldSpaceCameraPos), sun.direction)), 3.0h);
                half3 ambient = SampleSH(half3(0, 1, 0)) + _RegionBounceColor.rgb * 0.22h;
                half3 color = i.leaf.rgb * (sun.color * sunlit * (direct + transmission) + ambient);
                return RegionOutput(RegionApplyAir(color, i.positionWS), 1.0h);
            }
            ENDHLSL
        }
    }
}
