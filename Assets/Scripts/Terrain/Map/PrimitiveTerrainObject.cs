using UnityEngine;
using System.Collections.Generic;
using Vastcore.Core;
using Vastcore.Generation;

namespace Vastcore.Terrain.Map
{
    /// <summary>
    /// プリミティブ地形オブジェクトのコンポーネント
    /// インタラクション、LOD、プール管理に関する情報を保持
    /// </summary>
    public class PrimitiveTerrainObject : MonoBehaviour, IPoolable
    {
        [Header("プリミティブ情報")]
        public GenerationPrimitiveType primitiveType;
        public Vector3 originalScale;
        public float generationTime;
        
        [Header("インタラクション設定")]
        public bool isClimbable = false;
        public bool isGrindable = false;
        public bool isDestructible = false;
        public float destructionThreshold = 100f;
        
        [Header("LOD設定")]
        public bool enableLOD = true;
        public float lodDistance0 = 50f;   // 高品質
        public float lodDistance1 = 100f;  // 中品質
        public float lodDistance2 = 200f;  // 低品質
        
        [Header("メモリ管理")]
        public bool isPooled = false;
        public float lastAccessTime;
        
        [Header("後方互換性")]
        public Vector3 scale
        {
            get => originalScale;
            set => originalScale = value;
        }
        public bool hasCollision
        {
            get => objectCollider != null && objectCollider.enabled;
            set
            {
                if (objectCollider != null)
                {
                    objectCollider.enabled = value;
                }
            }
        }
        public Mesh[] lodMeshes => new Mesh[0]; // TODO: LODメッシュ実装時に拡張
        
        // 内部状態
        private int currentLODLevel = 0;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private Collider objectCollider;
        
        // グローバル統計
        private static int totalActiveObjects = 0;
        private static Dictionary<int, int> lodLevelDistribution = new Dictionary<int, int>();
        
        void Awake()
        {
            CacheComponents();
            totalActiveObjects++;
        }
        
        void OnDestroy()
        {
            totalActiveObjects--;
            if (lodLevelDistribution.ContainsKey(currentLODLevel))
            {
                lodLevelDistribution[currentLODLevel]--;
            }
        }
        
        /// <summary>
        /// コンポーネントをキャッシュ
        /// </summary>
        private void CacheComponents()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
            objectCollider = GetComponent<Collider>();
        }
        
        /// <summary>
        /// プールから初期化
        /// </summary>
        public void InitializeFromPool(GenerationPrimitiveType type, Vector3 position, float scale)
        {
            primitiveType = type;
            transform.position = position;
            originalScale = Vector3.one * scale;
            transform.localScale = originalScale;
            generationTime = Time.time;
            lastAccessTime = Time.time;
            isPooled = true;
            
            // インタラクション設定の更新
            UpdateInteractionSettings();
            
            // LOD設定の初期化
            currentLODLevel = 0;
            UpdateLODLevel(0);
            
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// LODレベルを更新
        /// </summary>
        public void UpdateLODLevel(int newLevel)
        {
            if (currentLODLevel != newLevel)
            {
                // 統計を更新
                if (lodLevelDistribution.ContainsKey(currentLODLevel))
                {
                    lodLevelDistribution[currentLODLevel]--;
                }
                
                currentLODLevel = newLevel;
                
                if (!lodLevelDistribution.ContainsKey(currentLODLevel))
                {
                    lodLevelDistribution[currentLODLevel] = 0;
                }
                lodLevelDistribution[currentLODLevel]++;
                
                ApplyLODLevel(currentLODLevel);
            }
        }
        
        /// <summary>
        /// LODレベルを適用
        /// </summary>
        private void ApplyLODLevel(int level)
        {
            if (!enableLOD) return;
            
            switch (level)
            {
                case 0: // 高品質
                    if (meshRenderer != null) meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    if (objectCollider != null) objectCollider.enabled = true;
                    break;
                    
                case 1: // 中品質
                    if (meshRenderer != null) meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    if (objectCollider != null) objectCollider.enabled = true;
                    break;
                    
                case 2: // 低品質
                    if (meshRenderer != null) meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    if (objectCollider != null) objectCollider.enabled = false;
                    break;
                    
                case 3: // 最低品質（カリング候補）
                    if (meshRenderer != null) meshRenderer.enabled = false;
                    if (objectCollider != null) objectCollider.enabled = false;
                    break;
            }
        }
        
        /// <summary>
        /// カメラからの距離に基づいてLODを更新
        /// </summary>
        public void UpdateLODBasedOnDistance(Vector3 cameraPosition)
        {
            if (!enableLOD) return;
            
            float distance = Vector3.Distance(transform.position, cameraPosition);
            int newLevel = 0;
            
            if (distance > lodDistance2)
            {
                newLevel = 3; // カリング
            }
            else if (distance > lodDistance1)
            {
                newLevel = 2; // 低品質
            }
            else if (distance > lodDistance0)
            {
                newLevel = 1; // 中品質
            }
            else
            {
                newLevel = 0; // 高品質
            }
            
            UpdateLODLevel(newLevel);
            lastAccessTime = Time.time;
        }
        
        /// <summary>
        /// プールからオブジェクトを取得した時の初期化処理
        /// </summary>
        public void OnSpawnFromPool()
        {
            isPooled = true;
            lastAccessTime = Time.time;
            currentLODLevel = 0;
            UpdateLODLevel(0);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// プールにオブジェクトを返却する時のクリーンアップ処理
        /// </summary>
        public void OnReturnToPool()
        {
            PrepareForPool();
        }

        /// <summary>
        /// オブジェクトが利用可能かどうかの確認
        /// </summary>
        public bool IsAvailable => !isPooled && gameObject.activeSelf;
        
        /// <summary>
        /// プールに戻す準備
        /// </summary>
        public void PrepareForPool()
        {
            gameObject.SetActive(false);
            currentLODLevel = 0;
            
            // コンポーネントをリセット
            if (meshRenderer != null) meshRenderer.enabled = true;
            if (objectCollider != null) objectCollider.enabled = true;
        }
        
        /// <summary>
        /// グローバルLOD統計を取得
        /// </summary>
        public static LODStatistics GetGlobalLODStatistics()
        {
            return new LODStatistics
            {
                totalActiveObjects = totalActiveObjects,
                lod0Count = lodLevelDistribution.ContainsKey(0) ? lodLevelDistribution[0] : 0,
                lod1Count = lodLevelDistribution.ContainsKey(1) ? lodLevelDistribution[1] : 0,
                lod2Count = lodLevelDistribution.ContainsKey(2) ? lodLevelDistribution[2] : 0,
                lod3Count = lodLevelDistribution.ContainsKey(3) ? lodLevelDistribution[3] : 0
            };
        }
        
        /// <summary>
        /// グローバル統計をリセット
        /// </summary>
        public static void ResetGlobalStatistics()
        {
            lodLevelDistribution.Clear();
        }
    }
    
    /// <summary>
    /// LOD統計情報
    /// </summary>
    [System.Serializable]
    public struct LODStatistics
    {
        public int totalActiveObjects;
        public int lod0Count;
        public int lod1Count;
        public int lod2Count;
        public int lod3Count;
        
        // 後方互換性のためのプロパティ
        public int visibleObjects => totalActiveObjects;
        public int totalObjects => totalActiveObjects;
        public int[] lodCounts => new int[] { lod0Count, lod1Count, lod2Count, lod3Count };
        
        public override string ToString()
        {
            return $"Total: {totalActiveObjects}, LOD0: {lod0Count}, LOD1: {lod1Count}, LOD2: {lod2Count}, LOD3: {lod3Count}";
        }
    }
    
    // 拡張メソッドヘルパー
    public static class PrimitiveTerrainObjectExtensions
    {
        /// <summary>
        /// プリミティブタイプに基づいてインタラクション設定を更新
        /// </summary>
        public static void UpdateInteractionSettings(this PrimitiveTerrainObject obj)
        {
            switch (obj.primitiveType)
            {
                case GenerationPrimitiveType.Sphere:
                case GenerationPrimitiveType.Boulder:
                    obj.isClimbable = true;
                    obj.isGrindable = false;
                    break;
                case GenerationPrimitiveType.Ring:
                case GenerationPrimitiveType.Torus:
                    obj.isClimbable = false;
                    obj.isGrindable = true;
                    break;
                default:
                    obj.isClimbable = true;
                    obj.isGrindable = false;
                    break;
            }
        }
    }
}
