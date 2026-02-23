using Vastcore.WorldGen.Recipe;

namespace Vastcore.WorldGen.Observability
{
    /// <summary>
    /// レイヤー単位のデバッグ統計。
    /// </summary>
    public sealed class LayerDebugData
    {
        /// <summary>レイヤーインデックス。</summary>
        public int LayerIndex { get; set; }

        /// <summary>レイヤー種別。</summary>
        public FieldLayerType LayerType { get; set; }

        /// <summary>サンプル数。</summary>
        public int SampleCount { get; private set; }

        /// <summary>最小密度値。</summary>
        public float MinDensity { get; private set; } = float.PositiveInfinity;

        /// <summary>最大密度値。</summary>
        public float MaxDensity { get; private set; } = float.NegativeInfinity;

        /// <summary>平均密度値。</summary>
        public float AverageDensity => SampleCount == 0 ? 0f : _sumDensity / SampleCount;

        private float _sumDensity;

        /// <summary>
        /// 密度サンプルを記録する。
        /// </summary>
        public void RecordSample(float density)
        {
            SampleCount++;
            _sumDensity += density;
            if (density < MinDensity) MinDensity = density;
            if (density > MaxDensity) MaxDensity = density;
        }

        /// <summary>
        /// 統計値をリセットする。
        /// </summary>
        public void Reset()
        {
            SampleCount = 0;
            _sumDensity = 0f;
            MinDensity = float.PositiveInfinity;
            MaxDensity = float.NegativeInfinity;
        }
    }
}
