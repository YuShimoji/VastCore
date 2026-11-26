// 地域の 3D 表示の「光と空気」。地形・植生・空のシェーダーが共通に使う。
// プロジェクトはガンマ色空間なので、光の計算は線形の値で行い、最後に sRGB へ戻して書く（URP の後処理は sRGB→線形に戻してから色調を掛ける）。
// 値は RegionAtmosphere.cs が Shader.SetGlobal* で渡す。渡されていない時（0）は霞なし・照り返しなし。
#ifndef REGION_ATMOSPHERE_INCLUDED
#define REGION_ATMOSPHERE_INCLUDED

float4 _RegionFogColor;        // rgb: 太陽と反対の側の霞の色（線形）
float4 _RegionFogSunColor;     // rgb: 太陽の側の霞の色（線形）。w: 太陽の側へ寄る鋭さ
float4 _RegionFogParams;       // x: 濃さ（1/m）、y: 濃さが 1/e になる高さ（m）、z: 霞の基準の高さ（m）、w: 霞の上限（0〜1）
float4 _RegionBounceColor;     // rgb: 日の当たる地面と岩からの照り返し（線形）。w: 未使用
float4 _RegionShadowTint;      // rgb: 影の中だけ空の光に掛ける色（1 で変えない）

half3 RegionToLinear(half3 c) { return SRGBToLinear(c); }

// 霞の色。太陽の方を見るほど明るく暖かい（前方散乱）
half3 RegionFogColor(float3 viewDir, float3 sunDir)
{
    half toward = saturate(dot(viewDir, sunDir));
    half t = pow(toward, max(_RegionFogSunColor.w, 1.0));
    return lerp(_RegionFogColor.rgb, _RegionFogSunColor.rgb, t);
}

// 空気が地面の色を残す割合（1 で霞なし）。高い所ほど空気が薄い（カメラと点の高さの平均で近似）
half RegionTransmittance(float3 positionWS)
{
    float3 ray = positionWS - _WorldSpaceCameraPos;
    float dist = length(ray);
    float meanHeight = 0.5 * (positionWS.y + _WorldSpaceCameraPos.y) - _RegionFogParams.z;
    float falloff = max(_RegionFogParams.y, 1.0);
    float density = _RegionFogParams.x * exp(-max(meanHeight, 0.0) / falloff);
    half t = exp(-density * dist);
    return max(t, 1.0 - _RegionFogParams.w);
}

half3 RegionApplyAir(half3 colorLinear, float3 positionWS)
{
    float3 view = normalize(positionWS - _WorldSpaceCameraPos);
    half t = RegionTransmittance(positionWS);
    return lerp(RegionFogColor(view, _MainLightPosition.xyz), colorLinear, t);
}

// 書き出し。HDR の線形の値を sRGB に符号化する（1 を超える値もそのまま残す）
half4 RegionOutput(half3 colorLinear, half alpha)
{
    return half4(LinearToSRGB(max(colorLinear, 0.0)), alpha);
}

#endif
