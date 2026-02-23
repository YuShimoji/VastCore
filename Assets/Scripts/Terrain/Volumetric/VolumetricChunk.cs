using UnityEngine;
using Vastcore.WorldGen.Common;

namespace Vastcore.Terrain.Volumetric
{
    /// <summary>
    /// 3D チャンクのメッシュ表示/衝突を保持するコンポーネント。
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public sealed class VolumetricChunk : MonoBehaviour
    {
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private Mesh _runtimeMesh;

        /// <summary>チャンク座標。</summary>
        public VolumetricChunkCoord Coord { get; private set; }

        /// <summary>チャンク原点のワールド座標。</summary>
        public Vector3 WorldOrigin { get; private set; }

        /// <summary>
        /// 初期化する。
        /// </summary>
        public void Initialize(Material material)
        {
            EnsureComponents();
            if (material != null && _meshRenderer.sharedMaterial != material)
                _meshRenderer.sharedMaterial = material;
        }

        /// <summary>
        /// チャンク位置情報を更新する。
        /// </summary>
        public void SetLocation(VolumetricChunkCoord coord, Vector3 worldOrigin)
        {
            Coord = coord;
            WorldOrigin = worldOrigin;
            transform.position = worldOrigin;
            gameObject.name = $"VolumetricChunk_{coord.X}_{coord.Y}_{coord.Z}";
        }

        /// <summary>
        /// メッシュとコライダーを更新する。
        /// </summary>
        public void BuildMesh(Mesh mesh)
        {
            EnsureComponents();
            ReplaceRuntimeMesh(mesh);
            _meshFilter.sharedMesh = mesh;
            _meshCollider.sharedMesh = null;
            _meshCollider.sharedMesh = mesh;
        }

        /// <summary>
        /// メッシュをクリアする。
        /// </summary>
        public void ClearMesh()
        {
            ReplaceRuntimeMesh(null);
            if (_meshFilter != null)
                _meshFilter.sharedMesh = null;
            if (_meshCollider != null)
                _meshCollider.sharedMesh = null;
        }

        private void OnDestroy()
        {
            ReplaceRuntimeMesh(null);
        }

        private void EnsureComponents()
        {
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshCollider == null) _meshCollider = GetComponent<MeshCollider>();
        }

        private void ReplaceRuntimeMesh(Mesh nextMesh)
        {
            if (_runtimeMesh != null && _runtimeMesh != nextMesh)
            {
                if (Application.isPlaying)
                    Destroy(_runtimeMesh);
                else
                    DestroyImmediate(_runtimeMesh);
            }

            _runtimeMesh = nextMesh;
        }
    }
}
