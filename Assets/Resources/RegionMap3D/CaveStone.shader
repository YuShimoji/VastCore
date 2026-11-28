Shader "Vastcore/Cave Stone"
{
    Properties
    {
        _EntranceZ ("Entrance plane", Float) = -26.0
        _FreshCollapse ("Fresh collapse", Float) = 0
        _ReturnBounce ("Return wall bounce", Vector) = (0,0,0,0)
        _BedrockDetail ("Chamber bedrock", Float) = 0
        _CliffPhoto ("Rock face color", 2D) = "gray" {}
        _CliffNormal ("Rock face normal", 2D) = "bump" {}
        _CaveGravel ("Gravel color", 2D) = "gray" {}
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

            TEXTURE2D(_CliffPhoto); SAMPLER(sampler_CliffPhoto);
            TEXTURE2D(_CliffNormal); SAMPLER(sampler_CliffNormal);
            TEXTURE2D(_CaveGravel); SAMPLER(sampler_CaveGravel);

            CBUFFER_START(UnityPerMaterial)
                float _EntranceZ;
                float _FreshCollapse;
                float _BedrockDetail;
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

            float3 Bedrock(float3 p, float3 surface, float fault, float amount, inout float3 n)
            {
                // A common world-space projection at the two sides of each join.
                float3 weights = pow(abs(surface), 4);
                weights /= max(0.001, weights.x + weights.y + weights.z);
                float3 q = p * float3(0.29, 0.39, 0.27);
                q += float3(fault * 0.19, 0.07 * q.x + 0.09 * q.z + fault * 0.15, fault * -0.17);
                float2 x = q.zy + float2(0.43, 0.17);
                float2 y = q.xz + float2(0.25, 0.71);
                float2 z = q.xy + float2(0.12, 0.51);
                float3 fractured = SAMPLE_TEXTURE2D(_CliffPhoto, sampler_CliffPhoto, x).rgb * weights.x
                                 + SAMPLE_TEXTURE2D(_CliffPhoto, sampler_CliffPhoto, y).rgb * weights.y
                                 + SAMPLE_TEXTURE2D(_CliffPhoto, sampler_CliffPhoto, z).rgb * weights.z;
                // The second, broad projection breaks up recognizable patches of the scan.
                float3 broad = SAMPLE_TEXTURE2D(_CliffPhoto, sampler_CliffPhoto, float2(-x.y, x.x) * 0.37 + 0.16).rgb * weights.x
                             + SAMPLE_TEXTURE2D(_CliffPhoto, sampler_CliffPhoto, float2(-y.y, y.x) * 0.37 + 0.54).rgb * weights.y
                             + SAMPLE_TEXTURE2D(_CliffPhoto, sampler_CliffPhoto, float2(-z.y, z.x) * 0.37 + 0.33).rgb * weights.z;
                float3 rock = lerp(fractured, broad, 0.28 + 0.18 * smoothstep(0.35, 0.78, fault));
                float2 nx = UnpackNormal(SAMPLE_TEXTURE2D(_CliffNormal, sampler_CliffNormal, x)).xy;
                float2 ny = UnpackNormal(SAMPLE_TEXTURE2D(_CliffNormal, sampler_CliffNormal, y)).xy;
                float2 nz = UnpackNormal(SAMPLE_TEXTURE2D(_CliffNormal, sampler_CliffNormal, z)).xy;
                float3 slope = weights.x * float3(0, nx.y, nx.x) + weights.y * float3(ny.x, 0, ny.y)
                             + weights.z * float3(nz.x, nz.y, 0);
                float floorMask = smoothstep(0.52, 0.91, surface.y) * (1 - smoothstep(0.02, 1.35, p.y));
                float drift = abs(p.x + 0.62 * sin(p.z * 0.28));
                float packed = 1 - smoothstep(1.45, 4.5, drift + 1.8 * (fault - 0.5));
                float3 gravel = SAMPLE_TEXTURE2D(_CaveGravel, sampler_CaveGravel, p.xz * 0.52 + fault * 0.18).rgb;
                float gravelValue = dot(gravel, float3(0.2126, 0.7152, 0.0722));
                gravel = lerp(gravel, gravelValue.xxx, 0.57) * (0.69 + 0.25 * packed);
                n = normalize(n + (0.73 - 0.36 * floorMask) * amount * (slope - dot(slope, n) * n));
                return lerp(rock, gravel, floorMask * (0.38 + 0.35 * packed));
            }

            float2 JoinedBeds(float3 p, float bed, float fault)
            {
                // Trace the beds and the offset across the joint in the rock volume.
                float bank = smoothstep(4.8, 10.2, abs(p.x)) * (1 - smoothstep(5.5, 9.4, abs(p.z + 0.9)));
                float crosscut = p.z + 0.31 * p.x + 0.36 * p.y
                    - lerp(-3.05, 8.3, smoothstep(-6, 6, p.x)) + 0.68 * (fault - 0.55);
                float displacedBed = bed + 0.67 * (fault - 0.55) + 0.22 * smoothstep(-0.12, 0.17, crosscut);
                float upper = displacedBed - 6.58;
                float lower = displacedBed - 5.34 - 0.10 * (fault - 0.55);
                float upperSeam = 1 - smoothstep(0.025, 0.16, abs(upper));
                float lowerSeam = (1 - smoothstep(0.027, 0.13, abs(lower)))
                    * (1 - smoothstep(0.27, 1.36, abs(crosscut)));
                float broken = 1 - smoothstep(0.036, 0.13, abs(crosscut));
                broken *= smoothstep(4.62, 5.14, displacedBed) * (1 - smoothstep(6.7, 7.25, displacedBed));
                float rim = smoothstep(0.035, 0.13, upper) * (1 - smoothstep(0.20, 0.48, upper));
                float wear = lerp(0.18, 0.94, smoothstep(0.36, 0.74, fault));
                return bank * float2(max(max(wear * upperSeam, lowerSeam * 0.58), broken * 0.85), rim * wear);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 p = i.pos, n = normalize(i.normal);
                float3 surface = n;
                float chamber = _BedrockDetail * smoothstep(-16, -9, p.z);
                float fault = Noise(p * 0.39) + 0.33 * Noise(p * 1.55);
                float stratum = p.y * 1.7 + p.z * 0.17 + p.x * 0.12 + 1.45 * fault;
                float seam = smoothstep(0.68, 0.95, sin(stratum * 5.4));
                seam *= 1 - chamber * (0.83 + 0.12 * smoothstep(0.38, 0.66, fault));
                float grit = Noise(p * 11) - 0.5;
                float bedding = 0.58 * Noise(p * float3(1.4,4.2,1.3)) + 0.3 * fault + 0.12 * grit;
                float bed = p.y + 0.10 * p.z - 0.064 * p.x;
                float2 joinedBeds = 0;
                if (chamber > 0.001 && bed > 3.84 && bed < 7.65 && abs(p.x) > 4.8)
                    joinedBeds = JoinedBeds(p, bed, fault) * chamber;
                float relief = bedding * lerp(0.16, 0.055, chamber) - 0.032 * seam
                    - 0.005 * joinedBeds.x + 0.004 * joinedBeds.y;
                float3 across = cross(ddy(p), n), along = cross(n, ddx(p));
                float det = dot(ddx(p), across);
                n = normalize(abs(det) * n - sign(det) * (ddx(relief) * across + ddy(relief) * along));

                float mineral = smoothstep(0.56, 0.72, fault);
                float3 stone = lerp(float3(0.24,0.20,0.16), float3(0.38,0.37,0.335), mineral);
                stone *= 0.61 + bedding * 0.72 + grit * 0.18 - seam * 0.12;
                if (chamber > 0.001)
                {
                    float3 rock = Bedrock(p, surface, fault, chamber, n);
                    float sediment = Noise(float3(p.x * 0.19, bed * 1.24 + 0.30 * fault, p.z * 0.18));
                    float3 bedColor = lerp(float3(0.69, 0.75, 0.79), float3(0.97, 0.89, 0.77),
                        smoothstep(0.23, 0.78, sediment));
                    stone = lerp(stone, rock * bedColor * (0.86 + 0.22 * mineral), chamber);
                    stone *= (1 - 0.28 * joinedBeds.x) + float3(0.14, 0.10, 0.065) * joinedBeds.y;
                }
                float3 freshStone = lerp(float3(0.42,0.39,0.34) * (0.77 + 0.32 * bedding + 0.1 * grit),
                    stone * float3(1.30, 1.27, 1.20), chamber);
                stone = lerp(stone, freshStone, 0.72 * i.fresh);
                // Light spreads from the entrance and the roof fissure. Wall bounce is muted.
                float3 light = float3(0.082,0.096,0.12);
                light += float3(0.67,1.09,1.49) * Diffuse(p,n,float3(-3,11,7),9.5);
                light += float3(1.16,0.86,0.63) * Diffuse(p,n,float3(9,4.6,10),6.7);
                light += float3(0.76,0.66,0.56) * Diffuse(p,n,float3(-10,6,-3),5.5);
                light += float3(0.65,0.79,1.0) * Diffuse(p,n,float3(0,2.6,_EntranceZ+3),6);
                light += float3(0.26,0.25,0.23) * Diffuse(p,n,_WorldSpaceCameraPos,3.2) * (1 - 0.55 * chamber);
                light += float3(0.47,0.53,0.54) * (i.fresh * Diffuse(p,n,float3(5.2,4.6,-5.4),13));
                // The recess wall scatters entrance light back onto the turn and the ground.
                float3 fromWall = p - _ReturnBounce.xyz;
                float bounceDistance = length(fromWall * float3(0.85, 1.65, 0.67));
                float bounceSpread = (1.0 - smoothstep(0.8, 7.4, bounceDistance))
                    * smoothstep(-0.5, 1.2, fromWall.x) * (0.24 + 0.72 * saturate(n.y));
                light += float3(0.66,0.74,0.78) * (_ReturnBounce.w * bounceSpread);
                // Diffuse bounce from the chamber floor reaches the underside of the span.
                float reflected = Diffuse(p, n, float3(-2.4, 0.35, -5.6), 8.8);
                float shelfBounce = Diffuse(p, n, float3(8.7, 7.1, 9.6), 7.4);
                float luminance = dot(light, float3(0.2126, 0.7152, 0.0722));
                light = lerp(light, luminance.xxx, 0.42 * chamber);
                light += chamber * (0.018 + float3(0.31, 0.28, 0.23) * reflected
                    + float3(0.19, 0.18, 0.16) * shelfBounce);
                float fissure = smoothstep(12.6,13.6,p.y) * saturate(-n.y) *
                    exp(-0.7*abs(p.x+3+sin(p.z*0.45)+fault)*3 - 0.08*abs(p.z-7));
                float3 color = stone * light + fissure * lerp(float3(0.65,1.1,1.5), float3(0.46,0.65,0.79), chamber);
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
