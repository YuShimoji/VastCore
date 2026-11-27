using System;

namespace Vastcore.RegionMap
{
    /// <summary>統合セッション（RegionWorldSession）における現在の操作・表示モード。</summary>
    public enum RegionWorldMode : byte
    {
        /// <summary>2D 鳥瞰図探索（既存の RegionExplorer）。</summary>
        Overview2D = 0,
        /// <summary>2D 地点指定から 3D 空間へのシームレス降下遷移中。</summary>
        Diving = 1,
        /// <summary>3D 上空ドローン派遣・オートパイロット・旋回モード。</summary>
        DroneRecon3D = 2,
        /// <summary>3D オープンワールド移動モード（徒歩・簡易ビークル）。</summary>
        OpenWorld3D = 3,
        /// <summary>3D 空間から 2D 鳥瞰図へシームレス復帰中。</summary>
        Ascending = 4,
    }

    /// <summary>
    /// 2D 見下ろし ⇄ 3D 地上/上空のシームレス遷移カーブ（高度・Pitch・2D 透過・雲通過フォグ）。
    /// UnityEngine 非依存のため EditMode テストで直接検証できる。
    /// </summary>
    public sealed class DiveTransitionCurve
    {
        public float StartAglMeters = 18000f;
        public float TargetAglMeters = 4.5f;
        public float TopPitchDegrees = 89.2f;
        public float TargetPitchDegrees = 18f;
        public float CloudBaseAglMeters = 1500f;
        public float CloudTopAglMeters = 2600f;

        /// <summary>2D 探索画面の表示幅（m）とカメラ画角・アスペクト比から同期開始高度を算出する。</summary>
        public static float ComputeSyncAltitude(float visibleWidthMeters, float verticalFovDegrees, float aspect)
        {
            if (visibleWidthMeters < 200f) visibleWidthMeters = 200f;
            if (aspect < 0.5f) aspect = 1.777f;
            double halfVFov = Math.Max(10.0, verticalFovDegrees) * 0.5 * Math.PI / 180.0;
            double halfHFovTan = Math.Tan(halfVFov) * aspect;
            double alt = (visibleWidthMeters * 0.5) / Math.Max(0.1, halfHFovTan);
            if (alt < 1200.0) alt = 1200.0;
            if (alt > 45000.0) alt = 45000.0;
            return (float)alt;
        }

        /// <summary>
        /// progress01（0→1）に対するカメラ対地高度・絶対高度・Pitch・2D オーバーレイ不透明度・雲通過フォグ強度を返す。
        /// ascending = true のときは逆方向（3D → 2D）を評価する。
        /// </summary>
        public void Evaluate(float progress01, float groundHeightM, float cloudCover, bool ascending,
            out float altitudeM, out float pitchDegrees, out float overlay2DAlpha, out float cloudPassFog)
        {
            float t = progress01 < 0f ? 0f : (progress01 > 1f ? 1f : progress01);
            float diveT = ascending ? 1f - t : t;

            // 対数高度補間により高高度→低高度の体感ズーム速度を一定に保つ
            float startAgl = Math.Max(100f, StartAglMeters);
            float endAgl = Math.Max(1.5f, TargetAglMeters);
            float easeAlt = SmoothStep(0f, 1f, diveT);
            double logAgl = Math.Log(startAgl) + (Math.Log(endAgl) - Math.Log(startAgl)) * easeAlt;
            float agl = (float)Math.Exp(logAgl);
            altitudeM = groundHeightM + agl;

            // 前半 35% は真下維持、後半 65% で斜め前方へチルト
            float pitchT = SmoothStep(0.35f, 1f, diveT);
            pitchDegrees = TopPitchDegrees + (TargetPitchDegrees - TopPitchDegrees) * pitchT;

            // 2D オーバーレイは前半 45% でフェードアウト（上昇時は後半 45% でフェードイン）
            overlay2DAlpha = 1f - SmoothStep(0.05f, 0.45f, diveT);

            // 雲の層（1500m〜2600m AGL）を通過するときだけ柔らかな霧ピークを作る
            float cloudMid = 0.5f * (CloudBaseAglMeters + CloudTopAglMeters);
            float cloudHalf = Math.Max(200f, 0.5f * (CloudTopAglMeters - CloudBaseAglMeters) + 350f);
            float dist = Math.Abs(agl - cloudMid) / cloudHalf;
            float band = dist < 1f ? (1f - dist * dist) : 0f;
            float cover = cloudCover < 0f ? 0f : (cloudCover > 1f ? 1f : cloudCover);
            cloudPassFog = band * band * cover;
        }

        private static float SmoothStep(float edge0, float edge1, float x)
        {
            if (edge1 <= edge0) return x >= edge1 ? 1f : 0f;
            float u = (x - edge0) / (edge1 - edge0);
            if (u < 0f) u = 0f; else if (u > 1f) u = 1f;
            return u * u * (3f - 2f * u);
        }
    }
}
