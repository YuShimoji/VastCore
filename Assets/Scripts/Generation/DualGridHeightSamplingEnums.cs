namespace Vastcore.Generation
{
    /// <summary>
    /// DualGrid 高さサンプリング時の UV アドレスモード
    /// </summary>
    public enum DualGridUvAddressMode
    {
        /// <summary>UV を 0～1 にクランプ</summary>
        Clamp,

        /// <summary>UV をリピート（ラップアラウンド）</summary>
        Wrap
    }

    /// <summary>
    /// DualGrid 高さサンプリング時の量子化ポリシー
    /// </summary>
    public enum DualGridHeightQuantization
    {
        /// <summary>切り捨て (Floor)</summary>
        FloorToInt,

        /// <summary>四捨五入 (Round)</summary>
        RoundToInt,

        /// <summary>切り上げ (Ceil)</summary>
        CeilToInt
    }
}
