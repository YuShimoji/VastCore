using System.Text;

namespace Vastcore.WorldGen.Observability
{
    /// <summary>
    /// WorldGen パイプラインの実行統計。
    /// </summary>
    public sealed class WorldGenStats
    {
        /// <summary>Field 構築時間 (ms)。</summary>
        public float FieldBuildTimeMs { get; private set; }

        /// <summary>生成済みチャンク数。</summary>
        public int ChunkCount { get; private set; }

        /// <summary>合計頂点数。</summary>
        public int TotalVertices { get; private set; }

        /// <summary>合計三角形数。</summary>
        public int TotalTriangles { get; private set; }

        /// <summary>直近チャンク更新時間 (ms)。</summary>
        public float LastChunkUpdateTimeMs { get; private set; }

        /// <summary>
        /// Field 構築時間を記録する。
        /// </summary>
        public void RecordFieldBuild(float ms)
        {
            FieldBuildTimeMs = ms < 0f ? 0f : ms;
        }

        /// <summary>
        /// チャンク生成実績を加算する。
        /// </summary>
        public void RecordChunkGeneration(int vertices, int triangles, float ms)
        {
            ChunkCount++;
            TotalVertices += vertices < 0 ? 0 : vertices;
            TotalTriangles += triangles < 0 ? 0 : triangles;
            LastChunkUpdateTimeMs = ms < 0f ? 0f : ms;
        }

        /// <summary>
        /// 統計値をリセットする。
        /// </summary>
        public void Reset()
        {
            FieldBuildTimeMs = 0f;
            ChunkCount = 0;
            TotalVertices = 0;
            TotalTriangles = 0;
            LastChunkUpdateTimeMs = 0f;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(128);
            sb.Append("FieldBuildMs=").Append(FieldBuildTimeMs.ToString("F2"));
            sb.Append(", Chunks=").Append(ChunkCount);
            sb.Append(", Vertices=").Append(TotalVertices);
            sb.Append(", Triangles=").Append(TotalTriangles);
            sb.Append(", LastChunkMs=").Append(LastChunkUpdateTimeMs.ToString("F2"));
            return sb.ToString();
        }
    }
}
