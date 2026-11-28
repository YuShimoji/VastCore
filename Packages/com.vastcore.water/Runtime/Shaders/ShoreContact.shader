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
                float4 flow : TEXCOORD2; // distance or sheet seed, along, elapsed phase, opacity
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

            // The lower edge follows the drawn sea as the upper sheet tears away.
            float2 RoadBreaker(float2 bankXZ, float u, float age, float energy)
            {
                float z = bankXZ.y;
                float pulse = RoadImpact(age);
                float curl = _Breaking.x;
                float spread = smoothstep(0.08, 0.23, age);
                float ragged = sin(z * 1.27 + u * 3.8) * 0.10 + sin(z * 4.1 - u * 2.3) * 0.056;
                float root = -1.25 - pulse * 0.41;
                float height = SeaHeight(bankXZ + float2(root, 0.0), root);
                float cut = WallCrestCollapse(age);
                float shoulders = 0.48 + 0.31 * sin(z * 1.86) + 0.17 * sin(z * 3.57 + 1.9);
                float rise = min(_Wave.x * 1.46, 3.35) * energy * pulse * lerp(1.0, 0.12 + 0.56 * shoulders, cut);
                float cap = max(height + rise * 0.42, _Bank.z - 0.04 + rise);
                float2 a = float2(root, height - 0.35);
                float2 b = float2(-0.52, lerp(height, cap, 0.62));
                float2 c = float2(-0.41 + 1.14 * curl * energy * pulse, cap + ragged * pulse);
                float throwDistance = (0.20 + curl * 1.47) * pulse * energy * lerp(1.0, 0.30 + 0.45 * shoulders, cut);
                float2 tip = float2(_Shore.y + throwDistance + spread * 0.25,
                    lerp(cap - (0.06 + curl * 0.44) * rise, _Bank.w + 0.075, spread));
                return CrestSection(a, b, c, tip, u);
            }

            float3 ImpactSheet(Attributes input, out float age, out float visibility)
            {
                float2 anchor = input.part.yz;
                float seed = input.part.w;
                float z = anchor.y;
                float energy = ShoreOvertopping(z) * (0.58 + 0.42 * ShoreImpactGroup(z));
                float release = lerp(0.018, 0.066, seed);
                float wallAge = frac(-MainWavePhase(anchor) * 0.15915494);
                float seconds = max(0.0, wallAge - release) * TWO_PI / max(0.1, _Wave.z);
                float span = lerp(0.34, 0.64, frac(seed * 7.31));
                // Older water is at the tip. The tail separates from the wall when the emission ends.
                float u = input.uv.x;
                float head = min(seconds, 1.19);
                float t = lerp(max(0.0, head - span), head, u);
                float across = input.uv.y * 2.0 - 1.0;
                float loft = frac(seed * 11.71);
                float spread = (0.44 + sin(u * PI) * 0.64) * (1.0 - 0.53 * u) * (1.0 - loft * 0.41);
                spread *= 1.0 + sin(u * 22.0 + across * 1.6 + seed * 12.7) * 0.17;
                float riseSpeed = lerp(5.0, 7.35, loft) * sqrt(energy);
                float driftSpeed = lerp(1.25, 3.95, seed) * (0.67 + _Breaking.x * 0.50) * energy * (1.0 - loft * 0.24);
                float tilt = lerp(-1.48, 1.28, frac(seed * 17.43)) + _Motion.y * 1.4;
                float ridges = sin(u * 17.0 + across * 2.9 + seed * 8.3) * 0.14 * sin(u * PI);
                float d = lerp(-0.71, 0.33, seed) + t * driftSpeed + across * t * 0.57 + ridges;
                float y = _Bank.z - 0.27 + t * (riseSpeed - 4.85 * t)
                    + t * (1.0 - across * across) * 0.46 + ridges;
                age = t;
                visibility = smoothstep(0.012, 0.135, seconds) * (1.0 - smoothstep(0.91, 1.19, seconds))
                    * smoothstep(0.13, 0.74, energy) * (1.0 - smoothstep(82.0, 155.0, abs(z)));
                float x = lerp(anchor.x, input.positionOS.x, spread) + d;
                z += (input.positionOS.z - z) * spread + tilt * t;
                return float3(x, _Shore.x + y, z);
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
                #if defined(COASTAL_BARRIER)
                if (mode > 3.5)
                {
                    float sheetAge;
                    float sheetVisibility;
                    float3 sheet = ImpactSheet(input, sheetAge, sheetVisibility);
                    output.positionWS = TransformObjectToWorld(sheet);
                    output.positionCS = TransformWorldToHClip(output.positionWS);
                    output.uv = input.uv;
                    output.flow = float4(input.part.w, sheet.z, sheetAge, sheetVisibility);
                    output.part = mode;
                    return output;
                }
                #endif
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
                    float2 section = RoadBreaker(input.positionOS.xz, u, age, energy);
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
                    bool mist = mode > 2.1;
                    float start = mist ? lerp(0.036, 0.171, seed.x) : lerp(0.026, 0.122, seed.x);
                    float lifetime = mist ? lerp(0.13, 0.24, seed.y) : lerp(0.11, 0.238, seed.y);
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
                    float seconds = max(0.0, age - start) * TWO_PI / max(0.1, _Wave.z);
                    float2 birth = float2(lerp(-0.52, _Shore.y, seed.y), _Bank.z + lerp(0.12, 0.62, seed.x));
                    float drift = lerp(2.4, 6.1, seed.x) * (0.48 + energy * 0.52);
                    float riseSpeed = lerp(2.6, 7.3, seed.y) * energy;
                    float alongSpeed = lerp(-2.3, 1.2, seed.y) + _Motion.y * 1.2;
                    if (mist)
                    {
                        birth.x -= 0.34;
                        drift = lerp(0.57, 2.43, seed.x) * (0.58 + energy * 0.42);
                        riseSpeed = lerp(1.1, 3.65, seed.y) * energy;
                        alongSpeed = _Motion.y * lerp(4.4, 9.6, seed.z) + lerp(-0.75, 0.85, seed.y);
                    }
                    z += alongSpeed * seconds;
                    d = birth.x + drift * seconds;
                    float gravity = mist ? 0.63 : 4.91;
                    y = birth.y + seconds * (riseSpeed - gravity * seconds);
                    fade *= smoothstep(_Bank.w + 0.08, _Bank.w + 0.34, y);
                    float size = saturate(seed.z / 0.63);
                    float radius = mist ? lerp(0.38, 1.17, seed.y) * (0.67 + flight)
                        : lerp(0.006, 0.027, size * size * size);
                    float stretch = mist ? lerp(1.05, 0.67, flight) : lerp(1.1, 1.73, size);
                    float density = mist ? lerp(0.39, 0.14, flight) : 1.14;
                    #else
                    d = edge + seed.y * 0.47 + flight * lerp(1.3, 6.2, seed.z) * energy;
                    float lift = min(amp * 2.19, 5.2) * lerp(0.36, 1.0, seed.x) * (mist ? 0.69 : 1.0);
                    y = 0.16 + arch * energy * lift - flight * flight * 0.19;
                    float radius = (mist ? lerp(0.066, 0.215, seed.y) : lerp(0.008, 0.035, seed.z)) * 0.68;
                    radius *= 0.66 + flight * (mist ? 0.82 : 0.41);
                    float stretch = 1.15 + (1.0 - arch) * 1.28;
                    float density = mist ? 0.49 : 1.0;
                    z += (_Motion.y * 0.85 + seed.x - 0.41) * flight * 5.4;
                    #endif
                    float2 corner = input.uv * 2.0 - 1.0;
                    float3 center = TransformObjectToWorld(float3(input.positionOS.x + d, _Shore.x + y, z));
                    float3 right = UNITY_MATRIX_I_V._m00_m10_m20;
                    float3 up = UNITY_MATRIX_I_V._m01_m11_m21;
                    #if defined(COASTAL_BARRIER)
                    if (!mist)
                    {
                        float3 velocity = TransformObjectToWorldDir(float3(drift, riseSpeed - 2.0 * gravity * seconds, alongSpeed), false);
                        float2 axis = float2(dot(velocity, right), dot(velocity, up) + 0.02);
                        axis *= rsqrt(max(0.0001, dot(axis, axis)));
                        corner = corner.x * float2(axis.y, -axis.x) + corner.y * stretch * axis;
                        stretch = 1.0;
                    }
                    #endif
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
                if (input.part > 3.5)
                {
                    float across = input.uv.y * 2.0 - 1.0;
                    float2 water = float2(p.y * 3.7, u * 6.4 - phase * 3.1);
                    float ragged = WaterNoise(water) * 0.63 + WaterNoise(water * 3.78) * 0.37;
                    float torn = abs(across + (ragged - 0.5) * 0.52);
                    float feather = 1.0 - smoothstep(0.55, 1.0, torn);
                    float end = smoothstep(0.003, 0.17, u) * (1.0 - smoothstep(0.72, 1.0, u));
                    // Stretch opens holes in the older water before the remaining strands thin out.
                    float stretch = smoothstep(0.31, 0.83, phase);
                    float2 fibres = float2(across * 3.8 + p.x * 8.64, u * 1.36);
                    float connected = WaterNoise(fibres) * 0.77 + WaterNoise(fibres * float2(5.9, 9.1)) * 0.23;
                    float holes = smoothstep(0.17 + 0.53 * stretch, 0.48 + 0.38 * stretch,
                        connected * 0.73 + ragged * 0.27);
                    float foam = ShoreFoam(water * 0.41);
                    float air = smoothstep(0.26, 0.72, foam * 0.54 + ragged * 0.48 + torn * 0.18);
                    float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                    float3 normal = SafeNormalize(cross(ddx(input.positionWS), ddy(input.positionWS)));
                    normal *= dot(normal, view) >= 0.0 ? 1.0 : -1.0;
                    float lift = 1.0 - saturate(dot(normal, view));
                    float3 body = lerp(_ShallowWater.rgb * 0.72, WaterSky(reflect(-view, normal)), lift * lift * 0.59);
                    color = lerp(body, white, 0.29 + air * 0.65);
                    alpha = strength * feather * end * holes * lerp(0.80, 1.0, air);
                }
                else if (input.part > 2.5)
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
                        float2 air = input.positionWS.xy * float2(1.85, 2.6) + input.positionWS.z * float2(0.83, 0.43);
                        air -= float2(1.07, 0.41) * _WaterTime;
                        float wisps = WaterNoise(air) * 0.61 + WaterNoise(air * 3.21 + 1.2) * 0.39;
                        float fringe = 1.0 - smoothstep(0.04, 0.97, radial);
                        alpha = fringe * fringe * strength * lerp(0.32, 0.91, wisps) * closeFade;
                    }
                    else
                    {
                        float distance = length(input.positionWS - GetCameraPositionWS());
                        alpha = (1.0 - smoothstep(0.02, 0.87, radial)) * strength * smoothstep(1.5, 4.2, distance);
                    }
                    #else
                    float breakup = phase < 0.5 ? 1.0 : lerp(0.12, 0.90, ShoreFoam(input.uv * 1.64 + p.y * 0.32));
                    if (phase > 0.5) radial += (WaterNoise(c * 3.1 + p.y * 0.49) - 0.5) * 0.36;
                    alpha = (1.0 - smoothstep(0.07, 0.95, radial)) * strength * breakup * closeFade;
                    #endif
                    color = phase > 0.5 && barrier ? white * 0.86 : white * (barrier ? 1.14 : 1.0);
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
