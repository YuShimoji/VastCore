using UnityEngine;
using Vastcore.Player;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    /// <summary>
    /// プレイヤー追跡システム
    /// プレイヤーの位置と移動を監視し、地形生成の優先度を決定
    /// </summary>
    public class PlayerTrackingSystem : MonoBehaviour
    {
        [Header("プレイヤー追跡設定")]
        public Transform playerTransform;
        public float playerMoveThreshold = 50f;
        public bool predictPlayerMovement = true;
        public float predictionTime = 2f;
        
        [Header("タイル範囲設定")]
        public int immediateLoadRadius = 2;
        public int preloadRadius = 4;
        public int keepAliveRadius = 6;
        public int forceUnloadRadius = 8;
        
        private Vector3 lastPlayerPosition;
        private Vector3 playerVelocity;
        private Vector2Int currentPlayerTile;
        private Vector2Int lastPlayerTile;
        
        public Vector2Int CurrentPlayerTile => currentPlayerTile;
        public Vector3 PlayerVelocity => playerVelocity;
        public Vector3 PredictedPosition => GetPredictedPlayerPosition();
        
        private void Start()
        {
            if (playerTransform == null)
            {
                playerTransform = FindFirstObjectByType<AdvancedPlayerController>()?.transform;
            }
            
            if (playerTransform != null)
            {
                lastPlayerPosition = playerTransform.position;
                currentPlayerTile = WorldToTileCoordinate(playerTransform.position);
                lastPlayerTile = currentPlayerTile;
            }
        }
        
        private void Update()
        {
            if (playerTransform == null) return;
            
            UpdatePlayerTracking();
        }
        
        private void UpdatePlayerTracking()
        {
            Vector3 currentPosition = playerTransform.position;
            
            // プレイヤー速度の計算
            playerVelocity = (currentPosition - lastPlayerPosition) / Time.deltaTime;
            
            // タイル座標の更新
            currentPlayerTile = WorldToTileCoordinate(currentPosition);
            
            lastPlayerPosition = currentPosition;
        }
        
        public bool HasPlayerMovedSignificantly()
        {
            if (playerTransform == null) return false;
            
            float distanceMoved = Vector3.Distance(playerTransform.position, lastPlayerPosition);
            return distanceMoved > playerMoveThreshold;
        }
        
        public bool HasPlayerChangedTile()
        {
            bool changed = currentPlayerTile != lastPlayerTile;
            if (changed)
            {
                lastPlayerTile = currentPlayerTile;
            }
            return changed;
        }
        
        public Vector3 GetPredictedPlayerPosition()
        {
            if (!predictPlayerMovement || playerTransform == null)
            {
                return playerTransform?.position ?? Vector3.zero;
            }
            
            return playerTransform.position + playerVelocity * predictionTime;
        }
        
        public List<Vector2Int> GetTilesInRadius(Vector2Int center, int radius)
        {
            var tiles = new List<Vector2Int>();
            
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    if (x * x + z * z <= radius * radius)
                    {
                        tiles.Add(center + new Vector2Int(x, z));
                    }
                }
            }
            
            return tiles;
        }
        
        public List<Vector2Int> GetImmediateLoadTiles()
        {
            return GetTilesInRadius(currentPlayerTile, immediateLoadRadius);
        }
        
        public List<Vector2Int> GetPreloadTiles()
        {
            var preloadTiles = GetTilesInRadius(currentPlayerTile, preloadRadius);
            var immediateTiles = GetTilesInRadius(currentPlayerTile, immediateLoadRadius);
            
            // 即座に読み込むタイルを除外
            preloadTiles.RemoveAll(tile => immediateTiles.Contains(tile));
            
            return preloadTiles;
        }
        
        public List<Vector2Int> GetKeepAliveTiles()
        {
            return GetTilesInRadius(currentPlayerTile, keepAliveRadius);
        }
        
        public List<Vector2Int> GetUnloadCandidates(Dictionary<Vector2Int, TerrainTile> activeTiles)
        {
            var unloadCandidates = new List<Vector2Int>();
            var keepAliveTiles = GetKeepAliveTiles();
            
            foreach (var tileCoord in activeTiles.Keys)
            {
                if (!keepAliveTiles.Contains(tileCoord))
                {
                    unloadCandidates.Add(tileCoord);
                }
            }
            
            return unloadCandidates;
        }
        
        public int GetTilePriority(Vector2Int tileCoord)
        {
            int distance = Mathf.Max(
                Mathf.Abs(tileCoord.x - currentPlayerTile.x),
                Mathf.Abs(tileCoord.y - currentPlayerTile.y)
            );
            
            if (distance <= immediateLoadRadius) return 3; // 最高優先度
            if (distance <= preloadRadius) return 2;       // 高優先度
            if (distance <= keepAliveRadius) return 1;     // 中優先度
            return 0; // 低優先度（削除候補）
        }
        
        private Vector2Int WorldToTileCoordinate(Vector3 worldPosition)
        {
            const float tileSize = 1000f; // RuntimeTerrainManagerのtileSizeと同期
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / tileSize),
                Mathf.FloorToInt(worldPosition.z / tileSize)
            );
        }
        
        public Vector3 TileCoordinateToWorldPosition(Vector2Int tileCoord)
        {
            const float tileSize = 1000f;
            return new Vector3(
                tileCoord.x * tileSize + tileSize * 0.5f,
                0f,
                tileCoord.y * tileSize + tileSize * 0.5f
            );
        }
    }
}