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
            #pragma multi_compile_local _ COASTAL_BARRIER
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

            float2 CrestSection(float2 a, float2 b, float2 c, float2 tip, float u)
            {
                float inv = 1.0 - u;
                return a * (inv * inv * inv) + b * (3.0 * inv * inv * u)
                    + c * (3.0 * inv * u * u) + tip * (u * u * u);
            }

            #if defined(COASTAL_BARRIER)
            static const bool barrier = true;

            float RoadImpact(float age)
            {
                return smoothstep(0.0, 0.061, age) * (1.0 - smoothstep(0.13, 0.275, age));
            }

            // The current root follows the drawn sea; particles sample the lip at their birth phase.
            float2 RoadBreaker(float2 bankXZ, float u, float age, float energy, bool currentRoot)
            {
                float z = bankXZ.y;
                float pulse = RoadImpact(age);
                float curl = _Breaking.x;
                float spread = smoothstep(0.08, 0.23, age);
                float ragged = sin(z * 1.27 + u * 3.8) * 0.10 + sin(z * 4.1 - u * 2.3) * 0.056;
                float root = -1.25 - pulse * 0.41;
                float height = (_Wave.x * 0.84) * cos(_Wave.y * _Motion.x * root - age * TWO_PI);
                if (currentRoot)
                    height = SeaHeight(bankXZ + float2(root, 0.0), root);
                float rise = min(_Wave.x * 1.46, 3.35) * energy * pulse;
                float cap = max(height + rise * 0.42, _Bank.z - 0.04 + rise);
                float2 a = float2(root, height - 0.35);
                float2 b = float2(-0.52, lerp(height, cap, 0.62));
                float2 c = float2(-0.41 + 1.14 * curl * energy * pulse, cap + ragged * pulse);
                float2 tip = float2(_Shore.y + (0.20 + curl * 1.47) * pulse * energy + spread * 0.25,
                    lerp(cap - (0.06 + curl * 0.44) * rise, _Bank.w + 0.075, spread));
                return CrestSection(a, b, c, tip, u);
            }

            #else
            static const bool barrier = false;

            float BeachCrestAge(float age)
            {
                return age > 0.75 ? age - 1.0 : age;
            }

            float2 BeachBreaker(float2 bankXZ, float u, float age, float group, out float visibility)
            {
                float arrival = BeachCrestAge(age);
                float pitch = smoothstep(-0.12, -0.017, arrival) * (1.0 - smoothstep(0.127, 0.305, arrival));
                float travel = smoothstep(-0.08, 0.33, arrival);
                float root = -_Shore.w - 1.74 + travel * (_Shore.w + 0.36);
                float height = SeaHeight(bankXZ + float2(root, 0.0), root);
                float rise = min(_Wave.x * 1.03, 1.61) * pitch * (0.63 + group * 0.45);
                float lean = _Breaking.x * smoothstep(-0.088, 0.102, arrival);
                float variation = 0.080 * sin(bankXZ.y * 2.93 + u * 3.6) + 0.043 * sin(bankXZ.y * 6.9 - u * 2.1);
                float2 a = float2(root, height - 0.15);
                float2 b = float2(root + 0.19, height + rise * 0.89);
                float2 c = float2(root + 0.66 + lean * rise * 0.66, height + rise * (1.12 + variation));
                float2 tip = float2(root + 0.79 + (0.58 + lean * 1.15) * rise,
                    height + rise * (0.88 - 1.24 * lean) + variation * pitch);
                visibility = pitch * (0.69 + group * 0.42) * smoothstep(0.20, 0.73, _Wave.x);
                return CrestSection(a, b, c, tip, u);
            }

            void DivertSwash(float2 position, float shoreward, inout float2 samplePosition,
                out float coverage, out float contactFoam)
            {
                coverage = 1.0;
                contactFoam = 0.0;
                int count = (int)_SwashShape.x;
                [loop] for (int i = 0; i < count; ++i)
                {
                    float4 stone = _SwashObstacles[i];
                    float2 ellipse = (position - stone.xy) / stone.zw;
                    if (abs(ellipse.x) > 3.3 || abs(ellipse.y) > 1.85) continue;
                    float downstream = ellipse.x * shoreward;
                    float across = abs(ellipse.y);
                    float wake = smoothstep(-0.16, 0.83, downstream) * (1.0 - smoothstep(1.02, 2.95, downstream))
                        * (1.0 - smoothstep(0.31, 0.96, across));
                    float rim = 1.0 - smoothstep(0.045, 0.23, abs(length(ellipse) - 1.13));
                    float turn = wake * _SwashShape.y;
                    coverage = min(coverage, 1.0 - turn);
                    samplePosition.y += sign(ellipse.y) * turn * stone.w * 1.12;
                    contactFoam = max(contactFoam, rim * _SwashShape.y * (1.0 - 0.54 * saturate(downstream)));
                }
            }

            #endif

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float z = input.positionOS.z;
                float u = input.uv.x;
                float mode = input.part.x;
                float edge = barrier ? 0.0 : -_Shore.w;
                float age = frac(-MainWavePhase(input.positionOS.xz + float2(edge, 0)) * 0.15915494);
                float alongFade = 1.0 - smoothstep(124.0, 273.0, abs(z));
                float group = ShoreImpactGroup(z);
                float over = ShoreOvertopping(z);
                float amp = _Wave.x;
                float d;
                float y;
                float live;
                float burst = pow(saturate(group * 1.03), 2.6);

                if (mode < 0.5)
                {
                    float reach = RunupPosition(age, barrier);
                    float limit = MaximumRunup(z, amp, _Bank, _Shore);
                    float start = barrier ? _Shore.y : 0.0;
                    d = start + u * reach * limit;
                    float runupThickness = 0.007 + 0.029 * (1.0 - u) * reach;
                    y = (barrier ? _Bank.w : d * _Bank.y) + runupThickness;
                    live = alongFade * smoothstep(0.008, 0.24, reach) * (barrier ? over : smoothstep(0.045, 0.5, amp));
                }
                else if (mode < 1.5)
                {
                    #if defined(COASTAL_BARRIER)
                    float energy = over * (0.35 + 0.65 * burst);
                    float2 section = RoadBreaker(input.positionOS.xz, u, age, energy, true);
                    z += _Motion.y * u * u * energy * RoadImpact(age) * 0.76;
                    live = alongFade * RoadImpact(age) * smoothstep(0.01, 0.56, energy);
                    #else
                    float2 section = BeachBreaker(input.positionOS.xz, u, age, group, live);
                    live *= alongFade;
                    #endif
                    d = section.x;
                    y = section.y;
                }
                #if defined(COASTAL_BARRIER)
                else if (mode > 2.5)
                {
                    float falling = smoothstep(0.061, 0.146, age) * (1.0 - smoothstep(0.27, 0.415, age));
                    float cap = saturate(u * 3.0);
                    float fall = saturate((u - 0.3333) * 1.5);
                    d = lerp(0.008, _Shore.y + 0.025, cap) + fall * (0.12 + burst * falling * 0.27);
                    y = lerp(_Bank.z + 0.028 + 0.045 * (1.0 - cap), _Bank.w + 0.011, fall * fall);
                    live = alongFade * falling * over * (0.40 + burst * 0.60);
                }
                #endif
                else
                {
                    float3 seed = input.part.yzw;
                    #if defined(COASTAL_BARRIER)
                    bool mist = seed.z > 0.63;
                    // Mist is released during the whole strike as the lip shatters against the wall.
                    float start = mist ? lerp(0.032, 0.164, seed.x) : lerp(0.032, 0.108, seed.x);
                    float lifetime = mist ? lerp(0.103, 0.198, seed.y) : lerp(0.105, 0.248, seed.y);
                    #else
                    bool mist = seed.z > 0.78;
                    float start = lerp(0.032, 0.099, seed.x);
                    float lifetime = lerp(0.105, 0.265, seed.y);
                    #endif
                    float flight = saturate((age - start) / lifetime);
                    float fade = smoothstep(0.0, 0.14, flight) * (1.0 - smoothstep(0.51, 1.0, flight));
                    float energy = (0.32 + burst * 0.68) * (barrier ? over : smoothstep(0.15, 1.08, amp) * 0.56);
                    float arch = 4.0 * flight * (1.0 - flight);
                    #if defined(COASTAL_BARRIER)
                    float2 birth = RoadBreaker(input.positionOS.xz, lerp(0.62, 0.94, seed.y), start,
                        over * (0.35 + 0.65 * burst), false);
                    float seconds = max(0.0, age - start) * TWO_PI / max(0.1, _Wave.z);
                    float drift = lerp(1.5, 4.4, seed.x) * (0.6 + energy * 0.40);
                    float riseSpeed = lerp(0.95, 4.6, seed.y) * energy;
                    if (mist)
                    {
                        float fromWall = smoothstep(0.63, 1.0, seed.z);
                        birth = lerp(birth, float2(_Shore.y * 0.72, _Bank.z + 0.14), fromWall * 0.86);
                        drift = lerp(0.67, 3.10, seed.x) * (0.60 + energy * 0.40);
                        riseSpeed = lerp(0.64, 2.75, seed.y) * energy;
                        z += seconds * lerp(-2.15, 2.35, seed.y);
                    }
                    d = birth.x + drift * seconds;
                    y = birth.y + seconds * (riseSpeed - (mist ? 0.59 : 4.91) * seconds);
                    fade *= smoothstep(_Bank.w + 0.08, _Bank.w + 0.34, y);
                    float radius = mist ? lerp(0.17, 0.42, seed.y) * (0.60 + flight * 1.18)
                        : lerp(0.008, 0.039, seed.z) * (0.66 + flight * 0.41);
                    float stretch = mist ? 1.07 + flight * 0.31 : 1.15 + (1.0 - arch) * 1.28;
                    float density = mist ? lerp(0.56, 0.26, flight) : 1.0;
                    #else
                    d = edge + seed.y * 0.47 + flight * lerp(1.3, 6.2, seed.z) * energy;
                    float lift = min(amp * 2.19, 5.2) * lerp(0.36, 1.0, seed.x) * (mist ? 0.69 : 1.0);
                    y = 0.16 + arch * energy * lift - flight * flight * 0.19;
                    float radius = (mist ? lerp(0.066, 0.215, seed.y) : lerp(0.008, 0.035, seed.z)) * 0.68;
                    radius *= 0.66 + flight * (mist ? 0.82 : 0.41);
                    float stretch = 1.15 + (1.0 - arch) * 1.28;
                    float density = mist ? 0.49 : 1.0;
                    #endif
                    z += (_Motion.y * 0.85 + seed.x - 0.41) * flight * 5.4;
                    float2 corner = input.uv * 2.0 - 1.0;
                    float3 center = TransformObjectToWorld(float3(input.positionOS.x + d, _Shore.x + y, z));
                    float3 right = UNITY_MATRIX_I_V._m00_m10_m20;
                    float3 up = UNITY_MATRIX_I_V._m01_m11_m21;
                    output.positionWS = center + radius * (right * corner.x + up * corner.y * stretch);
                    output.positionCS = TransformWorldToHClip(output.positionWS);
                    output.uv = input.uv;
                    output.flow = float4(d, z, mist ? 1.0 : 0.0, fade * energy * density * _Breaking.y);
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

                #if defined(COASTAL_BARRIER)
                if (input.part > 2.5)
                {
                    float strand = WaterNoise(float2(p.y * 3.7, u * 1.8 - phase * 4.3)) * 0.60
                        + WaterNoise(float2(p.y * 0.68, 0.1)) * 0.40;
                    float rivulet = ShoreFoam(float2(u * 1.16 - phase * 4.2, p.y));
                    float ends = smoothstep(0.0, 0.12, u) * (1.0 - smoothstep(0.91, 1.0, u));
                    color = lerp(float3(0.15, 0.24, 0.25), white, rivulet * 0.78);
                    alpha = strength * ends * smoothstep(0.25, 0.79, strand) * (0.38 + rivulet * 0.55);
                }
                else
                #endif
                if (input.part > 1.5)
                {
                    float2 c = input.uv * 2.0 - 1.0;
                    float closeFade = smoothstep(2.0, 6.0, length(input.positionWS - GetCameraPositionWS()));
                    float radial = dot(c, c);
                    #if defined(COASTAL_BARRIER)
                    if (phase > 0.5)
                    {
                        // Broad, low-opacity fringes overlap near the wall without individual round puffs.
                        float2 air = input.positionWS.xy * float2(2.9, 3.7) + input.positionWS.z * float2(1.37, 0.53);
                        float wisps = WaterNoise(air) * 0.72 + WaterNoise(air * 2.81 + 1.2) * 0.28;
                        float fringe = 1.0 - smoothstep(0.0, 0.96, radial);
                        alpha = fringe * fringe * strength * lerp(0.32, 0.90, wisps) * closeFade;
                    }
                    else
                    {
                        alpha = (1.0 - smoothstep(0.07, 0.95, radial)) * strength * closeFade;
                    }
                    #else
                    float breakup = phase < 0.5 ? 1.0 : lerp(0.12, 0.90, ShoreFoam(input.uv * 1.64 + p.y * 0.32));
                    if (phase > 0.5) radial += (WaterNoise(c * 3.1 + p.y * 0.49) - 0.5) * 0.36;
                    alpha = (1.0 - smoothstep(0.07, 0.95, radial)) * strength * breakup * closeFade;
                    #endif
                    color = white;
                }
                else if (input.part < 0.5)
                {
                    float advance = RunupPosition(phase, barrier);
                    float distance = MaximumRunup(p.y, _Wave.x, _Bank, _Shore);
                    // The same streaks move inland and then back with the advancing and receding edge.
                    float2 flow = (p - float2(advance * distance * 0.66, _Motion.y * advance * 0.46))
                        * float2(0.75, 1.54);
                    float coverage = 1.0;
                    float rockFoam = 0.0;
                    #if !defined(COASTAL_BARRIER)
                    if (_SwashShape.x >= 1.0)
                    {
                        float direction = lerp(1.0, -1.0, smoothstep(0.435, 0.55, phase));
                        DivertSwash(p, direction, flow, coverage, rockFoam);
                    }
                    #endif
                    float cluster = WaterNoise(flow * float2(0.68, 0.50)) * 0.71 + WaterNoise(flow * 1.91) * 0.29;
                    float frontAt = 0.82 + 0.15 * cluster + 0.023 * sin(p.y * 4.1);
                    float front = 1.0 - smoothstep(frontAt - 0.04, frontAt + 0.077, u);
                    float edge = 1.0 - smoothstep(0.013, 0.067, abs(u - frontAt + 0.035));
                    float textureFoam = ShoreFoam(flow + float2(cluster * 0.47, 0.0));
                    float patches = smoothstep(barrier ? 0.38 : 0.31, 0.74, cluster + textureFoam * 0.26);
                    float brokenFilm = smoothstep(0.22, 0.76, textureFoam * patches) * 0.69;
                    float tornEdge = edge * smoothstep(0.24, 0.69, textureFoam * 0.76 + patches * 0.33) * 0.51;
                    float foam = saturate(brokenFilm + tornEdge);
                    // Scattered foam and the clear moving film have separate coverage.
                    float foamAlpha = max(foam * _FilmFoam, rockFoam * (0.17 + textureFoam * 0.53)) * _Motion.w;
                    float2 ripples = UnpackNormal(SAMPLE_TEXTURE2D(_RippleNormal, sampler_RippleNormal, flow * 0.44)).xy * 0.51;
                    float3 normal = TransformObjectToWorldNormal(normalize(float3(ripples.x, 1.0, ripples.y)));
                    float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                    float3 reflection = reflect(-view, normal);
                    float glancing = pow(1.0 - saturate(dot(normal, view)), 3.4);
                    float filmAlpha = (0.052 + glancing * 0.21) * (0.43 + 0.57 * patches);
                    color = lerp(WaterSky(reflection) * 0.65, white, foamAlpha / max(0.001, filmAlpha + foamAlpha));
                    alpha = strength * front * coverage * saturate(filmAlpha + foamAlpha);
                }
                else
                {
                    float2 streak = float2(p.y * 1.30, u * 7.6 - phase * 19.0);
                    float n = WaterNoise(streak) * 0.55 + WaterNoise(streak * 3.4 + 4.0) * 0.45;
                    float rim = smoothstep(0.015, 0.16, u) * (1.0 - smoothstep(0.66 + 0.12 * n, 0.99, u));
                    float grains = ShoreFoam(streak * 0.34 + float2(2.2, -0.37));
                    float droplets = smoothstep(0.20 + 0.17 * u, 0.78, n * 0.73 + grains * 0.60);
                    float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                    float3 surfaceNormal = SafeNormalize(cross(ddx(input.positionWS), ddy(input.positionWS)));
                    surfaceNormal *= dot(surfaceNormal, view) >= 0.0 ? 1.0 : -1.0;
                    float fresnel = 0.02 + 0.64 * pow(1.0 - saturate(dot(surfaceNormal, view)), 4.0);
                    float thinning = smoothstep(barrier ? 0.43 : 0.20, 0.94, u);
                    float thickness = lerp(barrier ? 1.48 : 0.43, 0.011, thinning) * (0.72 + 0.28 * n);
                    float bodyAlpha = (1.0 - exp2(-thickness * 3.4)) * (0.80 + fresnel * 0.20);
                    float foamEdge = smoothstep(0.34, 0.85, n * 0.49 + grains * 0.79);
                    #if defined(COASTAL_BARRIER)
                    float collapseFoam = 1.0;
                    #else
                    float collapseFoam = smoothstep(-0.025, 0.117, BeachCrestAge(phase));
                    #endif
                    float whitewater = foamEdge * smoothstep(0.46, 0.93, u) * _Motion.w * collapseFoam;
                    float3 body = _ShallowWater.rgb * lerp(0.89, 1.61, n + thinning * 0.23);
                    float3 tinted = lerp(body, WaterSky(reflect(-view, surfaceNormal)) * 0.68, fresnel);
                    color = lerp(tinted, white, whitewater);
                    alpha = strength * rim * saturate(bodyAlpha + whitewater * 0.57) * lerp(1.0, 0.48 + 0.77 * droplets, thinning);
                }
                return half4(WaterDistanceFog(color, input.positionWS), saturate(alpha));
            }
            ENDHLSL
        }
    }
}
