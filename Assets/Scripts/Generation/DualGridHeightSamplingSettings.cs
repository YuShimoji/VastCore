using System;
using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// DualGrid 高さサンプリング用プロファイル設定
    /// ワールド座標→UV 変換のバウンズ、UV アドレスモード、量子化ポリシーを保持する
    /// </summary>
    [Serializable]
    public class DualGridHeightSamplingSettings
    {
        [Tooltip("プロファイルのバウンズを使用するか（false の場合レガシー固定レンジ）")]
        public bool UseProfileBounds = true;

        [Tooltip("ワールド空間の最小 XZ")]
        public Vector2 WorldMinXZ = new Vector2(-10f, -10f);

        [Tooltip("ワールド空間の最大 XZ")]
        public Vector2 WorldMaxXZ = new Vector2(10f, 10f);

        [Tooltip("UV アドレスモード")]
        public DualGridUvAddressMode UvAddressMode = DualGridUvAddressMode.Clamp;

        [Tooltip("高さ量子化ポリシー")]
        public DualGridHeightQuantization HeightQuantization = DualGridHeightQuantization.RoundToInt;

        /// <summary>
        /// デフォルト値で新規インスタンスを返す
        /// </summary>
        public static DualGridHeightSamplingSettings CreateDefault()
        {
            return new DualGridHeightSamplingSettings();
        }

        /// <summary>
        /// 別の設定からコピーする
        /// </summary>
        public void CopyFrom(DualGridHeightSamplingSettings other)
        {
            if (other == null) return;
            UseProfileBounds = other.UseProfileBounds;
            WorldMinXZ = other.WorldMinXZ;
            WorldMaxXZ = other.WorldMaxXZ;
            UvAddressMode = other.UvAddressMode;
            HeightQuantization = other.HeightQuantization;
        }
    }
}
