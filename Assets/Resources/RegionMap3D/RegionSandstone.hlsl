// Coordinates are metres in the common frame of the inner terrain, outer terrain and fallen blocks.
#ifndef REGION_SANDSTONE_INCLUDED
#define REGION_SANDSTONE_INCLUDED

float SandHash(float2 cell)
{
    float3 p = frac(cell.xyx * float3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yzx + 33.33);
    return frac((p.x + p.y) * p.z);
}

float SandNoise(float2 uv)
{
    float2 cell = floor(uv);
    float2 f = frac(uv);
    f = f * f * (3.0 - 2.0 * f);
    float n = lerp(lerp(SandHash(cell), SandHash(cell + float2(1, 0)), f.x),
                   lerp(SandHash(cell + float2(0, 1)), SandHash(cell + 1.0), f.x), f.y);
    float2 footprint = fwidth(uv);
    return lerp(n, 0.5, smoothstep(0.35, 1.2, max(footprint.x, footprint.y)));
}

float SandTriplanar(float3 p, float3 weights)
{
    return dot(weights, float3(SandNoise(p.zy), SandNoise(p.xz + 7.31), SandNoise(p.xy - 2.19)));
}

// A broken bedding plane, fading to its mean when several laminae fit inside a pixel.
float SandSeam(float phase, float width)
{
    float footprint = max(fwidth(phase), 0.002);
    float dist = abs(frac(phase + 0.5) - 0.5);
    float edge = min(0.23, 0.5 * footprint);
    float coverage = 1.0 - smoothstep(width - edge, width + edge, dist);
    return lerp(coverage, 2.0 * width, smoothstep(0.20, 0.65, footprint));
}

// Angular chips in size- and position-jittered cells. Opposite facets pick up light and shade.
float SandChips(float2 uv)
{
    float2 id = floor(uv);
    float choice = SandHash(id);
    float radius = 0.05 + 0.22 * choice * choice;
    float2 center = lerp(radius, 1.0 - radius, float2(SandHash(id + 13.3), SandHash(id - 7.1)));
    float2 offset = (frac(uv) - center) / radius;
    float angle = SandHash(id + 3.8) * 6.283185;
    float2 axis = float2(cos(angle), sin(angle));
    float2 q = float2(dot(offset, axis), dot(offset, float2(-axis.y, axis.x)));
    float outline = max(abs(q.x) + 0.42 * abs(q.y), abs(q.y * 1.45 + 0.32 * q.x));
    float2 footprint = fwidth(uv);
    float mask = (1.0 - smoothstep(0.80, 1.0, outline))
               * (1.0 - smoothstep(0.10, 0.46, max(footprint.x, footprint.y)));
    float facet = smoothstep(-0.50, 0.36, offset.x - 0.7 * offset.y);
    return mask * (facet * 0.31 - 0.18 + 0.28 * (SandHash(id - 2.4) - 0.5));
}

// Four surfaces: exposed bedrock, talus, sand/soil and patchy dry ground cover.
// surface = talus density, metres above the caprock contact, moisture for cover, dry-land coverage.
half3 SandstoneSurface(float3 p, float3 n, float4 surface, half tint, half looseRock, out float bedHeightOut, out float4 laminaOut)
{
    float3 weights = n * n;
    weights *= weights;
    weights /= max(dot(weights, float3(1, 1, 1)), 0.001);
    float mottling = SandTriplanar(p * 0.22, weights);
    float grains = SandTriplanar(p * 9.7, weights);
    float broad = SandNoise(p.xz * 0.023);
    float broken = SandTriplanar(p * 0.065 + 3.5, weights);
    float mineral = 0.63 * mottling + 0.37 * grains;
    bedHeightOut = 0.0;
    laminaOut = 0.0;
    if (looseRock > 0.5)
    {
        float3 block = lerp(float3(0.56, 0.37, 0.28), float3(0.85, 0.62, 0.44), broken);
        return block * (0.79 + 0.43 * mineral) * (0.74 + 0.36 * tint);
    }

    float bedHeight = surface.y + 1.8 * (broad - 0.5) + 0.28 * (mottling - 0.5);
    bedHeightOut = bedHeight;
    float bed = bedHeight * 0.12 + 0.78 * SandNoise(float2(bedHeight * 0.072, 5.4));
    float unit = SandNoise(float2(bedHeight * 0.28, 3.7));
    float3 rock = lerp(float3(0.64, 0.31, 0.20), float3(0.91, 0.64, 0.40), 0.62 * unit + 0.38 * broken);
    float mud = (1.0 - smoothstep(-6.0, 1.5, bedHeight)) * smoothstep(-38.0, -29.0, bedHeight);
    float3 mudstone = lerp(float3(0.42, 0.32, 0.35), float3(0.73, 0.58, 0.54), unit);
    rock = lerp(rock, mudstone, mud);

    // Finer wind-deposited sets change direction at the erosional boundary between beds.
    float set = floor(bed);
    float2 dip = lerp(float2(0.29, 0.37), float2(0.48, 0.63), SandHash(float2(set, 17.0)));
    dip.y *= SandHash(float2(set, 1.3)) > 0.5 ? 1.0 : -1.0;
    float lamina = (bedHeight + dot(dip, p.xz)) * 0.93 + 2.1 * broken + 0.38 * mottling;
    lamina += 0.42 * SandNoise(float2(lamina * 0.47, 3.2));
    float crossBed = SandSeam(lamina, 0.13) * smoothstep(0.03, 0.22, frac(bed))
                   * (1.0 - smoothstep(0.68, 0.98, frac(bed))) * smoothstep(0.20, 0.68, broken);
    rock += float3(0.22, 0.18, 0.12) * crossBed;
    // 凹凸用：斜交葉理の位相、縞が出る度合い（縞の形を除く）、傾きの向き
    laminaOut = float4(lamina, smoothstep(0.03, 0.22, frac(bed)) * (1.0 - smoothstep(0.68, 0.98, frac(bed))) * smoothstep(0.20, 0.68, broken), dip);
    float seam = (0.06 + 0.17 * broken) * SandSeam(bed, 0.018 + 0.020 * mottling);
    float pores = SandTriplanar(p * 1.82 + mottling, weights);
    rock *= 0.76 + 0.40 * mineral + 0.13 * (pores - 0.5) - seam;

    // Runoff follows gravity on each cliff face. Its extent breaks up vertically and between outcrops.
    float3 streakPos = p * float3(0.52, 0.027, 0.52);
    streakPos.xz += 0.34 * mottling;
    float streak = SandTriplanar(streakPos, weights);
    float patches = SandTriplanar(p * float3(0.085, 0.033, 0.085) + 21.3, weights);
    float varnish = smoothstep(0.54, 0.79, streak) * smoothstep(0.37, 0.67, patches)
                  * (1.0 - smoothstep(0.38, 0.73, n.y));
    rock = lerp(rock, float3(0.27, 0.20, 0.18) * (0.78 + 0.50 * mineral), varnish * 0.88);

    float sandPatch = 0.52 * broad + 0.30 * mottling + 0.18 * grains;
    float3 soil = lerp(float3(0.56, 0.40, 0.29), float3(0.87, 0.71, 0.51), sandPatch);
    float cover = smoothstep(0.60, 0.78, 0.56 * broad + 0.44 * mottling)
                * surface.z * smoothstep(0.78, 0.97, n.y);
    soil = lerp(soil, float3(0.43, 0.41, 0.28) * (0.65 + 0.70 * grains), cover);
    float2 ground = float2(0.8 * p.x + 0.6 * p.z, -0.6 * p.x + 0.8 * p.z);
    float2 drift = float2(mottling, pores);
    float gravel = SandChips(ground * 4.6 + drift) + 0.75 * SandChips(p.xz * 13.3 - drift);
    // Talus is broken from the orange cliff above, so it stays warmer than the mudstone it covers.
    float3 rubble = lerp(float3(0.58, 0.42, 0.33), float3(0.80, 0.63, 0.48), sandPatch);
    rubble = lerp(rubble, mudstone, mud * 0.45);
    float talus = smoothstep(0.04, 0.82, surface.x);
    float driftSand = smoothstep(0.47, 0.80, 0.62 * mottling + 0.38 * broken);
    float3 groundColor = lerp(soil, rubble, talus * (1.0 - 0.56 * driftSand));
    groundColor *= 0.82 + 0.24 * grains + 0.14 * pores + (0.55 + talus) * gravel;
    float rockFace = 1.0 - smoothstep(0.52, 0.82, n.y);
    return lerp(groundColor, rock, rockFace);
}

// ---- 表面の凹凸（RegionSurfaceRelief.cs が場面を作る時に焼いた傾きの画像を読む） ----

// 地層の柱の起伏（m）。層ごとの硬さで張り出しと引っ込みが変わり、層の境は溝、下の泥岩の帯は奥へ下がる。
// 色の縞（SandstoneSurface の bed・unit・mud）と同じ式なので、溝と色の境は一致する。
float SandstoneRelief(float bedHeight)
{
    float bed = bedHeight * 0.12 + 0.78 * SandNoise(float2(bedHeight * 0.072, 5.4));
    float set = floor(bed);
    float hard = SandHash(float2(set, 9.1)) - 0.5;
    float below = SandHash(float2(set - 1.0, 9.1)) - 0.5;
    float protrude = lerp(below, hard, smoothstep(0.0, 0.04, frac(bed)));
    float unit = SandNoise(float2(bedHeight * 0.28, 3.7));
    float mud = (1.0 - smoothstep(-6.0, 1.5, bedHeight)) * smoothstep(-38.0, -29.0, bedHeight);
    return 0.9 * protrude + 0.15 * (unit - 0.5) - 0.18 * SandSeam(bed, 0.025) - 0.45 * mud;
}

TEXTURE2D(_RegionReliefStrataSlope);
SAMPLER(sampler_RegionReliefStrataSlope);
TEXTURE2D(_RegionReliefRockSlope);
SAMPLER(sampler_RegionReliefRockSlope);
float4 _RegionReliefStrata;  // x 柱の最低の高さ m、y 1 / 柱の幅 m、z 強さ、w 1 で有効
float4 _RegionReliefRock;    // x 1 / 繰り返しの一辺 m、y 強さ、z 薄れ始める距離 m、w 読まなくなる距離 m

bool RegionReliefEnabled() { return _RegionReliefStrata.w > 0.5; }

float3 SandAxis(int axis) { return axis == 0 ? float3(1, 0, 0) : (axis == 1 ? float3(0, 1, 0) : float3(0, 0, 1)); }

// 岩肌の細かな凹凸の、体積の勾配（m/m）。biplanar：重みの大きい 2 面だけ読む（Quilez）。
// 面の切り替わりで mip の線が出ないよう、uv の微分は分岐の外で出して渡す
float3 SandRockGradient(float3 q, float3 qdx, float3 qdy, float3 n)
{
    float3 a = abs(n);
    int3 ma = (a.x > a.y && a.x > a.z) ? int3(0, 1, 2) : ((a.y > a.z) ? int3(1, 2, 0) : int3(2, 0, 1));
    int3 mi = (a.x < a.y && a.x < a.z) ? int3(0, 1, 2) : ((a.y < a.z) ? int3(1, 2, 0) : int3(2, 0, 1));
    int3 me = int3(3, 3, 3) - mi - ma;
    float2 s0 = SAMPLE_TEXTURE2D_GRAD(_RegionReliefRockSlope, sampler_RegionReliefRockSlope,
        float2(q[ma.y], q[ma.z]), float2(qdx[ma.y], qdx[ma.z]), float2(qdy[ma.y], qdy[ma.z])).rg;
    float2 s1 = SAMPLE_TEXTURE2D_GRAD(_RegionReliefRockSlope, sampler_RegionReliefRockSlope,
        float2(q[me.y], q[me.z]), float2(qdx[me.y], qdx[me.z]), float2(qdy[me.y], qdy[me.z])).rg;
    float2 w = saturate((float2(a[ma.x], a[me.x]) - 0.5773) / (1.0 - 0.5773));
    float3 g = w.x * (s0.x * SandAxis(ma.y) + s0.y * SandAxis(ma.z))
             + w.y * (s1.x * SandAxis(me.y) + s1.y * SandAxis(me.z));
    return g / max(w.x + w.y, 1e-4);
}

// 凹凸を足した法線（物体の座標）。高さの体積の勾配から面に沿う成分だけを取り、法線を傾ける（surface gradient）。
// 地層の起伏は上下の向きだけに効くので、水平な面には効かない。岩肌の細かな凹凸は崖の面と転石で、遠くでは読まない
float3 SandstoneReliefNormal(float3 p, float3 n, float bedHeight, float4 lamina, half looseRock, float viewDistance)
{
    float face = 1.0 - smoothstep(0.52, 0.82, n.y);
    float u = (bedHeight - _RegionReliefStrata.x) * _RegionReliefStrata.y;
    float strata = SAMPLE_TEXTURE2D_GRAD(_RegionReliefStrataSlope, sampler_RegionReliefStrataSlope,
        float2(u, 0.5), float2(ddx(u), 0), float2(ddy(u), 0)).r;
    // 斜交葉理の明るい縞は、風化で少し張り出した稜（高さ 6 cm、幅は位相の ±0.2）。縞が画素より細くなると消す
    float side = frac(lamina.x + 0.5) - 0.5;
    float t = saturate(abs(side) / 0.2);
    float ridge = -0.06 * lamina.y * (6.0 * t * (1.0 - t) / 0.2) * sign(side)
                * (1.0 - smoothstep(0.15, 0.5, fwidth(lamina.x)));
    float3 g = (float3(0, strata, 0) + ridge * 0.93 * float3(lamina.z, 1.0, lamina.w))
             * (_RegionReliefStrata.z * face * (1.0 - looseRock));

    float3 q = p * _RegionReliefRock.x;
    float3 qdx = ddx(q);
    float3 qdy = ddy(q);
    float rock = max(face, looseRock) * (1.0 - smoothstep(_RegionReliefRock.z, _RegionReliefRock.w, viewDistance));
    [branch]
    if (rock > 0.0)
        g += SandRockGradient(q, qdx, qdy, n) * (_RegionReliefRock.y * rock);
    return normalize(n - (g - n * dot(n, g)));
}

#endif
