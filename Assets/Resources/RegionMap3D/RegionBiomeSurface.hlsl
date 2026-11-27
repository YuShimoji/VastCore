// バイオーム別 3D 地表・法線・粗さ・崖色の共通評価（RegionSurfaceMaterials.BindBiomeSurfaces がバインドする）。
// 砂岩（RegionSandstone.hlsl）以外のバイオーム（Grassland / Forest / Savanna / Taiga / Tundra / Coast 等）および
// Mesa=0 の地形に対して、遠景 _BaseMap の気候色調を保ちつつ近〜中景の PBR 細部・法線・粗さ・崖材質を適用する。
#ifndef REGION_BIOME_SURFACE_INCLUDED
#define REGION_BIOME_SURFACE_INCLUDED

TEXTURE2D(_BiomeGroundMap);
TEXTURE2D(_BiomeGroundNormal);
TEXTURE2D(_BiomeGroundRoughness);
TEXTURE2D(_BiomeSubsoilMap);
TEXTURE2D(_BiomeSubsoilNormal);
TEXTURE2D(_BiomeSubsoilRoughness);
TEXTURE2D(_BiomeCliffMap);
TEXTURE2D(_BiomeCliffNormal);
TEXTURE2D(_BiomeCliffRoughness);
SAMPLER(sampler_BiomeGroundMap);
#include "RegionGrasslandHabitat.hlsl"


struct BiomeSurfaceExtra
{
    half3 normalWS;
    half roughness;
    half specular;
    half occlusion;
};

float BiomeHash21(float2 p)
{
    float3 p3 = frac(p.xyx * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float BiomeNoise(float2 uv)
{
    float2 i = floor(uv);
    float2 f = frac(uv);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(
        lerp(BiomeHash21(i), BiomeHash21(i + float2(1, 0)), f.x),
        lerp(BiomeHash21(i + float2(0, 1)), BiomeHash21(i + 1.0), f.x),
        f.y);
}

half3 BiomeBlendWorldNormal(half3 geomNormal, half3 tangentNormal, half strength)
{
    half3 up = abs(geomNormal.y) < 0.99h ? half3(0, 1, 0) : half3(1, 0, 0);
    half3 tangent = normalize(cross(up, geomNormal));
    half3 bitangent = cross(geomNormal, tangent);
    half3 t = half3(tangentNormal.xy * strength, tangentNormal.z);
    return normalize(tangent * t.x + bitangent * t.y + geomNormal * t.z);
}

half3 EvaluateBiomeSurface(
    float3 positionWS,
    half3 normalWS,
    half rockMask,
    half3 baseAlbedoLinear,
    float4 biomeChannels,
    half tintAlpha,
    float viewDistance,
    out BiomeSurfaceExtra extra)
{
    extra.normalWS = normalWS;
    extra.roughness = _RegionBiomeTile.w > 0.05 ? _RegionBiomeTile.w : 0.88h;
    extra.specular = 0.0h;
    extra.occlusion = 1.0h;

    half detailFade = saturate(1.0h - (viewDistance - 45.0) / 235.0);
    half nearFade = saturate(1.0h - (viewDistance - 25.0) / 95.0);

    bool meadow = _GrasslandSurface.z > 0.5;
    bool continuous = meadow && _GrasslandSurface.w > 0.5;
    float2 geographic = positionWS.xz;
    if (meadow)
        geographic = TransformWorldToObject(positionWS).xz * float2(1.0, -1.0) + _GrasslandSurface.xy;
    float macro = BiomeNoise(geographic * 0.045);
    float mid = BiomeNoise(geographic * 0.19 + float2(5.2, 1.7));
    float micro = BiomeNoise(geographic * 1.45 - float2(2.3, 8.1));

    // x: 草地度合い, y: 湿り気, z: ローム/土露出, w: 重み
    half grassBias = saturate(biomeChannels.x * (0.75h + 0.35h * macro) * (1.0h - 0.65h * rockMask));
    half subsoilShare = saturate(biomeChannels.z * 0.65h + (1.0h - grassBias) * 0.45h + (1.0h - normalWS.y) * 0.55h);
    // 草原の地面は湿り気と広さの違う分布から、草のまとまりと土の露出を作る。
    half patch = smoothstep(0.30h, 0.72h, macro * 0.55h + mid * 0.45h + (biomeChannels.y - 0.5h) * 0.24h);
    subsoilShare = max(subsoilShare, (1.0h - patch) * _RegionBiomeGroundBlend.y);
    half turfShare = 1.0h - subsoilShare;
    half3 canopy = baseAlbedoLinear;
    half trailWear = 0.0h;
    half growth = 0.0h;
    half3 groundNormal = normalWS;
    if (meadow)
    {
        float3 field = continuous ? GrasslandCanopy(geographic, biomeChannels.y)
            : float3(GrasslandGrowth(geographic, biomeChannels.y), 0.0, 0.0);
        growth = field.x;
        trailWear = _GrasslandTrail.z > 0.5 ? GrasslandTrailWear(geographic - _GrasslandTrail.xy) : 0.0h;
        turfShare = lerp(0.49h, 0.96h, growth) * (1.0h - trailWear) * (1.0h - rockMask);
        half coverage = (1.0h - rockMask) * (1.0h - trailWear);
        if (continuous)
        {
            // Keep the relief of grass clumps where the blades become too small to read.
            float2 tuftEast = float2(0.44, 0.33);
            float2 tuftSouth = float2(-0.48, 0.64);
            float2 tuftUv = float2(dot(geographic, tuftEast), dot(geographic, tuftSouth)) + float2(16.7, 5.3);
            float footprint = max(length(ddx(tuftUv)), length(ddy(tuftUv)));
            float tuftWeight = smoothstep(28.0, 74.0, viewDistance) * (1.0 - smoothstep(0.24, 0.82, footprint));
            float2 canopySlope = field.yz * 1.65 * smoothstep(18.0, 120.0, viewDistance);
            half tuftShade = 0.0h;
            [branch]
            if (tuftWeight > 0.001)
            {
                float3 tuft = GrasslandNoiseAndSlope(tuftUv);
                canopySlope += (tuft.y * tuftEast + tuft.z * tuftSouth) * tuftWeight * lerp(0.11, 0.37, growth);
                tuftShade = tuftWeight * (1.0 - tuft.x) * (1.0 - tuft.x) * 0.44h;
            }
            groundNormal = normalize(normalWS + coverage * half3(-canopySlope.x, 0.0h, canopySlope.y));
            canopy = RegionDisplayToLinear(GrasslandCrownColor(growth)) * (0.81h + 0.29h * mid);
            half canopyFade = lerp(0.70h, 0.98h, smoothstep(22.0, 135.0, viewDistance));
            baseAlbedoLinear = lerp(baseAlbedoLinear, canopy, coverage * canopyFade);
            extra.normalWS = groundNormal;
            extra.occlusion = 1.0h - coverage * (lerp(0.09h, 0.36h, growth) + tuftShade);
        }
        else
        {
            canopy = RegionDisplayToLinear(lerp(half3(0.43h, 0.43h, 0.32h), half3(0.32h, 0.39h, 0.25h), growth));
            canopy *= 0.89h + 0.23h * mid;
            baseAlbedoLinear = lerp(baseAlbedoLinear, canopy, coverage * lerp(0.61h, 0.95h, smoothstep(38.0, 185.0, viewDistance)));
        }
    }

    half3 resultAlbedo = baseAlbedoLinear;
    half3 cliffTint = _RegionBiomeCliffColor.a > 0.01h
        ? _RegionBiomeCliffColor.rgb * (0.76h + 0.32h * tintAlpha) * (0.88h + 0.24h * mid)
        : baseAlbedoLinear;

    // 微分を分岐の外で計算
    float groundScale = _RegionBiomeTile.x > 0.001 ? _RegionBiomeTile.x : 0.40;
    float subsoilScale = _RegionBiomeTile.y > 0.001 ? _RegionBiomeTile.y : 0.45;
    float cliffScale = _RegionBiomeTile.z > 0.001 ? _RegionBiomeTile.z : 0.36;

    float2 gUv0 = geographic * groundScale;
    float2 gUv1 = geographic * (groundScale * 0.29) + float2(3.7, 1.9);
    float2 sUv0 = geographic * subsoilScale;
    float2 sUv1 = geographic * (subsoilScale * 0.31) + float2(6.1, 4.3);

    // 崖面用の簡易 biplanar UV（横軸 + 高さ）
    half3 absN = abs(normalWS);
    float2 cUvX = positionWS.zy * cliffScale;
    float2 cUvZ = positionWS.xy * cliffScale;
    half cliffProjWeight = absN.x / max(absN.x + absN.z, 0.001h);

    float2 gDx = ddx(gUv0), gDy = ddy(gUv0);
    float2 sDx = ddx(sUv0), sDy = ddy(sUv0);
    float2 cXDx = ddx(cUvX), cXDy = ddy(cUvX);
    float2 cZDx = ddx(cUvZ), cZDy = ddy(cUvZ);

    [branch]
    if (_RegionBiomeParams.x > 0.5 && detailFade > 0.001h)
    {
        half3 turf0 = RegionToLinear(SAMPLE_TEXTURE2D_GRAD(_BiomeGroundMap, sampler_BiomeGroundMap, gUv0, gDx, gDy).rgb);
        half3 turf1 = RegionToLinear(SAMPLE_TEXTURE2D_GRAD(_BiomeGroundMap, sampler_BiomeGroundMap, gUv1, gDx * 0.29, gDy * 0.29).rgb);
        half3 turfTex = lerp(turf0, turf1, 0.36h);

        half3 loam0 = RegionToLinear(SAMPLE_TEXTURE2D_GRAD(_BiomeSubsoilMap, sampler_BiomeGroundMap, sUv0, sDx, sDy).rgb);
        half3 loam1 = RegionToLinear(SAMPLE_TEXTURE2D_GRAD(_BiomeSubsoilMap, sampler_BiomeGroundMap, sUv1, sDx * 0.31, sDy * 0.31).rgb);
        half3 loamTex = lerp(loam0, loam1, 0.34h);

        half3 groundTex = lerp(loamTex, turfTex, turfShare);
        // 広域マップ（_BaseMap）の気候色調を保ちながら、近景のテクスチャ細部を乗せる
        half texLuma = max(dot(groundTex, half3(0.2126h, 0.7152h, 0.0722h)), 0.04h);
        half3 tintedGround = lerp(baseAlbedoLinear * (groundTex / texLuma) * 0.86h, groundTex * 0.92h, _RegionBiomeGroundBlend.x);
        tintedGround *= 0.90h + 0.18h * macro;
        if (meadow)
        {
            // Cooler, darker litter under the leaves. Worn soil catches light through the open tread.
            tintedGround *= lerp(half3(0.65h, 0.60h, 0.72h), 0.89h, trailWear);
            tintedGround = lerp(tintedGround, canopy, 0.39h * turfShare);
            if (continuous)
            {
                half understory = lerp(0.56h, 0.38h, growth);
                tintedGround = lerp(tintedGround * lerp(1.0h, understory, 1.0h - trailWear), canopy,
                    (1.0h - trailWear) * smoothstep(24.0, 145.0, viewDistance));
            }
        }

        half3 cliffX = RegionToLinear(SAMPLE_TEXTURE2D_GRAD(_BiomeCliffMap, sampler_BiomeGroundMap, cUvX, cXDx, cXDy).rgb);
        half3 cliffZ = RegionToLinear(SAMPLE_TEXTURE2D_GRAD(_BiomeCliffMap, sampler_BiomeGroundMap, cUvZ, cZDx, cZDy).rgb);
        half3 cliffTex = lerp(cliffZ, cliffX, cliffProjWeight);
        half cliffLuma = max(dot(cliffTex, half3(0.2126h, 0.7152h, 0.0722h)), 0.05h);
        half3 rockSurface = lerp(cliffTint * (cliffTex / cliffLuma), cliffTex * (0.78h + 0.22h * tintAlpha), 0.45h);

        half3 detailed = lerp(tintedGround, rockSurface, rockMask);
        resultAlbedo = lerp(baseAlbedoLinear, detailed, detailFade * saturate(_RegionBiomeParams.y));

        half3 turfN = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(_BiomeGroundNormal, sampler_BiomeGroundMap, gUv0, gDx, gDy), _RegionBiomeParams.z);
        half3 loamN = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(_BiomeSubsoilNormal, sampler_BiomeGroundMap, sUv0, sDx, sDy), _RegionBiomeParams.z);
        half3 cliffNX = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(_BiomeCliffNormal, sampler_BiomeGroundMap, cUvX, cXDx, cXDy), _RegionBiomeParams.z);
        half3 cliffNZ = UnpackNormalScale(SAMPLE_TEXTURE2D_GRAD(_BiomeCliffNormal, sampler_BiomeGroundMap, cUvZ, cZDx, cZDy), _RegionBiomeParams.z);
        half3 groundN = normalize(lerp(loamN, turfN, turfShare));
        half3 cliffN = normalize(lerp(cliffNZ, cliffNX, cliffProjWeight));
        half3 combinedN = normalize(lerp(groundN, cliffN, rockMask));

        extra.normalWS = normalize(lerp(groundNormal, BiomeBlendWorldNormal(groundNormal, combinedN, nearFade), detailFade));

        half turfR = SAMPLE_TEXTURE2D_GRAD(_BiomeGroundRoughness, sampler_BiomeGroundMap, gUv0, gDx, gDy).r;
        half loamR = SAMPLE_TEXTURE2D_GRAD(_BiomeSubsoilRoughness, sampler_BiomeGroundMap, sUv0, sDx, sDy).r;
        half cliffRX = SAMPLE_TEXTURE2D_GRAD(_BiomeCliffRoughness, sampler_BiomeGroundMap, cUvX, cXDx, cXDy).r;
        half cliffRZ = SAMPLE_TEXTURE2D_GRAD(_BiomeCliffRoughness, sampler_BiomeGroundMap, cUvZ, cZDx, cZDy).r;
        half rSample = lerp(lerp(loamR, turfR, turfShare), lerp(cliffRZ, cliffRX, cliffProjWeight), rockMask);
        extra.roughness = lerp(extra.roughness, clamp(rSample, 0.35h, 1.0h), nearFade);
        extra.specular = detailFade * _RegionBiomeParams.w;
    }
    else
    {
        half3 procGround = baseAlbedoLinear * (0.88h + 0.14h * macro + 0.10h * micro);
        resultAlbedo = lerp(procGround, cliffTint, rockMask);
    }

    return resultAlbedo;
}

#endif
