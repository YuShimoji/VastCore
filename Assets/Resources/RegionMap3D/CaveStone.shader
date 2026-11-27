Shader "Vastcore/Cave Stone"
{
    Properties
    {
        _EntranceZ ("Entrance plane", Float) = -26.0
        _FreshCollapse ("Fresh collapse", Float) = 0
        _ReturnBounce ("Return wall bounce", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _EntranceZ;
                float _FreshCollapse;
                float4 _ReturnBounce;
            CBUFFER_END

            struct Varyings { float4 clip : SV_POSITION; float3 pos : TEXCOORD0; half3 normal : TEXCOORD1; half fresh : TEXCOORD2; };
            Varyings Vert(float3 position : POSITION, float3 normal : NORMAL, half4 color : COLOR)
            {
                Varyings o;
                o.clip = TransformObjectToHClip(position);
                o.pos = TransformObjectToWorld(position);
                o.normal = TransformObjectToWorldNormal(normal);
                o.fresh = color.r * _FreshCollapse;
                return o;
            }

            float Hash(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float Noise(float3 p)
            {
                float3 cell = floor(p), t = frac(p);
                t = t * t * (3 - 2 * t);
                float4 a = float4(Hash(cell), Hash(cell + float3(0,1,0)), Hash(cell + float3(0,0,1)), Hash(cell + float3(0,1,1)));
                float4 b = float4(Hash(cell+float3(1,0,0)), Hash(cell+float3(1,1,0)), Hash(cell+float3(1,0,1)), Hash(cell+1));
                float4 h = lerp(a, b, t.x);
                return lerp(lerp(h.x,h.y,t.y), lerp(h.z,h.w,t.y), t.z);
            }

            float Diffuse(float3 p, float3 n, float3 source, float range)
            {
                float3 delta = source - p;
                return (0.15 + 0.85 * saturate(dot(n, normalize(delta)))) / (1 + dot(delta,delta)/(range*range));
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 p = i.pos, n = normalize(i.normal);
                float fault = Noise(p * 0.39) + 0.33 * Noise(p * 1.55);
                float stratum = p.y * 1.7 + p.z * 0.17 + p.x * 0.12 + 1.45 * fault;
                float seam = smoothstep(0.68, 0.95, sin(stratum * 5.4));
                float grit = Noise(p * 11) - 0.5;
                float bedding = 0.58 * Noise(p * float3(1.4,4.2,1.3)) + 0.3 * fault + 0.12 * grit;
                float relief = bedding * 0.16 - 0.032 * seam;
                float3 across = cross(ddy(p), n), along = cross(n, ddx(p));
                float det = dot(ddx(p), across);
                n = normalize(abs(det) * n - sign(det) * (ddx(relief) * across + ddy(relief) * along));

                float mineral = smoothstep(0.56, 0.72, fault);
                float3 stone = lerp(float3(0.24,0.20,0.16), float3(0.38,0.37,0.335), mineral);
                stone *= 0.61 + bedding * 0.72 + grit * 0.18 - seam * 0.12;
                stone = lerp(stone, float3(0.42,0.39,0.34) * (0.77 + 0.32 * bedding + 0.1 * grit), 0.72 * i.fresh);
                // Light spreads from the entrance and the roof fissure. Wall bounce is muted.
                float3 light = float3(0.082,0.096,0.12);
                light += float3(0.67,1.09,1.49) * Diffuse(p,n,float3(-3,11,7),9.5);
                light += float3(1.16,0.86,0.63) * Diffuse(p,n,float3(9,4.6,10),6.7);
                light += float3(0.76,0.66,0.56) * Diffuse(p,n,float3(-10,6,-3),5.5);
                light += float3(0.65,0.79,1.0) * Diffuse(p,n,float3(0,2.6,_EntranceZ+3),6);
                light += float3(0.26,0.25,0.23) * Diffuse(p,n,_WorldSpaceCameraPos,3.2);
                light += float3(0.47,0.53,0.54) * (i.fresh * Diffuse(p,n,float3(5.2,4.6,-5.4),13));
                // The recess wall scatters entrance light back onto the turn and the ground.
                float3 fromWall = p - _ReturnBounce.xyz;
                float bounceDistance = length(fromWall * float3(0.85, 1.65, 0.67));
                float bounceSpread = (1.0 - smoothstep(0.8, 7.4, bounceDistance))
                    * smoothstep(-0.5, 1.2, fromWall.x) * (0.24 + 0.72 * saturate(n.y));
                light += float3(0.66,0.74,0.78) * (_ReturnBounce.w * bounceSpread);
                float fissure = smoothstep(12.6,13.6,p.y) * saturate(-n.y) *
                    exp(-0.7*abs(p.x+3+sin(p.z*0.45)+fault)*3 - 0.08*abs(p.z-7));
                float3 color = stone * light + fissure * float3(0.65,1.1,1.5);
                // Daylight at the end of the narrow exit passage gives the return door a silhouette.
                float opening = saturate((_EntranceZ + 0.15 - p.z) * 7) * smoothstep(0.6,1.05, 2.7 - abs(p.x) -
                    abs(p.y - 1.2) * 0.76 + fault * 0.38);
                color += opening * float3(0.59,0.83,1.0);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
