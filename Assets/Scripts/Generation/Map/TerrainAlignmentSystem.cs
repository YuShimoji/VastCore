using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    /// <summary>
    /// 地形配置・整列システム
    /// プリミティブ地形オブジェクトの地形への適切な配置と整列を管理
    /// </summary>
    public static class TerrainAlignmentSystem
    {
        #region 配置設定
        [System.Serializable]
        public struct AlignmentSettings
        {
            [Header("整列設定")]
            public bool alignToTerrainNormal;
            public bool embedInTerrain;
            public float embedDepthRatio; // オブジェクトの何%を地面に埋めるか
            
            [Header("配置制約")]
            public float minDistanceBetweenObjects;
            public float maxTerrainSlope; // 配置可能な最大傾斜（度）
            public float minTerrainHeight;
            public float maxTerrainHeight;
            
            [Header("衝突回避")]
            public bool enableCollisionAvoidance;
            public LayerMask obstacleLayerMask;
            public float collisionCheckRadius;
            
            public static AlignmentSettings Default()
            {
                return new AlignmentSettings
                {
                    alignToTerrainNormal = true,
                    embedInTerrain = true,
                    embedDepthRatio = 0.1f, // 10%埋め込み
                    minDistanceBetweenObjects = 200f,
                    maxTerrainSlope = 45f,
                    minTerrainHeight = 0f,
                    maxTerrainHeight = 1000f,
                    enableCollisionAvoidance = true,
                    obstacleLayerMask = -1, // すべてのレイヤー
                    collisionCheckRadius = 50f
                };
            }
        }
        #endregion

        #region 地形整列機能
        /// <summary>
        /// プリミティブを地形に整列させる
        /// </summary>
        public static void AlignPrimitiveToTerrain(GameObject primitive, Vector3 terrainNormal, AlignmentSettings settings)
        {
            if (primitive == null)
            {
                Debug.LogError("Primitive object is null");
                return;
            }

            if (!settings.alignToTerrainNormal)
            {
                return;
            }

            // 地形の法線に基づいて回転を調整
            AlignRotationToNormal(primitive, terrainNormal);
            
            // 地形に埋め込み調整
            if (settings.embedInTerrain)
            {
                AdjustHeightForTerrain(primitive, terrainNormal, settings.embedDepthRatio);
            }
        }

        /// <summary>
        /// 地形法線に基づいて回転を調整
        /// </summary>
        private static void AlignRotationToNormal(GameObject primitive, Vector3 terrainNormal)
        {
            // 地形の法線をY軸として使用
            Vector3 up = terrainNormal.normalized;
            
            // 現在の前方向を維持しつつ、上方向を地形法線に合わせる
            Vector3 forward = primitive.transform.forward;
            Vector3 right = Vector3.Cross(up, forward).normalized;
            forward = Vector3.Cross(right, up).normalized;
            
            // 新しい回転を適用
            primitive.transform.rotation = Quaternion.LookRotation(forward, up);
        }

        /// <summary>
        /// 地形に適切な高さで配置
        /// </summary>
        private static void AdjustHeightForTerrain(GameObject primitive, Vector3 terrainNormal, float embedDepthRatio)
        {
            // プリミティブの境界ボックスを取得
            Bounds bounds = GetObjectBounds(primitive);
            
            if (bounds.size == Vector3.zero)
            {
                Debug.LogWarning($"Could not get bounds for {primitive.name}");
                return;
            }
            
            // 埋め込み深度を計算
            float embedDepth = bounds.size.y * embedDepthRatio;
            
            // 地形表面からの適切な距離を計算
            Vector3 adjustedPosition = primitive.transform.position - terrainNormal * embedDepth;
            primitive.transform.position = adjustedPosition;
        }

        /// <summary>
        /// オブジェクトの境界ボックスを取得
        /// </summary>
        private static Bounds GetObjectBounds(GameObject obj)
        {
            Bounds bounds = new Bounds();
            bool hasBounds = false;
            
            // すべてのMeshRendererから境界を計算
            MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            
            return bounds;
        }
        #endregion

        #region 配置検証機能
        /// <summary>
        /// 指定位置が配置に適しているかを判定
        /// </summary>
        public static bool IsValidPlacementPosition(Vector3 position, float radius, AlignmentSettings settings, List<Vector3> occupiedPositions = null)
        {
            // 地形情報を取得
            TerrainInfo terrainInfo = GetTerrainInfoAtPosition(position);
            
            // 地形高度チェック
            if (terrainInfo.height < settings.minTerrainHeight || terrainInfo.height > settings.maxTerrainHeight)
            {
                return false;
            }
            
            // 地形傾斜チェック
            float slope = Vector3.Angle(Vector3.up, terrainInfo.normal);
            if (slope > settings.maxTerrainSlope)
            {
                return false;
            }
            
            // 他のオブジェクトとの距離チェック
            if (occupiedPositions != null)
            {
                foreach (var occupied in occupiedPositions)
                {
                    if (Vector3.Distance(position, occupied) < settings.minDistanceBetweenObjects + radius)
                    {
                        return false;
                    }
                }
            }
            
            // 衝突チェック
            if (settings.enableCollisionAvoidance)
            {
                if (HasCollisionAtPosition(position, settings.collisionCheckRadius, settings.obstacleLayerMask))
                {
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// 指定位置の地形情報を取得
        /// </summary>
        public static TerrainInfo GetTerrainInfoAtPosition(Vector3 position)
        {
            TerrainInfo info = new TerrainInfo();
            
            // レイキャストで地形情報を取得
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 1000f, Vector3.down, out hit, 2000f))
            {
                info.position = hit.point;
                info.normal = hit.normal;
                info.height = hit.point.y;
                info.hasValidTerrain = true;
            }
            else
            {
                // レイキャストが失敗した場合のフォールバック
                info.position = position;
                info.normal = Vector3.up;
                info.height = position.y;
                info.hasValidTerrain = false;
            }
            
            return info;
        }

        /// <summary>
        /// 指定位置に衝突があるかチェック
        /// </summary>
        private static bool HasCollisionAtPosition(Vector3 position, float radius, LayerMask layerMask)
        {
            Collider[] colliders = Physics.OverlapSphere(position, radius, layerMask);
            return colliders.Length > 0;
        }
        #endregion

        #region 配置最適化機能
        /// <summary>
        /// 有効な配置位置を検索
        /// </summary>
        public static Vector3? FindNearestValidPosition(Vector3 desiredPosition, float radius, AlignmentSettings settings, List<Vector3> occupiedPositions = null, int maxAttempts = 20)
        {
            // まず希望位置をチェック
            if (IsValidPlacementPosition(desiredPosition, radius, settings, occupiedPositions))
            {
                return desiredPosition;
            }
            
            // 螺旋状に検索
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                float searchRadius = settings.minDistanceBetweenObjects * attempt * 0.5f;
                int searchPoints = attempt * 8; // 検索点数を増やす
                
                for (int i = 0; i < searchPoints; i++)
                {
                    float angle = (float)i / searchPoints * Mathf.PI * 2f;
                    Vector3 offset = new Vector3(
                        Mathf.Cos(angle) * searchRadius,
                        0f,
                        Mathf.Sin(angle) * searchRadius
                    );
                    
                    Vector3 testPosition = desiredPosition + offset;
                    
                    // 地形の高さに合わせて調整
                    TerrainInfo terrainInfo = GetTerrainInfoAtPosition(testPosition);
                    if (terrainInfo.hasValidTerrain)
                    {
                        testPosition.y = terrainInfo.height;
                        
                        if (IsValidPlacementPosition(testPosition, radius, settings, occupiedPositions))
                        {
                            return testPosition;
                        }
                    }
                }
            }
            
            Debug.LogWarning($"Could not find valid placement position near {desiredPosition} after {maxAttempts} attempts");
            return null;
        }

        /// <summary>
        /// 複数のオブジェクトを効率的に配置
        /// </summary>
        public static List<Vector3> GenerateOptimalPlacements(Vector3 centerPosition, float areaRadius, int objectCount, float objectRadius, AlignmentSettings settings)
        {
            List<Vector3> placements = new List<Vector3>();
            List<Vector3> occupiedPositions = new List<Vector3>();
            
            // ポアソンディスク分布を使用して均等な配置を生成
            for (int i = 0; i < objectCount * 3; i++) // 余裕を持って多めに試行
            {
                if (placements.Count >= objectCount)
                    break;
                
                // ランダムな位置を生成
                Vector2 randomPoint = Random.insideUnitCircle * areaRadius;
                Vector3 candidatePosition = centerPosition + new Vector3(randomPoint.x, 0f, randomPoint.y);
                
                // 有効な位置を検索
                Vector3? validPosition = FindNearestValidPosition(candidatePosition, objectRadius, settings, occupiedPositions, 10);
                
                if (validPosition.HasValue)
                {
                    placements.Add(validPosition.Value);
                    occupiedPositions.Add(validPosition.Value);
                }
            }
            
            Debug.Log($"Generated {placements.Count} optimal placements out of requested {objectCount}");
            return placements;
        }
        #endregion

        #region データ構造
        [System.Serializable]
        public struct TerrainInfo
        {
            public Vector3 position;
            public Vector3 normal;
            public float height;
            public bool hasValidTerrain;
        }
        #endregion

        #region デバッグ機能
        /// <summary>
        /// 配置情報をデバッグ表示
        /// </summary>
        public static void DrawPlacementDebugInfo(Vector3 position, float radius, AlignmentSettings settings, Color color)
        {
            // 配置範囲を表示
            Debug.DrawWireSphere(position, radius, color, 5f);
            
            // 地形法線を表示
            TerrainInfo terrainInfo = GetTerrainInfoAtPosition(position);
            if (terrainInfo.hasValidTerrain)
            {
                Debug.DrawRay(terrainInfo.position, terrainInfo.normal * 20f, color, 5f);
            }
            
            // 最小距離範囲を表示
            Debug.DrawWireSphere(position, settings.minDistanceBetweenObjects, Color.yellow, 5f);
        }

        /// <summary>
        /// 配置統計を取得
        /// </summary>
        public static PlacementStats GetPlacementStats(List<Vector3> positions, AlignmentSettings settings)
        {
            PlacementStats stats = new PlacementStats();
            
            if (positions.Count == 0)
                return stats;
            
            stats.totalObjects = positions.Count;
            
            // 平均距離を計算
            float totalDistance = 0f;
            int distanceCount = 0;
            
            for (int i = 0; i < positions.Count; i++)
            {
                for (int j = i + 1; j < positions.Count; j++)
                {
                    totalDistance += Vector3.Distance(positions[i], positions[j]);
                    distanceCount++;
                }
            }
            
            stats.averageDistance = distanceCount > 0 ? totalDistance / distanceCount : 0f;
            stats.minDistance = settings.minDistanceBetweenObjects;
            
            return stats;
        }

        [System.Serializable]
        public struct PlacementStats
        {
            public int totalObjects;
            public float averageDistance;
            public float minDistance;
        }
        #endregion
    }
}