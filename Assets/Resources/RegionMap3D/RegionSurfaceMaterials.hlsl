// 表面の写真素材（RegionSurfaceMaterials.cs が場面を作る時に束ねた配列）を読む。役の番号は RegionSurfaceRole と同じ
// （0 崖の岩肌、1 泥岩の帯、2 岩屑、3 砂と土、4 地被、5 転石、6 細かい土砂、7 流路の礫）。置かれていない役は読まない。
// 座標は m（内側・外側の地形と転石に共通の座標）。
#ifndef REGION_SURFACE_MATERIALS_INCLUDED
#define REGION_SURFACE_MATERIALS_INCLUDED

#define REGION_SURFACE_ROLES 8

TEXTURE2D_ARRAY(_RegionSurfaceDetail);   // 明るさと色のむら（平均 0.5）
SAMPLER(sampler_RegionSurfaceDetail);
TEXTURE2D_ARRAY(_RegionSurfaceNormal);   // 法線（赤 x・緑 y、OpenGL 向き）
TEXTURE2D_ARRAY(_RegionSurfaceHRA);      // 赤 高さ（0.5 が平均の面）、緑 粗さ、青 AO
float4 _RegionSurfaceRoles[REGION_SURFACE_ROLES];  // x 配列の番号（-1 で無し）、y 1 / 横 1 枚の m、z 法線の強さ、w 粗さの既定
float4 _RegionSurfaceParams;                        // x 1 で有効、y 鏡面の強さ（0.04 の反射率に掛ける）、z 1 で繰り返しを崩す
float4 _RegionSurfaceBoost;  // x 写真の明暗の強さ（比を x 倍に広げる。0 以下は 1、既定 2）、y 写真の平均の色を混ぜる割合（既定 0.5）
float4 _RegionSurfaceTints[REGION_SURFACE_ROLES];  // 役ごとの写真の平均の色（Linear）。a は混ぜる割合に掛ける（記録の無い役は 0）。y が 0 の時は使わない
float4 _RegionSurfaceTiers;  // 距離の段（m）: x〜y で高さ粗さAO を読まなくなる（近）、z〜w で写真を読まなくなる（中、その先は遠）

struct RegionSurfaceSample
{
    half3 detail;      // 平均 1 の明るさと色の比
    float3 gradient;   // 高さの体積の勾配（m/m、物体の座標）
    half roughness;
    half occlusion;
};

bool RegionSurfaceEnabled() { return _RegionSurfaceParams.x > 0.5; }

float RegionSurfaceNearFade(float viewDistance) { return 1.0 - smoothstep(_RegionSurfaceTiers.x, _RegionSurfaceTiers.y, viewDistance); }

float RegionSurfaceFarFade(float viewDistance) { return 1.0 - smoothstep(_RegionSurfaceTiers.z, _RegionSurfaceTiers.w, viewDistance); }

bool RegionSurfaceHas(int role) { return RegionSurfaceEnabled() && _RegionSurfaceRoles[role].x >= 0.0; }

float3 RegionSurfaceAxis(int axis) { return axis == 0 ? float3(1, 0, 0) : (axis == 1 ? float3(0, 1, 0) : float3(0, 0, 1)); }

// 投影の向きごとの、画像の横（u）と縦（v）の軸。横向きの面は画像の上を空へ向ける（地層や割れ目の向きを保つ）
void RegionSurfaceFrame(int axis, out int u, out int v)
{
    u = axis == 0 ? 2 : 0;
    v = axis == 1 ? 2 : 1;
}

bool RegionSurfaceTileBreak() { return _RegionSurfaceParams.z > 0.5; }

float RegionSurfaceHash(float2 c)
{
    float3 p3 = frac(c.xyx * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

// 横 1 枚の繰り返しを崩す（設計 3.3 節）。横 4 枚ほどの大きさで変わるむらで、画像を読む位置を 8 通りにずらし、
// 隣り合う 2 通りを混ぜる（ずらすだけなので法線の向きはそのまま。微分は元の uv のものを使うので mip は跳ねない）。
// 混ぜる帯は段の 4 割だけにし、残りは片方だけを読む（重さの予算、2026-11-27 の計測）。
// 高さによる層の混合（RegionSurfaceHeight）も同じずらしで読む
void RegionSurfaceTileOffsets(float2 uv, float slice, out float2 offA, out float2 offB, out float t)
{
    float2 x = uv * 0.23 + slice * 17.0;
    float2 i = floor(x), f = frac(x);
    f = f * f * (3.0 - 2.0 * f);
    float n = lerp(lerp(RegionSurfaceHash(i), RegionSurfaceHash(i + float2(1, 0)), f.x),
                   lerp(RegionSurfaceHash(i + float2(0, 1)), RegionSurfaceHash(i + float2(1, 1)), f.x), f.y);
    float k = n * 8.0;
    float ia = floor(k);
    offA = sin(float2(3.0, 7.0) * ia);
    offB = sin(float2(3.0, 7.0) * (ia + 1.0));
    t = smoothstep(0.3, 0.7, k - ia);
}

void RegionSurfaceReadAt(float slice, float2 uv, float2 dx, float2 dy, float nearFade, out half3 detail, out float2 nxy, out half3 hra)
{
    detail = SAMPLE_TEXTURE2D_ARRAY_GRAD(_RegionSurfaceDetail, sampler_RegionSurfaceDetail, uv, slice, dx, dy).rgb * 2.0;
    nxy = SAMPLE_TEXTURE2D_ARRAY_GRAD(_RegionSurfaceNormal, sampler_RegionSurfaceDetail, uv, slice, dx, dy).rg * 2.0 - 1.0;
    hra = half3(0.5, 0.0, 1.0);
    [branch]
    if (nearFade > 0.0)
        hra = SAMPLE_TEXTURE2D_ARRAY_GRAD(_RegionSurfaceHRA, sampler_RegionSurfaceDetail, uv, slice, dx, dy).rgb;
}

void RegionSurfaceRead1(float slice, float scale, int axis, float3 p, float3 pdx, float3 pdy, float strength, float nearFade,
    out half3 detail, out float3 gradient, out half3 hra)
{
    int u, v;
    RegionSurfaceFrame(axis, u, v);
    float2 uv = float2(p[u], p[v]) * scale;
    float2 dx = float2(pdx[u], pdx[v]) * scale;
    float2 dy = float2(pdy[u], pdy[v]) * scale;
    float2 nxy;
    [branch]
    if (RegionSurfaceTileBreak())
    {
        float2 offA, offB;
        float t;
        RegionSurfaceTileOffsets(uv, slice, offA, offB, t);
        RegionSurfaceReadAt(slice, uv + (t < 1.0 ? offA : offB), dx, dy, nearFade, detail, nxy, hra);
        [branch]
        if (t > 0.0 && t < 1.0)
        {
            half3 dB, hB;
            float2 nB;
            RegionSurfaceReadAt(slice, uv + offB, dx, dy, nearFade, dB, nB, hB);
            detail = lerp(detail, dB, t);
            nxy = lerp(nxy, nB, t);
            hra = lerp(hra, hB, t);
        }
    }
    else
        RegionSurfaceReadAt(slice, uv, dx, dy, nearFade, detail, nxy, hra);
    float nz = sqrt(saturate(1.0 - dot(nxy, nxy)));
    float2 slope = -nxy / max(nz, 0.2) * strength;
    gradient = slope.x * RegionSurfaceAxis(u) + slope.y * RegionSurfaceAxis(v);
}

// 1 つの役の素材を biplanar（向きの重みの大きい 2 面）で読む。uv の微分は分岐の外で出したものを受け取る。
// 高さ粗さAO は近く（nearFade > 0）だけ読み、その先は粗さを役の既定、AO を 1 にする
RegionSurfaceSample RegionSurfaceRead(int role, float3 p, float3 pdx, float3 pdy, float3 n, float nearFade)
{
    float4 r = _RegionSurfaceRoles[role];
    float3 a = abs(n);
    int ma = (a.x > a.y && a.x > a.z) ? 0 : ((a.y > a.z) ? 1 : 2);
    int mi = (a.x < a.y && a.x < a.z) ? 0 : ((a.y < a.z) ? 1 : 2);
    if (mi == ma) mi = (ma + 1) % 3;
    int me = 3 - mi - ma;
    half3 d0, d1, h0, h1;
    float3 g0, g1;
    RegionSurfaceRead1(r.x, r.y, ma, p, pdx, pdy, r.z, nearFade, d0, g0, h0);
    RegionSurfaceRead1(r.x, r.y, me, p, pdx, pdy, r.z, nearFade, d1, g1, h1);
    float2 w = saturate((float2(a[ma], a[me]) - 0.5773) / (1.0 - 0.5773));
    // 3 軸が同じ重みに近い向き（斜め 45° の角）では両方 0 になるので、大きい方だけにする
    w = (w.x + w.y) > 1e-4 ? w / (w.x + w.y) : float2(1.0, 0.0);
    RegionSurfaceSample s;
    s.detail = d0 * w.x + d1 * w.y;
    s.gradient = g0 * w.x + g1 * w.y;
    half3 hra = h0 * w.x + h1 * w.y;
    s.roughness = lerp(r.w, hra.g, nearFade);
    s.occlusion = lerp(1.0, hra.b, nearFade);
    return s;
}

// 高さによる層の混合（設計 6.2 節）。重み 1 を幅 BlendSpan（m）とみなした単位で、
// _RegionSurfaceBlend.x 境の深さ（0 で切）、_RegionSurfaceBlendHeights 崖・泥岩・岩屑・地被の高さの画像の 0〜1 の幅
float4 _RegionSurfaceBlend;
float4 _RegionSurfaceBlendHeights;
float4 _RegionSurfaceBlendHeights2;  // x 細かい土砂、y 流路の礫

// 役 role の高さ（0〜1）だけを biplanar で読む。素材の無い役は 0.5
float RegionSurfaceHeight(int role, float3 p, float3 pdx, float3 pdy, float3 n)
{
    if (!RegionSurfaceHas(role)) return 0.5;
    float4 r = _RegionSurfaceRoles[role];
    float3 a = abs(n);
    int ma = (a.x > a.y && a.x > a.z) ? 0 : ((a.y > a.z) ? 1 : 2);
    int mi = (a.x < a.y && a.x < a.z) ? 0 : ((a.y < a.z) ? 1 : 2);
    if (mi == ma) mi = (ma + 1) % 3;
    int me = 3 - mi - ma;
    float h[2];
    int axes[2] = { ma, me };
    [unroll]
    for (int i = 0; i < 2; i++)
    {
        int u, v;
        RegionSurfaceFrame(axes[i], u, v);
        float2 uv = float2(p[u], p[v]) * r.y;
        float2 dx = float2(pdx[u], pdx[v]) * r.y, dy = float2(pdy[u], pdy[v]) * r.y;
        [branch]
        if (RegionSurfaceTileBreak())
        {
            float2 offA, offB;
            float t;
            RegionSurfaceTileOffsets(uv, r.x, offA, offB, t);
            h[i] = SAMPLE_TEXTURE2D_ARRAY_GRAD(_RegionSurfaceHRA, sampler_RegionSurfaceDetail, uv + (t < 1.0 ? offA : offB), r.x, dx, dy).r;
            [branch]
            if (t > 0.0 && t < 1.0)
                h[i] = lerp(h[i], SAMPLE_TEXTURE2D_ARRAY_GRAD(_RegionSurfaceHRA, sampler_RegionSurfaceDetail, uv + offB, r.x, dx, dy).r, t);
        }
        else
            h[i] = SAMPLE_TEXTURE2D_ARRAY_GRAD(_RegionSurfaceHRA, sampler_RegionSurfaceDetail, uv, r.x, dx, dy).r;
    }
    float2 w = saturate((float2(a[ma], a[me]) - 0.5773) / (1.0 - 0.5773));
    w = (w.x + w.y) > 1e-4 ? w / (w.x + w.y) : float2(1.0, 0.0);
    return h[0] * w.x + h[1] * w.y;
}

#endif
