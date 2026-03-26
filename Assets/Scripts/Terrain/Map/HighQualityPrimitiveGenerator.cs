using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Collections.Generic;
using System.Linq;
using Vastcore.Core;
using Vastcore.Utilities;

#if DEFORM_AVAILABLE
using Deform;
#endif

namespace Vastcore.Generation
{
    /// <summary>
    /// 高品質プリミティブ生成システム
    /// 16種類全てのプリミティブを最高品質で生成
    /// </summary>
    public static partial class HighQualityPrimitiveGenerator
    {
        #region 品質設定
        [System.Serializable]
        public struct QualitySettings
        {
            [Header("メッシュ品質")]
            public int subdivisionLevel;       // 細分化レベル (0-5)
            public bool enableSmoothNormals;   // 滑らかな法線
            public bool enableUVUnwrapping;    // UV展開
            public float meshOptimization;     // メッシュ最適化レベル
            
            [Header("形状品質")]
            public bool enableAdvancedDeformation;  // 高度な変形
            public float detailIntensity;          // 詳細度
            public bool enableProceduralDetails;   // プロシージャル詳細
            public int complexityLevel;            // 複雑度レベル (1-5)
            
            [Header("Deform統合")]
            public bool enableDeformSystem;        // Deformシステム使用
#if DEFORM_AVAILABLE
            public VastcoreDeformManager.DeformQualityLevel deformQuality; // Deform品質レベル
#endif
            public bool enableGeologicalDeformation; // 地質学的変形
            public bool enableOrganicDeformation;   // 有機的変形
            public float deformIntensity;          // 変形強度 (0-1)
            
            [Header("物理品質")]
            public bool enablePreciseColliders;    // 精密コライダー
            public bool enableLODColliders;        // LOD対応コライダー
            public ColliderType colliderType;      // コライダータイプ
            
            public static QualitySettings High => new QualitySettings
            {
                subdivisionLevel = 3,
                enableSmoothNormals = true,
                enableUVUnwrapping = true,
                meshOptimization = 0.8f,
                enableAdvancedDeformation = true,
                detailIntensity = 0.7f,
                enableProceduralDetails = true,
                complexityLevel = 4,
                enableDeformSystem = true,
#if DEFORM_AVAILABLE
                deformQuality = VastcoreDeformManager.DeformQualityLevel.High,
#endif
                enableGeologicalDeformation = true,
                enableOrganicDeformation = true,
                deformIntensity = 0.8f,
                enablePreciseColliders = true,
                enableLODColliders = true,
                colliderType = ColliderType.Mesh
            };
            
            public static QualitySettings Medium => new QualitySettings
            {
                subdivisionLevel = 2,
                enableSmoothNormals = true,
                enableUVUnwrapping = true,
                meshOptimization = 0.6f,
                enableAdvancedDeformation = true,
                detailIntensity = 0.5f,
                enableProceduralDetails = true,
                complexityLevel = 3,
                enableDeformSystem = true,
#if DEFORM_AVAILABLE
                deformQuality = VastcoreDeformManager.DeformQualityLevel.Medium,
#endif
                enableGeologicalDeformation = true,
                enableOrganicDeformation = false,
                deformIntensity = 0.6f,
                enablePreciseColliders = true,
                enableLODColliders = true,
                colliderType = ColliderType.Mesh
            };
            
            public static QualitySettings Low => new QualitySettings
            {
                subdivisionLevel = 1,
                enableSmoothNormals = false,
                enableUVUnwrapping = false,
                meshOptimization = 0.4f,
                enableAdvancedDeformation = false,
                detailIntensity = 0.3f,
                enableProceduralDetails = false,
                complexityLevel = 2,
                enableDeformSystem = false,
#if DEFORM_AVAILABLE
                deformQuality = VastcoreDeformManager.DeformQualityLevel.Low,
#endif
                enableGeologicalDeformation = false,
                enableOrganicDeformation = false,
                deformIntensity = 0.3f,
                enablePreciseColliders = false,
                enableLODColliders = false,
                colliderType = ColliderType.Box
            };
        }
        
        public enum ColliderType
        {
            Box,
            Sphere,
            Capsule,
            Mesh,
            Convex
        }
        #endregion

        #region メイン生成関数
        /// <summary>
        /// 高品質プリミティブを生成
        /// </summary>
        public static GameObject GenerateHighQualityPrimitive(
            PrimitiveTerrainGenerator.PrimitiveType primitiveType,
            Vector3 position,
            Vector3 scale,
            QualitySettings quality = default)
        {
            if (quality.Equals(default(QualitySettings)))
                quality = QualitySettings.High;

            try
            {
                // Debug.Log($"Generating high-quality primitive: {primitiveType} at {position}");

                VastcoreLogger.Instance.LogInfo("PrimitiveGen", $"Generating high-quality primitive: {primitiveType} at {position}");
                
                // 基本パラメータを設定
                var parameters = PrimitiveTerrainGenerator.PrimitiveGenerationParams.Default(primitiveType);
                parameters.position = position;
                parameters.scale = scale;
                parameters.subdivisionLevel = quality.subdivisionLevel;
                parameters.enableDeformation = quality.enableAdvancedDeformation;
                parameters.noiseIntensity = quality.detailIntensity * 0.1f;

                // 高品質メッシュを生成
                ProBuilderMesh highQualityMesh = GenerateHighQualityMesh(primitiveType, scale, quality);
                
                if (highQualityMesh == null)
                {
                    VastcoreLogger.Instance.LogError("PrimitiveGen", $"Failed to generate high-quality mesh for {primitiveType}");
                    return null;
                }
                // GameObjectを設定
                GameObject primitiveObject = highQualityMesh.gameObject;
                primitiveObject.name = $"HQ_{primitiveType}_{System.Guid.NewGuid().ToString("N")[..8]}";
                primitiveObject.transform.position = position;

                // PrimitiveTerrainObjectコンポーネントを追加して初期化
        var pto = primitiveObject.AddComponent<PrimitiveTerrainObject>();
        pto.InitializeFromPool((GenerationPrimitiveType)(int)primitiveType, position, scale);

        // 高品質処理を適用
        ApplyHighQualityProcessing(highQualityMesh, quality);
                
                // 詳細を追加
                if (quality.enableProceduralDetails)
                {
                    AddProceduralDetails(primitiveObject, primitiveType, quality);
                }

                // 高品質コライダーを設定
                SetupHighQualityColliders(primitiveObject, quality);

                // 最終的なメッシュ処理
                FinalizeHighQualityMesh(highQualityMesh, quality);

                // 品質検証
                if (!ValidatePrimitiveQuality(primitiveObject, primitiveType, quality))
                {
                    VastcoreLogger.Instance.LogWarning("PrimitiveGen", $"Quality validation failed for {primitiveType}");
                }

                VastcoreLogger.Instance.LogInfo("PrimitiveGen", $"Successfully generated high-quality {primitiveType}");
                return primitiveObject;
            }
            catch (System.Exception e)
            {
                VastcoreLogger.Instance.LogError("PrimitiveGen", $"Error generating high-quality primitive {primitiveType}: {e.Message}", e);
                return null;
            }
        }
        #endregion

        #region ユーティリティ
        /// <summary>
        /// 全16種類のプリミティブ品質テスト
        /// </summary>
        public static bool TestAllPrimitiveQuality(QualitySettings quality = default)
        {
            if (quality.Equals(default(QualitySettings)))
                quality = QualitySettings.High;

            bool allPassed = true;
            var primitiveTypes = System.Enum.GetValues(typeof(PrimitiveTerrainGenerator.PrimitiveType));
            
            VastcoreLogger.Instance.LogInfo("PrimitiveGen", "Starting comprehensive primitive quality test...");
            
            foreach (PrimitiveTerrainGenerator.PrimitiveType primitiveType in primitiveTypes)
            {
                try
                {
                    var testObject = GenerateHighQualityPrimitive(
                        primitiveType,
                        Vector3.zero,
                        Vector3.one * 100f,
                        quality
                    );
                    
                    if (testObject == null)
                    {
                        VastcoreLogger.Instance.LogError("PrimitiveGen", $"Failed to generate {primitiveType}");
                        allPassed = false;
                        continue;
                    }
                    
                    bool passed = ValidatePrimitiveQuality(testObject, primitiveType, quality);
                    if (!passed)
                    {
                        VastcoreLogger.Instance.LogError("PrimitiveGen", $"Quality validation failed for {primitiveType}");
                        allPassed = false;
                    }
                    else
                    {
                        VastcoreLogger.Instance.LogInfo("PrimitiveGen", $"✓ {primitiveType} passed quality test");
                    }
                    
                    // テストオブジェクトを削除
                    UnityEngine.Object.DestroyImmediate(testObject);
                }
                catch (System.Exception e)
                {
                    VastcoreLogger.Instance.LogError("PrimitiveGen", $"Exception testing {primitiveType}: {e.Message}", e);
                    allPassed = false;
                }
            }
            
            VastcoreLogger.Instance.LogInfo("PrimitiveGen", $"Primitive quality test completed. Result: {(allPassed ? "PASSED" : "FAILED")}");
            return allPassed;
        }

        #region Deform統合機能
    }
}
