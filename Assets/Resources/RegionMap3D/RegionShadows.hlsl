// 地形の遠くまで届く影を、描画のたびに GPU で計算する。
// 影を落とす高さの格子（内側 1 km と外側 6 km。RegionTerrainShadows.cs が渡す）を、点から太陽の方へたどり、
// 地形より下を通ったら影にする。縁は遮る物から離れるほどぼける（頂点に持たせていた日当たり BakeSun と同じ式）。
// 場面の座標は Unity の (x, -z)。格子が渡されていない時（_RegionShadowParams.w = 0）は頂点の値を使う。
#ifndef REGION_SHADOWS_INCLUDED
#define REGION_SHADOWS_INCLUDED

TEXTURE2D(_RegionShadowInner);
SAMPLER(sampler_RegionShadowInner);
TEXTURE2D(_RegionShadowOuter);
SAMPLER(sampler_RegionShadowOuter);
float4 _RegionShadowInnerRect;  // 最初の頂点の x, z（場面の座標）、格子の間隔（m）、一辺の頂点数
float4 _RegionShadowOuterRect;
float4 _RegionShadowParams;     // x: 影を落とす面の最高点（m）、y: 一歩の伸び率、z: 最大の歩数、w: 1 で有効

bool RegionShadowsEnabled() { return _RegionShadowParams.w > 0.5; }

float RegionShadowSample(TEXTURE2D_PARAM(grid, gridSampler), float4 rect, float2 c)
{
    float2 uv = ((c - rect.xy) / rect.z + 0.5) / rect.w;
    return SAMPLE_TEXTURE2D_LOD(grid, gridSampler, uv, 0).r;
}

// 日が当たる度合い（0〜1）。startAlong は、太陽の方へずらしてからたどり始める距離（m）。転石が自分の影に入らないように使う
half RegionSunVisibility(float3 positionWS, float3 normalWS, float startAlong)
{
    float3 toSun = _MainLightPosition.xyz;
    float3 p = positionWS + normalWS * 0.35 + toSun * startAlong;
    float2 c = float2(p.x, -p.z);
    float2 dir = float2(toSun.x, -toSun.z);
    float flat = max(length(dir), 1e-4);
    dir /= flat;
    float rise = toSun.y / flat;
    if (rise <= 0.0) return 0.0;

    float top = _RegionShadowParams.x;
    float2 innerMin = _RegionShadowInnerRect.xy;
    float2 innerMax = innerMin + _RegionShadowInnerRect.z * (_RegionShadowInnerRect.w - 1.0);
    float2 outerMin = _RegionShadowOuterRect.xy;
    float2 outerMax = outerMin + _RegionShadowOuterRect.z * (_RegionShadowOuterRect.w - 1.0);
    float visible = 1.0;
    float s = 1.2 * _RegionShadowInnerRect.z;
    int steps = (int)_RegionShadowParams.z;
    [loop]
    for (int i = 0; i < steps; i++)
    {
        float ray = p.y + s * rise;
        if (ray > top) break;
        float2 q = c + dir * s;
        float height, cell;
        if (all(q >= innerMin) && all(q <= innerMax))
        {
            height = RegionShadowSample(TEXTURE2D_ARGS(_RegionShadowInner, sampler_RegionShadowInner), _RegionShadowInnerRect, q);
            cell = _RegionShadowInnerRect.z;
        }
        else if (all(q >= outerMin) && all(q <= outerMax))
        {
            height = RegionShadowSample(TEXTURE2D_ARGS(_RegionShadowOuter, sampler_RegionShadowOuter), _RegionShadowOuterRect, q);
            cell = _RegionShadowOuterRect.z;
        }
        else break;
        visible = min(visible, (ray - height) / (0.03 * s + 0.3));
        if (visible <= 0.0) break;
        s += max(cell, s * _RegionShadowParams.y);
    }
    return saturate(visible);
}

#endif
