using UnityEngine;
using UnityEngine.ProBuilder;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Generation
{
    /// <summary>
    /// プリミティブ品質検証システム
    /// 16種類全てのプリミティブが高品質で生成されることを保証
    /// </summary>
    public static class PrimitiveQualityValidator
    {
        #region 品質基準定義
        [System.Serializable]
        public struct QualityStandards
        {
            [Header("メッシュ品質基準")]
            public int minVertexCount;          // 最小頂点数
            public int maxVertexCount;          // 最大頂点数
            public float minSurfaceArea;        // 最小表面積
            public float maxAspectRatio;        // 最大アスペクト比
            
            [Header("形状品質基準")]
            public float minSymmetryScore;      // 最小対称性スコア
            public float maxDeformationError;   // 最大変形エラー
            public bool requiresClosedMesh;     // 閉じたメッシュが必要
            public bool requiresManifoldMesh;   // 多様体メッシュが必要
            
            [Header("物理品質基準")]
            public bool requiresCollider;       // コライダーが必要
            public float minColliderAccuracy;   // 最小コライダー精度
            public bool requiresRigidbody;      // リジッドボディが必要
            
            public static QualityStandards High => new QualityStandards
            {
                minVertexCount = 24,
                maxVertexCount = 5000,
                minSurfaceArea = 100f,
                maxAspectRatio = 10f,
                minSymmetryScore = 0.8f,
                maxDeformationError = 0.1f,
                requiresClosedMesh = true,
                requiresManifoldMesh = true,
                requiresCollider = true,
                minColliderAccuracy = 0.9f,
                requiresRigidbody = false
            };
            
            public static QualityStandards Medium => new QualityStandards
            {
                minVertexCount = 12,
                maxVertexCount = 2000,
                minSurfaceArea = 50f,
                maxAspectRatio = 20f,
                minSymmetryScore = 0.6f,
                maxDeformationError = 0.2f,
                requiresClosedMesh = true,
                requiresManifoldMesh = false,
                requiresCollider = true,
                minColliderAccuracy = 0.7f,
                requiresRigidbody = false
            };
        }
        
        [System.Serializable]
        public struct QualityReport
        {
            public PrimitiveTerrainGenerator.PrimitiveType primitiveType;
            public bool passedValidation;
            public float overallScore;
            public Dictionary<string, float> categoryScores;
            public List<string> issues;
            public List<string> recommendations;
            
            public QualityReport(PrimitiveTerrainGenerator.PrimitiveType type)
            {
                primitiveType = type;
                passedValidation = false;
                overallScore = 0f;
                categoryScores = new Dictionary<string, float>();
                issues = new List<string>();
                recommendations = new List<string>();
            }
        }
        #endregion

        #region メイン検証関数
        /// <summary>
        /// プリミティブの品質を検証
        /// </summary>
        public static QualityReport ValidatePrimitiveQuality(
            GameObject primitiveObject, 
            PrimitiveTerrainGenerator.PrimitiveType primitiveType,
            QualityStandards standards = default)
        {
            if (standards.Equals(default(QualityStandards)))
                standards = QualityStandards.High;

            var report = new QualityReport(primitiveType);
            
            try
            {
                Debug.Log($"Validating quality for primitive: {primitiveType}");
                
                // メッシュ品質を検証
                float meshScore = ValidateMeshQuality(primitiveObject, standards, report);
                report.categoryScores["Mesh"] = meshScore;
                
                // 形状品質を検証
                float shapeScore = ValidateShapeQuality(primitiveObject, primitiveType, standards, report);
                report.categoryScores["Shape"] = shapeScore;
                
                // 物理品質を検証
                float physicsScore = ValidatePhysicsQuality(primitiveObject, standards, report);
                report.categoryScores["Physics"] = physicsScore;
                
                // インタラクション品質を検証
                float interactionScore = ValidateInteractionQuality(primitiveObject, primitiveType, report);
                report.categoryScores["Interaction"] = interactionScore;
                
                // 全体スコアを計算
                report.overallScore = (meshScore + shapeScore + physicsScore + interactionScore) / 4f;
                report.passedValidation = report.overallScore >= 0.7f && report.issues.Count == 0;
                
                // 推奨事項を生成
                GenerateRecommendations(report);
                
                Debug.Log($"Quality validation completed for {primitiveType}: Score={report.overallScore:F2}, Passed={report.passedValidation}");
                return report;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error validating primitive quality {primitiveType}: {e.Message}");
                report.issues.Add($"Validation error: {e.Message}");
                return report;
            }
        }

        /// <summary>
        /// 全16種類のプリミティブ品質を一括検証
        /// </summary>
        public static Dictionary<PrimitiveTerrainGenerator.PrimitiveType, QualityReport> ValidateAllPrimitives(
            Vector3 testPosition = default,
            Vector3 testScale = default,
            QualityStandards standards = default)
        {
            if (testPosition == default) testPosition = Vector3.zero;
            if (testScale == default) testScale = Vector3.one * 100f;
            if (standards.Equals(default(QualityStandards))) standards = QualityStandards.High;

            var results = new Dictionary<PrimitiveTerrainGenerator.PrimitiveType, QualityReport>();
            var allTypes = System.Enum.GetValues(typeof(PrimitiveTerrainGenerator.PrimitiveType))
                                .Cast<PrimitiveTerrainGenerator.PrimitiveType>();

            Debug.Log("Starting comprehensive quality validation for all 16 primitive types");

            foreach (var primitiveType in allTypes)
            {
                try
                {
                    // プリミティブを生成
                    var primitiveObject = HighQualityPrimitiveGenerator.GenerateHighQualityPrimitive(
                        primitiveType, 
                        testPosition + Vector3.right * results.Count * 200f, // 間隔を空けて配置
                        testScale
                    );

                    if (primitiveObject != null)
                    {
                        // 品質を検証
                        var report = ValidatePrimitiveQuality(primitiveObject, primitiveType, standards);
                        results[primitiveType] = report;
                        
                        // テスト用オブジェクトを削除
                        Object.DestroyImmediate(primitiveObject);
                    }
                    else
                    {
                        var failedReport = new QualityReport(primitiveType);
                        failedReport.issues.Add("Failed to generate primitive");
                        results[primitiveType] = failedReport;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error testing primitive {primitiveType}: {e.Message}");
                    var errorReport = new QualityReport(primitiveType);
                    errorReport.issues.Add($"Generation error: {e.Message}");
                    results[primitiveType] = errorReport;
                }
            }

            // 結果をログ出力
            LogValidationResults(results);
            
            return results;
        }
        #endregion

        #region 個別品質検証
        /// <summary>
        /// メッシュ品質を検証
        /// </summary>
        private static float ValidateMeshQuality(GameObject primitiveObject, QualityStandards standards, QualityReport report)
        {
            float score = 1.0f;
            
            var meshFilter = primitiveObject.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                report.issues.Add("Missing mesh or MeshFilter component");
                return 0f;
            }

            var mesh = meshFilter.sharedMesh;
            
            // 頂点数チェック
            int vertexCount = mesh.vertexCount;
            if (vertexCount < standards.minVertexCount)
            {
                report.issues.Add($"Insufficient vertex count: {vertexCount} < {standards.minVertexCount}");
                score *= 0.5f;
            }
            else if (vertexCount > standards.maxVertexCount)
            {
                report.issues.Add($"Excessive vertex count: {vertexCount} > {standards.maxVertexCount}");
                score *= 0.8f;
            }
            
            // 表面積チェック
            float surfaceArea = CalculateSurfaceArea(mesh);
            if (surfaceArea < standards.minSurfaceArea)
            {
                report.issues.Add($"Insufficient surface area: {surfaceArea:F2} < {standards.minSurfaceArea}");
                score *= 0.7f;
            }
            
            // 閉じたメッシュチェック
            if (standards.requiresClosedMesh && !IsClosedMesh(mesh))
            {
                report.issues.Add("Mesh is not closed (has holes or open edges)");
                score *= 0.6f;
            }
            
            // 多様体メッシュチェック
            if (standards.requiresManifoldMesh && !IsManifoldMesh(mesh))
            {
                report.issues.Add("Mesh is not manifold (has non-manifold edges)");
                score *= 0.8f;
            }
            
            // 法線チェック
            if (mesh.normals == null || mesh.normals.Length == 0)
            {
                report.issues.Add("Missing mesh normals");
                score *= 0.9f;
            }
            
            return Mathf.Clamp01(score);
        }

        /// <summary>
        /// 形状品質を検証
        /// </summary>
        private static float ValidateShapeQuality(GameObject primitiveObject, PrimitiveTerrainGenerator.PrimitiveType primitiveType, QualityStandards standards, QualityReport report)
        {
            float score = 1.0f;
            
            var meshFilter = primitiveObject.GetComponent<MeshFilter>();
            if (meshFilter?.sharedMesh == null) return 0f;
            
            var mesh = meshFilter.sharedMesh;
            var bounds = mesh.bounds;
            
            // アスペクト比チェック
            float aspectRatio = CalculateAspectRatio(bounds);
            if (aspectRatio > standards.maxAspectRatio)
            {
                report.issues.Add($"Excessive aspect ratio: {aspectRatio:F2} > {standards.maxAspectRatio}");
                score *= 0.8f;
            }
            
            // プリミティブタイプ固有の形状検証
            score *= ValidateTypeSpecificShape(mesh, primitiveType, report);
            
            // 対称性チェック
            float symmetryScore = CalculateSymmetryScore(mesh, primitiveType);
            if (symmetryScore < standards.minSymmetryScore)
            {
                report.issues.Add($"Poor symmetry: {symmetryScore:F2} < {standards.minSymmetryScore}");
                score *= 0.9f;
            }
            
            return Mathf.Clamp01(score);
        }

        /// <summary>
        /// 物理品質を検証
        /// </summary>
        private static float ValidatePhysicsQuality(GameObject primitiveObject, QualityStandards standards, QualityReport report)
        {
            float score = 1.0f;
            
            // コライダーチェック
            var collider = primitiveObject.GetComponent<Collider>();
            if (standards.requiresCollider)
            {
                if (collider == null)
                {
                    report.issues.Add("Missing required collider component");
                    score *= 0.5f;
                }
                else
                {
                    // コライダー精度チェック
                    float accuracy = CalculateColliderAccuracy(primitiveObject);
                    if (accuracy < standards.minColliderAccuracy)
                    {
                        report.issues.Add($"Poor collider accuracy: {accuracy:F2} < {standards.minColliderAccuracy}");
                        score *= 0.8f;
                    }
                }
            }
            
            // リジッドボディチェック
            var rigidbody = primitiveObject.GetComponent<Rigidbody>();
            if (standards.requiresRigidbody && rigidbody == null)
            {
                report.issues.Add("Missing required Rigidbody component");
                score *= 0.9f;
            }
            
            return Mathf.Clamp01(score);
        }

        /// <summary>
        /// インタラクション品質を検証
        /// </summary>
        private static float ValidateInteractionQuality(GameObject primitiveObject, PrimitiveTerrainGenerator.PrimitiveType primitiveType, QualityReport report)
        {
            float score = 1.0f;
            
            // PrimitiveTerrainObjectコンポーネントチェック
            var primitiveComponent = primitiveObject.GetComponent<PrimitiveTerrainObject>();
            if (primitiveComponent == null)
            {
                report.issues.Add("Missing PrimitiveTerrainObject component");
                score *= 0.8f;
            }
            else
            {
                // インタラクション設定の妥当性チェック
                if (!ValidateInteractionSettings(primitiveComponent, primitiveType))
                {
                    report.issues.Add("Invalid interaction settings for primitive type");
                    score *= 0.9f;
                }
            }
            
            // レイヤー設定チェック
            if (primitiveObject.layer == 0) // Default layer
            {
                report.issues.Add("Using default layer - should use dedicated terrain layer");
                score *= 0.95f;
            }
            
            return Mathf.Clamp01(score);
        }
        #endregion

        #region ユーティリティ関数
        /// <summary>
        /// 表面積を計算
        /// </summary>
        private static float CalculateSurfaceArea(Mesh mesh)
        {
            float totalArea = 0f;
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v1 = vertices[triangles[i]];
                Vector3 v2 = vertices[triangles[i + 1]];
                Vector3 v3 = vertices[triangles[i + 2]];
                
                float area = Vector3.Cross(v2 - v1, v3 - v1).magnitude * 0.5f;
                totalArea += area;
            }
            
            return totalArea;
        }

        /// <summary>
        /// アスペクト比を計算
        /// </summary>
        private static float CalculateAspectRatio(Bounds bounds)
        {
            var size = bounds.size;
            float max = Mathf.Max(size.x, size.y, size.z);
            float min = Mathf.Min(size.x, size.y, size.z);
            return min > 0 ? max / min : float.MaxValue;
        }

        /// <summary>
        /// 閉じたメッシュかどうかを判定
        /// </summary>
        private static bool IsClosedMesh(Mesh mesh)
        {
            // 簡易実装：境界エッジの検出
            var triangles = mesh.triangles;
            var edgeCount = new Dictionary<(int, int), int>();
            
            for (int i = 0; i < triangles.Length; i += 3)
            {
                for (int j = 0; j < 3; j++)
                {
                    int v1 = triangles[i + j];
                    int v2 = triangles[i + (j + 1) % 3];
                    
                    var edge = v1 < v2 ? (v1, v2) : (v2, v1);
                    edgeCount[edge] = edgeCount.GetValueOrDefault(edge, 0) + 1;
                }
            }
            
            // 境界エッジ（1回しか使われていないエッジ）があるかチェック
            return !edgeCount.Values.Any(count => count == 1);
        }

        /// <summary>
        /// 多様体メッシュかどうかを判定
        /// </summary>
        private static bool IsManifoldMesh(Mesh mesh)
        {
            // 簡易実装：各エッジが最大2つの面で共有されているかチェック
            var triangles = mesh.triangles;
            var edgeCount = new Dictionary<(int, int), int>();
            
            for (int i = 0; i < triangles.Length; i += 3)
            {
                for (int j = 0; j < 3; j++)
                {
                    int v1 = triangles[i + j];
                    int v2 = triangles[i + (j + 1) % 3];
                    
                    var edge = v1 < v2 ? (v1, v2) : (v2, v1);
                    edgeCount[edge] = edgeCount.GetValueOrDefault(edge, 0) + 1;
                }
            }
            
            // エッジが3回以上使われている場合は非多様体
            return !edgeCount.Values.Any(count => count > 2);
        }

        /// <summary>
        /// プリミティブタイプ固有の形状を検証
        /// </summary>
        private static float ValidateTypeSpecificShape(Mesh mesh, PrimitiveTerrainGenerator.PrimitiveType primitiveType, QualityReport report)
        {
            switch (primitiveType)
            {
                case PrimitiveTerrainGenerator.PrimitiveType.Sphere:
                    return ValidateSphereShape(mesh, report);
                case PrimitiveTerrainGenerator.PrimitiveType.Cube:
                    return ValidateCubeShape(mesh, report);
                case PrimitiveTerrainGenerator.PrimitiveType.Cylinder:
                    return ValidateCylinderShape(mesh, report);
                default:
                    return 1.0f; // 他のタイプは基本検証のみ
            }
        }

        /// <summary>
        /// 球体形状を検証
        /// </summary>
        private static float ValidateSphereShape(Mesh mesh, QualityReport report)
        {
            var vertices = mesh.vertices;
            var center = mesh.bounds.center;
            var expectedRadius = mesh.bounds.size.magnitude * 0.5f;
            
            float totalDeviation = 0f;
            foreach (var vertex in vertices)
            {
                float distance = Vector3.Distance(vertex, center);
                float deviation = Mathf.Abs(distance - expectedRadius) / expectedRadius;
                totalDeviation += deviation;
            }
            
            float averageDeviation = totalDeviation / vertices.Length;
            if (averageDeviation > 0.2f)
            {
                report.issues.Add($"Poor sphere shape quality: average deviation {averageDeviation:F3}");
                return 0.7f;
            }
            
            return 1.0f;
        }

        /// <summary>
        /// 立方体形状を検証
        /// </summary>
        private static float ValidateCubeShape(Mesh mesh, QualityReport report)
        {
            var bounds = mesh.bounds;
            var size = bounds.size;
            
            // 立方体は各辺がほぼ等しいはず
            float maxDifference = Mathf.Max(
                Mathf.Abs(size.x - size.y),
                Mathf.Abs(size.y - size.z),
                Mathf.Abs(size.z - size.x)
            );
            
            float relativeDifference = maxDifference / size.magnitude;
            if (relativeDifference > 0.1f)
            {
                report.issues.Add($"Poor cube proportions: relative difference {relativeDifference:F3}");
                return 0.8f;
            }
            
            return 1.0f;
        }

        /// <summary>
        /// 円柱形状を検証
        /// </summary>
        private static float ValidateCylinderShape(Mesh mesh, QualityReport report)
        {
            var bounds = mesh.bounds;
            var size = bounds.size;
            
            // 円柱は2つの軸が等しく、1つが異なるはず
            float[] dimensions = { size.x, size.y, size.z };
            System.Array.Sort(dimensions);
            
            if (Mathf.Abs(dimensions[0] - dimensions[1]) > dimensions[0] * 0.1f)
            {
                report.issues.Add("Poor cylinder proportions: base dimensions not equal");
                return 0.8f;
            }
            
            return 1.0f;
        }

        /// <summary>
        /// 対称性スコアを計算
        /// </summary>
        private static float CalculateSymmetryScore(Mesh mesh, PrimitiveTerrainGenerator.PrimitiveType primitiveType)
        {
            // 簡易実装：境界ボックスの対称性をチェック
            var bounds = mesh.bounds;
            var center = bounds.center;
            var vertices = mesh.vertices;
            
            float symmetryScore = 0f;
            int symmetryTests = 0;
            
            // X軸対称性
            if (ShouldTestAxisSymmetry(primitiveType, 0))
            {
                symmetryScore += CalculateAxisSymmetry(vertices, center, Vector3.right);
                symmetryTests++;
            }
            
            // Y軸対称性
            if (ShouldTestAxisSymmetry(primitiveType, 1))
            {
                symmetryScore += CalculateAxisSymmetry(vertices, center, Vector3.up);
                symmetryTests++;
            }
            
            // Z軸対称性
            if (ShouldTestAxisSymmetry(primitiveType, 2))
            {
                symmetryScore += CalculateAxisSymmetry(vertices, center, Vector3.forward);
                symmetryTests++;
            }
            
            return symmetryTests > 0 ? symmetryScore / symmetryTests : 1.0f;
        }

        /// <summary>
        /// 軸対称性をテストすべきかどうかを判定
        /// </summary>
        private static bool ShouldTestAxisSymmetry(PrimitiveTerrainGenerator.PrimitiveType primitiveType, int axis)
        {
            switch (primitiveType)
            {
                case PrimitiveTerrainGenerator.PrimitiveType.Sphere:
                case PrimitiveTerrainGenerator.PrimitiveType.Cube:
                    return true; // 全軸対称
                case PrimitiveTerrainGenerator.PrimitiveType.Cylinder:
                case PrimitiveTerrainGenerator.PrimitiveType.Cone:
                    return axis != 1; // Y軸以外対称
                case PrimitiveTerrainGenerator.PrimitiveType.Monolith:
                case PrimitiveTerrainGenerator.PrimitiveType.Spire:
                    return axis != 1; // Y軸以外対称
                default:
                    return axis == 0 || axis == 2; // XZ対称のみ
            }
        }

        /// <summary>
        /// 軸対称性を計算
        /// </summary>
        private static float CalculateAxisSymmetry(Vector3[] vertices, Vector3 center, Vector3 axis)
        {
            float symmetryScore = 0f;
            int validPairs = 0;
            
            foreach (var vertex in vertices)
            {
                Vector3 relativePos = vertex - center;
                Vector3 mirroredPos = relativePos - 2f * Vector3.Dot(relativePos, axis) * axis;
                Vector3 expectedVertex = center + mirroredPos;
                
                // 最も近い頂点を見つける
                float minDistance = float.MaxValue;
                foreach (var otherVertex in vertices)
                {
                    float distance = Vector3.Distance(otherVertex, expectedVertex);
                    minDistance = Mathf.Min(minDistance, distance);
                }
                
                // 対称性スコアを計算
                float tolerance = 0.1f;
                if (minDistance < tolerance)
                {
                    symmetryScore += 1f - (minDistance / tolerance);
                    validPairs++;
                }
            }
            
            return validPairs > 0 ? symmetryScore / validPairs : 0f;
        }

        /// <summary>
        /// コライダー精度を計算
        /// </summary>
        private static float CalculateColliderAccuracy(GameObject primitiveObject)
        {
            var meshFilter = primitiveObject.GetComponent<MeshFilter>();
            var collider = primitiveObject.GetComponent<Collider>();
            
            if (meshFilter?.sharedMesh == null || collider == null)
                return 0f;
            
            // 境界ボックスの比較による簡易精度計算
            var meshBounds = meshFilter.sharedMesh.bounds;
            var colliderBounds = collider.bounds;
            
            float volumeRatio = (colliderBounds.size.x * colliderBounds.size.y * colliderBounds.size.z) /
                               (meshBounds.size.x * meshBounds.size.y * meshBounds.size.z);
            
            return Mathf.Clamp01(1f - Mathf.Abs(1f - volumeRatio));
        }

        /// <summary>
        /// インタラクション設定の妥当性を検証
        /// </summary>
        private static bool ValidateInteractionSettings(PrimitiveTerrainObject primitiveComponent, PrimitiveTerrainGenerator.PrimitiveType primitiveType)
        {
            // プリミティブタイプに応じた適切なインタラクション設定をチェック
            switch (primitiveType)
            {
                case PrimitiveTerrainGenerator.PrimitiveType.Sphere:
                case PrimitiveTerrainGenerator.PrimitiveType.Boulder:
                    return primitiveComponent.isClimbable; // 球体は登れるべき
                    
                case PrimitiveTerrainGenerator.PrimitiveType.Ring:
                case PrimitiveTerrainGenerator.PrimitiveType.Torus:
                    return primitiveComponent.isGrindable; // リング系はグラインド可能であるべき
                    
                case PrimitiveTerrainGenerator.PrimitiveType.Mesa:
                case PrimitiveTerrainGenerator.PrimitiveType.Formation:
                    return primitiveComponent.isClimbable && primitiveComponent.hasCollision; // 台地系は登れて衝突判定があるべき
                    
                default:
                    return true; // 他のタイプは基本設定で OK
            }
        }

        /// <summary>
        /// 推奨事項を生成
        /// </summary>
        private static void GenerateRecommendations(QualityReport report)
        {
            if (report.categoryScores.ContainsKey("Mesh") && report.categoryScores["Mesh"] < 0.8f)
            {
                report.recommendations.Add("Consider increasing subdivision level for better mesh quality");
            }
            
            if (report.categoryScores.ContainsKey("Shape") && report.categoryScores["Shape"] < 0.8f)
            {
                report.recommendations.Add("Review shape generation algorithm for better geometric accuracy");
            }
            
            if (report.categoryScores.ContainsKey("Physics") && report.categoryScores["Physics"] < 0.8f)
            {
                report.recommendations.Add("Improve collider setup for better physics interaction");
            }
            
            if (report.issues.Count > 0)
            {
                report.recommendations.Add("Address all identified issues to improve overall quality");
            }
        }

        /// <summary>
        /// 検証結果をログ出力
        /// </summary>
        private static void LogValidationResults(Dictionary<PrimitiveTerrainGenerator.PrimitiveType, QualityReport> results)
        {
            Debug.Log("=== Primitive Quality Validation Results ===");
            
            int passedCount = 0;
            int totalCount = results.Count;
            
            foreach (var kvp in results)
            {
                var type = kvp.Key;
                var report = kvp.Value;
                
                string status = report.passedValidation ? "PASS" : "FAIL";
                Debug.Log($"{type}: {status} (Score: {report.overallScore:F2})");
                
                if (report.passedValidation)
                    passedCount++;
                
                if (report.issues.Count > 0)
                {
                    Debug.LogWarning($"  Issues: {string.Join(", ", report.issues)}");
                }
            }
            
            Debug.Log($"Overall Results: {passedCount}/{totalCount} primitives passed validation ({(float)passedCount/totalCount*100:F1}%)");
            
            if (passedCount == totalCount)
            {
                Debug.Log("✅ All 16 primitive types are generating with high quality!");
            }
            else
            {
                Debug.LogWarning($"⚠️ {totalCount - passedCount} primitive types need quality improvements");
            }
        }
        #endregion
    }
}