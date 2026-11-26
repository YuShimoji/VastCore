// 表面の写真素材（RegionSurfaceMaterials.cs が場面を作る時に束ねた配列）を読む。役の番号は RegionSurfaceRole と同じ
// （0 崖の岩肌、1 泥岩の帯、2 岩屑、3 砂と土、4 地被、5 転石）。置かれていない役は読まない。
// 座標は m（内側・外側の地形と転石に共通の座標）。
#ifndef REGION_SURFACE_MATERIALS_INCLUDED
#define REGION_SURFACE_MATERIALS_INCLUDED

#define REGION_SURFACE_ROLES 6

TEXTURE2D_ARRAY(_RegionSurfaceDetail);   // 明るさと色のむら（平均 0.5）
SAMPLER(sampler_RegionSurfaceDetail);
TEXTURE2D_ARRAY(_RegionSurfaceNormal);   // 法線（赤 x・緑 y、OpenGL 向き）
TEXTURE2D_ARRAY(_RegionSurfaceHRA);      // 赤 高さ（0.5 が平均の面）、緑 粗さ、青 AO
float4 _RegionSurfaceRoles[REGION_SURFACE_ROLES];  // x 配列の番号（-1 で無し）、y 1 / 横 1 枚の m、z 法線の強さ、w 粗さの既定
float4 _RegionSurfaceParams;                        // x 1 で有効、y 鏡面の強さ（0.04 の反射率に掛ける）
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

void RegionSurfaceRead1(float slice, float scale, int axis, float3 p, float3 pdx, float3 pdy, float strength, float nearFade,
    out half3 detail, out float3 gradient, out half3 hra)
{
    int u, v;
    RegionSurfaceFrame(axis, u, v);
    float2 uv = float2(p[u], p[v]) * scale;
    float2 dx = float2(pdx[u], pdx[v]) * scale;
    float2 dy = float2(pdy[u], pdy[v]) * scale;
    detail = SAMPLE_TEXTURE2D_ARRAY_GRAD(_RegionSurfaceDetail, sampler_RegionSurfaceDetail, uv, slice, dx, dy).rgb * 2.0;
    float2 nxy = SAMPLE_TEXTURE2D_ARRAY_GRAD(_RegionSurfaceNormal, sampler_RegionSurfaceDetail, uv, slice, dx, dy).rg * 2.0 - 1.0;
    float nz = sqrt(saturate(1.0 - dot(nxy, nxy)));
    float2 slope = -nxy / max(nz, 0.2) * strength;
    gradient = slope.x * RegionSurfaceAxis(u) + slope.y * RegionSurfaceAxis(v);
    hra = half3(0.5, 0.0, 1.0);
    [branch]
    if (nearFade > 0.0)
        hra = SAMPLE_TEXTURE2D_ARRAY_GRAD(_RegionSurfaceHRA, sampler_RegionSurfaceDetail, uv, slice, dx, dy).rgb;
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

#endif
