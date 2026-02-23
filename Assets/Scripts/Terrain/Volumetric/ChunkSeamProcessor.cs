using UnityEngine;

namespace Vastcore.Terrain.Volumetric
{
    /// <summary>
    /// 既定のチャンク境界シーム軽減処理。
    /// 境界スナップと任意量子化を実行する。
    /// </summary>
    public sealed class ChunkSeamProcessor : IChunkSeamProcessor
    {
        /// <summary>
        /// シーム軽減設定。
        /// </summary>
        public struct Options
        {
            public bool snapBorderVertices;
            public bool quantizeVertices;
            public float vertexQuantizeStep;
            public float borderSnapEpsilon;
        }

        private readonly Options _options;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public ChunkSeamProcessor(Options options)
        {
            _options = options;
        }

        /// <inheritdoc />
        public void Process(Mesh mesh, float chunkSize, float voxelSize)
        {
            if (mesh == null)
                return;
            if (!_options.snapBorderVertices && !_options.quantizeVertices)
                return;

            Vector3[] vertices = mesh.vertices;
            if (vertices == null || vertices.Length == 0)
                return;

            bool changed = false;
            float snapEpsilon = Mathf.Max(0.000001f, _options.borderSnapEpsilon * voxelSize);
            float step = Mathf.Max(0.000001f, _options.vertexQuantizeStep * voxelSize);

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];

                if (_options.snapBorderVertices)
                {
                    if (Mathf.Abs(v.x) <= snapEpsilon) v.x = 0f;
                    else if (Mathf.Abs(v.x - chunkSize) <= snapEpsilon) v.x = chunkSize;

                    if (Mathf.Abs(v.z) <= snapEpsilon) v.z = 0f;
                    else if (Mathf.Abs(v.z - chunkSize) <= snapEpsilon) v.z = chunkSize;
                }

                if (_options.quantizeVertices)
                {
                    v.x = Mathf.Round(v.x / step) * step;
                    v.y = Mathf.Round(v.y / step) * step;
                    v.z = Mathf.Round(v.z / step) * step;
                }

                if (v != vertices[i])
                {
                    vertices[i] = v;
                    changed = true;
                }
            }

            if (!changed)
                return;

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
