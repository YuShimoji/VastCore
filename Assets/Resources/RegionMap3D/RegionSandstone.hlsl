// Coordinates are metres in the common frame of the inner terrain, outer terrain and fallen blocks.
#ifndef REGION_SANDSTONE_INCLUDED
#define REGION_SANDSTONE_INCLUDED

#include "RegionSurfaceMaterials.hlsl"

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

// 乾いて割れたシルトの割れ目（ボロノイの境）。width は格子 1 つに対する割れ目の幅。plate は割れた板ごとの 0〜1。
// footprint は uv の 1 画素の幅（分岐の外で出したもの）。割れ目が画素より細くなると、平均の暗さへ薄れる
float SandCracks(float2 uv, float footprint, float width, out float plate)
{
    float2 id = floor(uv);
    float2 f = frac(uv);
    float d1 = 8.0, d2 = 8.0;
    float2 nearest = id;
    [unroll] for (int y = -1; y <= 1; y++)
    {
        [unroll] for (int x = -1; x <= 1; x++)
        {
            float2 o = float2(x, y);
            float2 c = o + 0.1 + 0.8 * float2(SandHash(id + o), SandHash(id + o + 19.7));
            float d = dot(c - f, c - f);
            if (d < d1) { d2 = d1; d1 = d; nearest = id + o; }
            else if (d < d2) d2 = d;
        }
    }
    plate = SandHash(nearest + 5.3);
    float edge = sqrt(d2) - sqrt(d1);
    float crack = 1.0 - smoothstep(width, width + max(footprint, 0.01), edge);
    return lerp(crack, 2.5 * width, smoothstep(0.08, 0.35, footprint));
}

// 水に転がされた丸い礫。xyz は礫の色、w は礫が覆う度合い（画素より小さくなると 0）。uv は東・北の向きのまま（北西の太陽の側を明るく）
float4 SandPebbles(float2 uv, float footprint)
{
    float2 id = floor(uv);
    float radius = 0.22 + 0.2 * SandHash(id + 41.7);
    float2 center = lerp(radius, 1.0 - radius, float2(SandHash(id + 2.9), SandHash(id - 11.3)));
    float2 offset = (frac(uv) - center) / radius;
    float r2 = dot(offset, offset);
    float mask = (1.0 - smoothstep(0.72, 1.0, r2)) * (1.0 - smoothstep(0.12, 0.5, footprint));
    float kind = SandHash(id + 8.1);
    float3 color = kind < 0.45 ? float3(0.60, 0.58, 0.55) : (kind < 0.8 ? float3(0.72, 0.49, 0.37) : float3(0.78, 0.70, 0.60));
    float dome = 1.0 + 0.18 * (offset.y - offset.x) * sqrt(saturate(1.0 - r2));
    return float4(color * (0.82 + 0.3 * SandHash(id - 3.3)) * dome, mask);
}

// 土砂の地図（RegionSedimentLayers が窓ごとに結び付けた画像）。x たまり（Deposit）、y 流路（SedimentFlow と Wear の大きい方）、
// z 崖から落ちた岩屑の錐（Scree）、w 大きい岩の割合（Coarse）。地図の無い所・層を切った時は 0（今の描き方のまま）
#if defined(REGION_SEDIMENT_MATERIAL)
TEXTURE2D(_SedimentMap);
SAMPLER(sampler_SedimentMap);
#endif
float4 _RegionSedimentParams;  // x 1 で層を出す

float4 SandSedimentMaps(float3 p)
{
#if defined(REGION_SEDIMENT_MATERIAL)
    if (_RegionSedimentParams.x > 0.5 && _SedimentRect.z > 0.0)
    {
        // 物体の座標は X 東・Z 北。地図は Core の座標（Z 南）で並ぶ
        float2 uv = float2(p.x - _SedimentRect.x, -p.z - _SedimentRect.y) * _SedimentRect.z + _SedimentRect.w;
        return SAMPLE_TEXTURE2D(_SedimentMap, sampler_SedimentMap, uv);
    }
#endif
    return 0.0;
}

// 色と一緒に出す、光を受ける向きのための値
struct SandstoneExtra
{
    float bedHeight;     // 帽子の岩の下端からの高さ（地層の柱を読む位置）
    float4 lamina;       // 斜交葉理の位相、縞が出る度合い、傾きの向き
    float3 gradient;     // 写真素材の凹凸（高さの体積の勾配 m/m、物体の座標）
    half occlusion;      // 写真素材の AO
    half rockMaterial;   // 岩肌の細かな凹凸（段1の仮の起伏）を写真素材に任せる度合い
    half roughness;      // 写真素材の粗さに、素材の重みを掛けた和（specular で割ると粗さ）
    half specular;       // 写真素材が受け持つ重み（鏡面を足す度合い）
};

// 写真素材を読む場所の値。微分は分岐の外で出しておく
struct SandMaterialContext
{
    float3 p;
    float3 pdx;
    float3 pdy;
    float3 n;
    float farFade;    // 1 で写真を読む、0 で読まない（遠く）
    float nearFade;   // 1 で高さ粗さAO も読む（近く）
};

// 役 role の写真素材を読み、重み weight で凹凸・AO・粗さを足して、明るさと色の比を返す。素材が無い・重みが小さい役は読まず 1 を返す。
// 遠くへ行くほど比は 1、凹凸は 0 に近づき、読まなくなる所で素材が無い時と同じになる
half3 SandRole(int role, float weight, SandMaterialContext c, inout SandstoneExtra extra)
{
    half3 detail = 1.0;
    float w = weight * c.farFade;
    [branch]
    if (w > 0.01 && RegionSurfaceHas(role))
    {
        RegionSurfaceSample s = RegionSurfaceRead(role, c.p, c.pdx, c.pdy, c.n, c.nearFade);
        detail = lerp(1.0, s.detail, c.farFade);
        extra.gradient += s.gradient * w;
        extra.occlusion -= (1.0 - s.occlusion) * w;
        extra.roughness += s.roughness * w;
        extra.specular += w;
    }
    return detail;
}

// 素材があれば 1（遠くでは 0 へ）。手続きの細かな粒を写真に置き換える度合い
float SandHas(int role, float farFade) { return RegionSurfaceHas(role) ? farFade : 0.0; }

// 他の 6 役のうち、重みが w より大きいものが 2 つ未満なら 1（重みの大きい 2 つだけ読む）
float SandTop2(float w, float4 others, float2 more)
{
    return dot((float4)(others > w), 1.0) + dot((float2)(more > w), 1.0) < 2.0 ? 1.0 : 0.0;
}

// Four surfaces: exposed bedrock, talus, sand/soil and patchy dry ground cover.
// surface = talus density, metres above the caprock contact, moisture for cover, dry-land coverage.
// 写真素材の置かれた役では、手続きの細かな粒（grains・pores・礫）を写真の明暗に置き換え、大きなむらと地層の色は手続きのまま残す。
// 写真は 1 画素で重みの大きい 2 役まで読み、カメラからの距離 viewDistance（m）で読む物を減らす（設計 3.3 節の距離の段）。
half3 SandstoneSurface(float3 p, float3 n, float4 surface, half tint, half looseRock, float viewDistance, out SandstoneExtra extra)
{
    float3 weights = n * n;
    weights *= weights;
    weights /= max(dot(weights, float3(1, 1, 1)), 0.001);
    float mottling = SandTriplanar(p * 0.22, weights);
    float grains = SandTriplanar(p * 9.7, weights);
    float broad = SandNoise(p.xz * 0.023);
    float broken = SandTriplanar(p * 0.065 + 3.5, weights);
    float mineral = 0.63 * mottling + 0.37 * grains;
    float coarseMineral = 0.63 * mottling + 0.37 * 0.5;
    SandMaterialContext c;
    c.p = p;
    c.pdx = ddx(p);
    c.pdy = ddy(p);
    c.n = n;
    c.farFade = RegionSurfaceFarFade(viewDistance);
    c.nearFade = RegionSurfaceNearFade(viewDistance);
    extra.bedHeight = 0.0;
    extra.lamina = 0.0;
    extra.gradient = 0.0;
    extra.occlusion = 1.0;
    extra.rockMaterial = 0.0;
    extra.roughness = 0.0;
    extra.specular = 0.0;
    if (looseRock > 0.5)
    {
        float hasLoose = SandHas(5, c.farFade);
        half3 detail = SandRole(5, 1.0, c, extra);
        extra.rockMaterial = hasLoose;
        float3 block = lerp(float3(0.56, 0.37, 0.28), float3(0.85, 0.62, 0.44), broken);
        return block * (0.79 + 0.43 * lerp(mineral, coarseMineral, hasLoose)) * (0.74 + 0.36 * tint) * detail;
    }

    float bedHeight = surface.y + 1.8 * (broad - 0.5) + 0.28 * (mottling - 0.5);
    extra.bedHeight = bedHeight;
    float bed = bedHeight * 0.12 + 0.78 * SandNoise(float2(bedHeight * 0.072, 5.4));
    float unit = SandNoise(float2(bedHeight * 0.28, 3.7));
    float3 rock = lerp(float3(0.64, 0.31, 0.20), float3(0.91, 0.64, 0.40), 0.62 * unit + 0.38 * broken);
    float mud = (1.0 - smoothstep(-6.0, 1.5, bedHeight)) * smoothstep(-38.0, -29.0, bedHeight);
    float3 mudstone = lerp(float3(0.42, 0.32, 0.35), float3(0.73, 0.58, 0.54), unit);
    rock = lerp(rock, mudstone, mud);

    // 土砂の地図（設計 6.2 節）。どの層を出すかだけを決め、色の模様は層ごとの手続きの模様と写真素材が持つ（流れの地図をそのまま色にしない）。
    // 主は崩れの錐と分級（錐の岩屑、大きい岩の多い所の礫）、従は水の溝（流路の礫）。水がためた所は細かい土砂
    float4 sed = SandSedimentMaps(p);
    float screeAmount = smoothstep(0.06, 0.55, sed.z);
    float coarse = sed.w;
    float washAmount = 0.85 * smoothstep(0.12, 0.65, sed.y);
    float depositAmount = smoothstep(0.08, 0.5, sed.x) * (1.0 - 0.8 * screeAmount);

    // 役の重み：崖の岩肌・泥岩の帯・岩屑・砂と土・地被・細かい土砂・流路の礫。写真素材のある役の中で重みの大きい 2 つだけを読む
    float rockFace = 1.0 - smoothstep(0.52, 0.82, n.y);
    float cover = smoothstep(0.60, 0.78, 0.56 * broad + 0.44 * mottling)
                * surface.z * smoothstep(0.78, 0.97, n.y);
    float talus = max(smoothstep(0.04, 0.82, surface.x), screeAmount);
    float driftSand = smoothstep(0.47, 0.80, 0.62 * mottling + 0.38 * broken);
    // 大きい岩の多い所は礫の間を砂が埋めず、斜面の裾の先へ転がり出た大きい岩も岩屑の層で描く
    float talusMix = max(talus * (1.0 - 0.56 * driftSand * (1.0 - coarse)), 0.6 * coarse * smoothstep(0.25, 0.6, coarse));
    float groundShare = 1.0 - rockFace;
    float wCliff = rockFace * (1.0 - mud);
    float wMud = rockFace * mud;
    float wWash = groundShare * washAmount;
    float wDeposit = groundShare * (1.0 - washAmount) * depositAmount;
    float rest = groundShare * (1.0 - washAmount) * (1.0 - depositAmount);
    float wTalus = rest * talusMix;
    float wSand = rest * (1.0 - talusMix) * (1.0 - cover);
    float wCrust = rest * (1.0 - talusMix) * cover;
    // 高さによる層の混合（UE の LB Height Blend と同じ考え）。近くだけで、礫の頭が砂の上に出て、間に砂が入る。
    // 砂と土は高さ 0 の基準の層。重みの小さい層の高さは重みに応じて弱め、無い層が高さだけで出てこないようにする
    [branch]
    if (_RegionSurfaceBlend.x > 0.0 && c.nearFade > 0.0 && RegionSurfaceEnabled())
    {
        float4 wr = float4(wCliff, wMud, wTalus, wCrust);
        float2 wr2 = float2(wDeposit, wWash);
        float4 hr = 0.5;
        float2 hr2 = 0.5;
        [branch] if (wCliff > 0.01) hr.x = RegionSurfaceHeight(0, p, c.pdx, c.pdy, n);
        [branch] if (wMud > 0.01) hr.y = RegionSurfaceHeight(1, p, c.pdx, c.pdy, n);
        [branch] if (wTalus > 0.01) hr.z = RegionSurfaceHeight(2, p, c.pdx, c.pdy, n);
        [branch] if (wCrust > 0.01) hr.w = RegionSurfaceHeight(4, p, c.pdx, c.pdy, n);
        [branch] if (wDeposit > 0.01) hr2.x = RegionSurfaceHeight(6, p, c.pdx, c.pdy, n);
        [branch] if (wWash > 0.01) hr2.y = RegionSurfaceHeight(7, p, c.pdx, c.pdy, n);
        float4 sr = wr + hr * _RegionSurfaceBlendHeights * smoothstep(0.0, 0.25, wr);
        float2 sr2 = wr2 + hr2 * _RegionSurfaceBlendHeights2.xy * smoothstep(0.0, 0.25, wr2);
        float top = max(max(max(sr.x, sr.y), max(sr.z, sr.w)), max(wSand, max(sr2.x, sr2.y)));
        float depth = _RegionSurfaceBlend.x;
        float4 br = max(0.0, sr - (top - depth));
        // 重みの無い土砂の層は、境の深さだけで出てこないようにする
        float2 br2 = max(0.0, sr2 - (top - depth)) * (float2)(wr2 > 0.0);
        float bs = max(0.0, wSand - (top - depth));
        float total = dot(br, 1.0) + bs + br2.x + br2.y;
        float k = c.nearFade * c.farFade;
        br = lerp(wr, br / total, k);
        br2 = lerp(wr2, br2 / total, k);
        bs = lerp(wSand, bs / total, k);
        wCliff = br.x; wMud = br.y; wTalus = br.z; wCrust = br.w; wSand = bs; wDeposit = br2.x; wWash = br2.y;
        rockFace = wCliff + wMud;
        mud = rockFace > 1e-4 ? wMud / rockFace : mud;
        groundShare = 1.0 - rockFace;
        float restShare = groundShare - wDeposit - wWash;
        talusMix = restShare > 1e-4 ? wTalus / restShare : talusMix;
        cover = (wSand + wCrust) > 1e-4 ? wCrust / (wSand + wCrust) : cover;
    }
    // 流路の礫と細かい土砂が地面に占める割合（色を重ねる順は、今の地面 → 細かい土砂 → 流路の礫）
    float washShare = groundShare > 1e-4 ? wWash / groundShare : 0.0;
    float depositShare = groundShare > 1e-4 ? wDeposit / groundShare : 0.0;
    float depositInRest = depositShare / max(1.0 - washShare, 1e-4);
    float hasCliff = SandHas(0, c.farFade), hasMud = SandHas(1, c.farFade), hasTalus = SandHas(2, c.farFade);
    float hasSand = SandHas(3, c.farFade), hasCrust = SandHas(4, c.farFade);
    float hasDeposit = SandHas(6, c.farFade), hasWash = SandHas(7, c.farFade);
    float eCliff = wCliff * hasCliff, eMud = wMud * hasMud, eTalus = wTalus * hasTalus, eSand = wSand * hasSand, eCrust = wCrust * hasCrust;
    float eDeposit = wDeposit * hasDeposit, eWash = wWash * hasWash;
    float2 eSediment = float2(eDeposit, eWash);

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
    extra.lamina = float4(lamina, smoothstep(0.03, 0.22, frac(bed)) * (1.0 - smoothstep(0.68, 0.98, frac(bed))) * smoothstep(0.20, 0.68, broken), dip);
    float seam = (0.06 + 0.17 * broken) * SandSeam(bed, 0.018 + 0.020 * mottling);
    float pores = SandTriplanar(p * 1.82 + mottling, weights);
    float rockMaterial = lerp(hasCliff, hasMud, mud);
    rock *= 0.76 + 0.40 * lerp(mineral, coarseMineral, rockMaterial) + 0.13 * (pores - 0.5) * (1.0 - rockMaterial) - seam;

    // Runoff follows gravity on each cliff face. Its extent breaks up vertically and between outcrops.
    float3 streakPos = p * float3(0.52, 0.027, 0.52);
    streakPos.xz += 0.34 * mottling;
    float streak = SandTriplanar(streakPos, weights);
    float patches = SandTriplanar(p * float3(0.085, 0.033, 0.085) + 21.3, weights);
    float varnish = smoothstep(0.54, 0.79, streak) * smoothstep(0.37, 0.67, patches)
                  * (1.0 - smoothstep(0.38, 0.73, n.y));
    rock = lerp(rock, float3(0.27, 0.20, 0.18) * (0.78 + 0.50 * mineral), varnish * 0.88);
    half3 cliffDetail = SandRole(0, wCliff * SandTop2(eCliff, float4(eMud, eTalus, eSand, eCrust), eSediment), c, extra);
    half3 mudDetail = SandRole(1, wMud * SandTop2(eMud, float4(eCliff, eTalus, eSand, eCrust), eSediment), c, extra);
    rock *= lerp(cliffDetail, mudDetail, mud);
    extra.rockMaterial = rockFace * rockMaterial;

    float sandPatch = 0.52 * broad + 0.30 * mottling + 0.18 * grains;
    float3 soil = lerp(float3(0.56, 0.40, 0.29), float3(0.87, 0.71, 0.51), sandPatch);
    half3 sandDetail = SandRole(3, wSand * SandTop2(eSand, float4(eCliff, eMud, eTalus, eCrust), eSediment), c, extra);
    half3 crustDetail = SandRole(4, wCrust * SandTop2(eCrust, float4(eCliff, eMud, eTalus, eSand), eSediment), c, extra);
    half3 talusDetail = SandRole(2, wTalus * SandTop2(eTalus, float4(eCliff, eMud, eSand, eCrust), eSediment), c, extra);
    soil = lerp(soil * sandDetail, float3(0.43, 0.41, 0.28) * (0.65 + 0.70 * lerp(grains, 0.5, hasCrust)) * crustDetail, cover);
    float groundMaterial = lerp(lerp(hasSand, hasCrust, cover), hasTalus, talusMix);
    float2 ground = float2(0.8 * p.x + 0.6 * p.z, -0.6 * p.x + 0.8 * p.z);
    float2 drift = float2(mottling, pores);
    float gravel = SandChips(ground * 4.6 + drift) + 0.75 * SandChips(p.xz * 13.3 - drift);
    // 大きい岩の多い所（分級の地図 Coarse）は、一辺 0.3〜0.7 m の角ばった塊を足し、塊の間の影で暗くする
    gravel += 1.3 * coarse * SandChips(ground * 1.4 - 2.0 * drift);
    // Talus is broken from the orange cliff above, so it stays warmer than the mudstone it covers.
    float3 rubble = lerp(float3(0.58, 0.42, 0.33), float3(0.80, 0.63, 0.48), sandPatch);
    rubble = lerp(rubble, mudstone, mud * 0.45) * talusDetail * (1.0 - 0.10 * coarse);
    float3 groundColor = lerp(soil, rubble, talusMix);
    groundColor *= lerp(0.82 + 0.24 * grains + 0.14 * pores + (0.55 + talus) * gravel, 1.0, groundMaterial);

    // 細かい土砂（乾いて割れたシルト）と流路の礫。土砂の地図のある所だけ描く。細かい模様の 1 画素の幅は分岐の外で出す
    float footprint = max(fwidth(p.x), fwidth(p.z));
    [branch]
    if (depositShare + washShare > 0.001)
    {
        half3 depositDetail = SandRole(6, wDeposit * SandTop2(eDeposit, float4(eCliff, eMud, eTalus, eSand), float2(eCrust, eWash)), c, extra);
        half3 washDetail = SandRole(7, wWash * SandTop2(eWash, float4(eCliff, eMud, eTalus, eSand), float2(eCrust, eDeposit)), c, extra);
        // シルトは砂より淡く細かい。厚くたまった所だけ、乾いて割れた板の境が出る（写真素材のある時は写真に任せる）
        float plate;
        float crack = SandCracks(p.xz * 1.7 + 0.25 * drift, footprint * 1.7, 0.06, plate) * smoothstep(0.25, 0.75, sed.x);
        // 色は周りの砂と同じ赤みを保ち、少しだけ淡くする（灰白にすると、離れた所で雪や塗り残しに見える）
        float3 silt = lerp(float3(0.62, 0.47, 0.37), float3(0.81, 0.66, 0.51), 0.5 * broad + 0.3 * mottling + 0.2 * plate);
        silt *= lerp((0.94 + 0.08 * grains) * (1.0 - 0.38 * crack), 1.0, hasDeposit) * depositDetail;
        // 流路の礫は、水に転がされた丸い礫と粗い砂。色は崖の赤だけでなく、灰と白の礫が混ざる。大きい岩の多い所は大きい礫も
        // 礫の間の粗い砂は岩屑と同じ赤み。礫が画素より小さくなる中景からは、岩屑と同じ角ばった塊の影でむらを残す
        float3 washBase = lerp(float3(0.53, 0.41, 0.33), float3(0.74, 0.60, 0.48), sandPatch) * (0.9 + 0.2 * grains);
        float4 pebble = SandPebbles(p.xz * 7.0 + drift, footprint * 7.0);
        float4 cobble = SandPebbles(p.xz * 2.3 - drift, footprint * 2.3);
        float3 washMix = lerp(washBase, pebble.rgb, 0.85 * pebble.w);
        washMix = lerp(washMix, cobble.rgb, 0.85 * cobble.w * (0.3 + 0.7 * coarse));
        washMix *= 0.8 + 0.14 * pores + 1.05 * gravel;
        float3 washColor = lerp(washMix, washBase, hasWash) * washDetail;
        groundColor = lerp(lerp(groundColor, silt, depositInRest), washColor, washShare);
    }
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
// 写真素材の凹凸（extra.gradient）に段1の自動の凹凸を足す。地層の起伏は上下の向きだけに効くので、水平な面には効かない。
// 岩肌の細かな凹凸（仮）は崖の面と転石で、写真素材のある役と遠くでは読まない
float3 SandstoneReliefNormal(float3 p, float3 n, SandstoneExtra extra, half looseRock, float viewDistance)
{
    float3 g = extra.gradient;
    // 微分は分岐の外で出す
    float u = (extra.bedHeight - _RegionReliefStrata.x) * _RegionReliefStrata.y;
    float2 du = float2(ddx(u), ddy(u));
    float laminaWidth = fwidth(extra.lamina.x);
    float3 q = p * _RegionReliefRock.x;
    float3 qdx = ddx(q);
    float3 qdy = ddy(q);
    [branch]
    if (RegionReliefEnabled())
    {
        float face = 1.0 - smoothstep(0.52, 0.82, n.y);
        float strata = SAMPLE_TEXTURE2D_GRAD(_RegionReliefStrataSlope, sampler_RegionReliefStrataSlope,
            float2(u, 0.5), float2(du.x, 0), float2(du.y, 0)).r;
        // 斜交葉理の明るい縞は、風化で少し張り出した稜（高さ 6 cm、幅は位相の ±0.2）。縞が画素より細くなると消す
        float side = frac(extra.lamina.x + 0.5) - 0.5;
        float t = saturate(abs(side) / 0.2);
        float ridge = -0.06 * extra.lamina.y * (6.0 * t * (1.0 - t) / 0.2) * sign(side)
                    * (1.0 - smoothstep(0.15, 0.5, laminaWidth));
        g += (float3(0, strata, 0) + ridge * 0.93 * float3(extra.lamina.z, 1.0, extra.lamina.w))
           * (_RegionReliefStrata.z * face * (1.0 - looseRock));

        float rock = max(face, looseRock) * (1.0 - extra.rockMaterial)
                   * (1.0 - smoothstep(_RegionReliefRock.z, _RegionReliefRock.w, viewDistance));
        [branch]
        if (rock > 0.0)
            g += SandRockGradient(q, qdx, qdy, n) * (_RegionReliefRock.y * rock);
    }
    return normalize(n - (g - n * dot(n, g)));
}

#endif
