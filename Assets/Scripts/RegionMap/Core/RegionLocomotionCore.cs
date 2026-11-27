using System;

namespace Vastcore.RegionMap
{
    /// <summary>オープンワールド地上モードでの移動形態。</summary>
    public enum LocomotionMode : byte
    {
        OnFoot = 0,
        Vehicle = 1,
    }

    /// <summary>
    /// RegionTraversal の傾斜・水域区分に基づき、徒歩および簡易ビークルの通行判定・減速係数を算出する。
    /// UnityEngine 非依存。
    /// </summary>
    public static class RegionLocomotionMath
    {
        public static RegionGround ClassifyGround(float heightM, float lakeDepthM, float waterCoverage,
            float slope, RegionTraversalSpec spec = null)
        {
            if (spec == null) spec = new RegionTraversalSpec();
            if (heightM <= 0f || lakeDepthM > 0f) return RegionGround.Water;
            if (waterCoverage > 0.5f) return RegionGround.Ford;
            if (slope > spec.CliffSlope) return RegionGround.Cliff;
            if (slope > spec.SteepSlope) return RegionGround.Scramble;
            if (slope > spec.WalkSlope) return RegionGround.Steep;
            return RegionGround.Walk;
        }

        /// <summary>
        /// 地面区分に対する移動速度倍率（0 で進入不可）。
        /// 徒歩・ビークルとも RegionTraversal の基準を共有し、急斜面・浅瀬で自然に減速する。
        /// </summary>
        public static float SpeedMultiplier(RegionGround ground, bool isVehicle)
        {
            switch (ground)
            {
                case RegionGround.Walk:
                    return 1f;
                case RegionGround.Steep:
                    return isVehicle ? 0.72f : 0.65f;
                case RegionGround.Scramble:
                    return isVehicle ? 0.35f : 0.32f;
                case RegionGround.Ford:
                    return isVehicle ? 0.55f : 0.45f;
                case RegionGround.Cliff:
                case RegionGround.Water:
                default:
                    return 0f;
            }
        }

        public static bool CanStand(RegionGround ground)
        {
            return ground != RegionGround.Water && ground != RegionGround.Cliff;
        }

        /// <summary>
        /// 指定座標付近でビークル呼び出し・降車に適した平坦寄りの地点をリング探索する。
        /// 見つからなければ元の座標を返す。
        /// </summary>
        public static bool FindNearestStandable(double centerEastM, double centerSouthM,
            float minRadiusM, float maxRadiusM, float stepM,
            Func<double, double, RegionGround> sampleGround,
            out double foundEastM, out double foundSouthM)
        {
            foundEastM = centerEastM;
            foundSouthM = centerSouthM;
            if (sampleGround == null) return false;

            float rStep = Math.Max(2f, stepM);
            for (float r = minRadiusM; r <= maxRadiusM; r += rStep)
            {
                int angles = r < 1f ? 1 : 12;
                for (int i = 0; i < angles; i++)
                {
                    double a = i * (Math.PI * 2.0 / angles);
                    double e = centerEastM + Math.Cos(a) * r;
                    double s = centerSouthM + Math.Sin(a) * r;
                    RegionGround g = sampleGround(e, s);
                    if (g == RegionGround.Walk || g == RegionGround.Steep)
                    {
                        foundEastM = e;
                        foundSouthM = s;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
