using System;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 256 km 規模のワールドにおける絶対座標（メートル、倍精度）。
    /// X は東向き正、SouthM は南向き正（RegionData の地図座標と同じ向き）、HeightM は標高（上向き正）。
    /// </summary>
    public struct RegionWorldPoint
    {
        public double EastM;
        public double SouthM;
        public float HeightM;

        public RegionWorldPoint(double eastM, float heightM, double southM)
        {
            EastM = eastM;
            HeightM = heightM;
            SouthM = southM;
        }
    }

    /// <summary>
    /// 地図座標（0〜100）⇄ 絶対メートル座標（double）⇄ Unity ローカル座標（float、Z 北向き正）を
    /// 一元変換し、Floating Origin（原点リセンタリング）を管理する。UnityEngine 非依存。
    /// </summary>
    public sealed class RegionWorldCoordinates
    {
        public const double DefaultRecenterThresholdMeters = 2000.0;

        public readonly float RegionSizeKm;
        public readonly double WorldSizeMeters;
        public readonly double MetersPerMapUnit;

        public double OriginEastM { get; private set; }
        public double OriginSouthM { get; private set; }
        public double RecenterThresholdMeters { get; set; }

        public RegionWorldCoordinates(float regionSizeKm, double originEastM = 0.0, double originSouthM = 0.0,
            double recenterThresholdMeters = DefaultRecenterThresholdMeters)
        {
            if (regionSizeKm <= 0f) throw new ArgumentOutOfRangeException("regionSizeKm");
            RegionSizeKm = regionSizeKm;
            WorldSizeMeters = regionSizeKm * 1000.0;
            MetersPerMapUnit = WorldSizeMeters / RegionData.MapUnits;
            OriginEastM = originEastM;
            OriginSouthM = originSouthM;
            RecenterThresholdMeters = recenterThresholdMeters > 0.0 ? recenterThresholdMeters : DefaultRecenterThresholdMeters;
        }

        public static RegionWorldCoordinates ForRegion(RegionData region, float originMapX = 50f, float originMapY = 50f,
            double snapGridMeters = 512.0)
        {
            if (region == null) throw new ArgumentNullException("region");
            var coords = new RegionWorldCoordinates(region.SizeKm);
            coords.MapToWorldMeters(originMapX, originMapY, out double eastM, out double southM);
            if (snapGridMeters > 0.0)
                SnapToGrid(eastM, southM, snapGridMeters, out eastM, out southM);
            coords.SetOrigin(eastM, southM);
            return coords;
        }

        public void SetOrigin(double originEastM, double originSouthM)
        {
            OriginEastM = originEastM;
            OriginSouthM = originSouthM;
        }

        public void MapToWorldMeters(float mapX, float mapY, out double eastM, out double southM)
        {
            eastM = mapX * MetersPerMapUnit;
            southM = mapY * MetersPerMapUnit;
        }

        public void WorldMetersToMap(double eastM, double southM, out float mapX, out float mapY)
        {
            mapX = (float)(eastM / MetersPerMapUnit);
            mapY = (float)(southM / MetersPerMapUnit);
        }

        public bool ContainsWorld(double eastM, double southM)
        {
            return eastM >= 0.0 && eastM <= WorldSizeMeters && southM >= 0.0 && southM <= WorldSizeMeters;
        }

        /// <summary>
        /// 絶対メートル座標から Unity ローカル座標へ変換する。
        /// Unity の Z は北向き正なので、南向き座標の符号を反転する。
        /// </summary>
        public void WorldToLocal(double eastM, float heightM, double southM,
            out float localX, out float localY, out float localZ)
        {
            localX = (float)(eastM - OriginEastM);
            localY = heightM;
            localZ = -(float)(southM - OriginSouthM);
        }

        /// <summary>Unity ローカル座標から絶対メートル座標へ変換する。</summary>
        public void LocalToWorld(float localX, float localY, float localZ,
            out double eastM, out float heightM, out double southM)
        {
            eastM = OriginEastM + localX;
            heightM = localY;
            southM = OriginSouthM - localZ;
        }

        /// <summary>現在のローカル座標が原点から閾値距離を超えているか。</summary>
        public bool NeedsRecenter(float localX, float localZ)
        {
            double distSq = (double)localX * localX + (double)localZ * localZ;
            return distSq >= RecenterThresholdMeters * RecenterThresholdMeters;
        }

        /// <summary>
        /// 原点を新しい絶対座標へ移し、既存の Unity ローカルオブジェクトへ加算すべき差分ベクトルを返す。
        /// </summary>
        public bool Recenter(double newOriginEastM, double newOriginSouthM,
            out float shiftLocalX, out float shiftLocalZ)
        {
            double dEast = newOriginEastM - OriginEastM;
            double dSouth = newOriginSouthM - OriginSouthM;
            if (Math.Abs(dEast) < 1e-6 && Math.Abs(dSouth) < 1e-6)
            {
                shiftLocalX = 0f;
                shiftLocalZ = 0f;
                return false;
            }
            OriginEastM = newOriginEastM;
            OriginSouthM = newOriginSouthM;
            // localX_new = east - newOrigin = (east - oldOrigin) - dEast = localX_old - dEast
            // localZ_new = -(south - newOrigin) = -(south - oldOrigin) + dSouth = localZ_old + dSouth
            shiftLocalX = (float)(-dEast);
            shiftLocalZ = (float)(dSouth);
            return true;
        }

        /// <summary>任意の絶対座標を格子点へ丸める（チャンク境界と原点を揃える用途）。</summary>
        public static void SnapToGrid(double eastM, double southM, double gridMeters,
            out double snappedEastM, out double snappedSouthM)
        {
            if (gridMeters <= 0.0)
            {
                snappedEastM = eastM;
                snappedSouthM = southM;
                return;
            }
            snappedEastM = Math.Floor(eastM / gridMeters + 0.5) * gridMeters;
            snappedSouthM = Math.Floor(southM / gridMeters + 0.5) * gridMeters;
        }
    }
}
