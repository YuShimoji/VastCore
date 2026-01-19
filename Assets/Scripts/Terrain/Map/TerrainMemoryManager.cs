using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Generation
{
    /// <summary>
    /// 地形システム専用のメモリ管理クラス
    /// メモリ使用量の監視、クリーンアップ、最適化を担当
    /// </summary>
    public class TerrainMemoryManager : MonoBehaviour
    {
        [Header("メモリ管理設定")]
        public float memoryLimitMB = 800f;
        public float memoryWarningThresholdMB = 600f;
        public bool enableAggressiveCleanup = true;
        public float cleanupInterval = 5f;
        
        private float lastCleanupTime;
        private Dictionary<Vector2Int, TerrainTile> activeTiles;
        
        public void Initialize(Dictionary<Vector2Int, TerrainTile> tileDict)
        {
            activeTiles = tileDict;
            lastCleanupTime = Time.time;
        }
        
        private void Update()
        {
            if (Time.time - lastCleanupTime > cleanupInterval)
            {
                PerformMemoryCleanup();
                lastCleanupTime = Time.time;
            }
        }
        
        public bool IsMemoryLimitExceeded()
        {
            float currentMemoryMB = GetCurrentMemoryUsageMB();
            return currentMemoryMB > memoryLimitMB;
        }
        
        public bool IsMemoryWarningLevel()
        {
            float currentMemoryMB = GetCurrentMemoryUsageMB();
            return currentMemoryMB > memoryWarningThresholdMB;
        }
        
        public float GetCurrentMemoryUsageMB()
        {
            return System.GC.GetTotalMemory(false) / (1024f * 1024f);
        }
        
        public void PerformMemoryCleanup()
        {
            if (!IsMemoryWarningLevel()) return;
            
            // 使用されていないタイルを特定
            var unusedTiles = activeTiles.Values
                .Where(tile =>
                {
                    var secondsSinceLastAccess = (float)(System.DateTime.Now - tile.lastAccessTime).TotalSeconds;
                    return !tile.isActive && secondsSinceLastAccess > 30f;
                })
                .OrderBy(tile => tile.lastAccessTime)
                .ToList();
            
            // メモリ制限に応じてタイルを削除
            int tilesToRemove = enableAggressiveCleanup ? unusedTiles.Count / 2 : unusedTiles.Count / 4;
            
            for (int i = 0; i < tilesToRemove && i < unusedTiles.Count; i++)
            {
                var tile = unusedTiles[i];
                if (tile.tileObject != null)
                {
                    DestroyImmediate(tile.tileObject);
                }
                activeTiles.Remove(tile.coordinate);
            }
            
            // ガベージコレクションを実行
            if (IsMemoryLimitExceeded())
            {
                System.GC.Collect();
                Resources.UnloadUnusedAssets();
            }
        }
        
        public void ForceCleanup()
        {
            PerformMemoryCleanup();
            System.GC.Collect();
            Resources.UnloadUnusedAssets();
        }
    }
}