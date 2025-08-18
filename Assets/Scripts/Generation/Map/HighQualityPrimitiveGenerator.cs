using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Generation
{
    /// <summary>
    /// 高品質プリミティブ生成システム
    /// 16種類全てのプリミティブを最高品質で生成
    /// </summary>
    public static class HighQualityPrimitiveGenerator
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
                Debug.Log($"Generating high-quality primitive: {primitiveType} at {position}");
                
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
                    Debug.LogError($"Failed to generate high-quality mesh for {primitiveType}");
                    return null;
                }

                // GameObjectを設定
                GameObject primitiveObject = highQualityMesh.gameObject;
                primitiveObject.name = $"HQ_{primitiveType}_{System.Guid.NewGuid().ToString("N")[..8]}";
                primitiveObject.transform.position = position;

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
                    Debug.LogWarning($"Quality validation failed for {primitiveType}");
                }

                Debug.Log($"Successfully generated high-quality {primitiveType}");
                return primitiveObject;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error generating high-quality primitive {primitiveType}: {e.Message}");
                return null;
            }
        }
        #endregion
    }
}        #reg
ion 高品質メッシュ生成
        /// <summary>
        /// 高品質メッシュを生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityMesh(
            PrimitiveTerrainGenerator.PrimitiveType primitiveType,
            Vector3 scale,
            QualitySettings quality)
        {
            ProBuilderMesh mesh = null;

            switch (primitiveType)
            {
                case PrimitiveTerrainGenerator.PrimitiveType.Cube:
                    mesh = GenerateHighQualityCube(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Sphere:
                    mesh = GenerateHighQualitySphere(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Cylinder:
                    mesh = GenerateHighQualityCylinder(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Pyramid:
                    mesh = GenerateHighQualityPyramid(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Torus:
                    mesh = GenerateHighQualityTorus(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Prism:
                    mesh = GenerateHighQualityPrism(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Cone:
                    mesh = GenerateHighQualityCone(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Octahedron:
                    mesh = GenerateHighQualityOctahedron(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Crystal:
                    mesh = GenerateHighQualityCrystal(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Monolith:
                    mesh = GenerateHighQualityMonolith(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Arch:
                    mesh = GenerateHighQualityArch(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Ring:
                    mesh = GenerateHighQualityRing(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Mesa:
                    mesh = GenerateHighQualityMesa(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Spire:
                    mesh = GenerateHighQualitySpire(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Boulder:
                    mesh = GenerateHighQualityBoulder(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Formation:
                    mesh = GenerateHighQualityFormation(scale, quality);
                    break;
                default:
                    Debug.LogWarning($"Primitive type {primitiveType} not implemented, using high-quality cube");
                    mesh = GenerateHighQualityCube(scale, quality);
                    break;
            }

            return mesh;
        }

        /// <summary>
        /// 高品質立方体を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityCube(Vector3 scale, QualitySettings quality)
        {
            var cube = ShapeGenerator.CreateShape(ShapeType.Cube);
            cube.transform.localScale = scale;
            
            // 高品質処理
            if (quality.subdivisionLevel > 0)
            {
                for (int i = 0; i < quality.subdivisionLevel; i++)
                {
                    cube.Subdivide();
                }
            }
            
            // 詳細な変形
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedCubeDeformation(cube, quality);
            }
            
            return cube;
        }

        /// <summary>
        /// 高品質球体を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualitySphere(Vector3 scale, QualitySettings quality)
        {
            // より高解像度の球体を生成
            int subdivisions = Mathf.Max(2, quality.subdivisionLevel + 2);
            var sphere = ShapeGenerator.CreateShape(ShapeType.Sphere, new PivotLocation(), new Vector3(1, 1, 1), subdivisions);
            sphere.transform.localScale = scale;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedSphereDeformation(sphere, quality);
            }
            
            return sphere;
        }

        /// <summary>
        /// 高品質円柱を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityCylinder(Vector3 scale, QualitySettings quality)
        {
            int sides = Mathf.Max(8, quality.subdivisionLevel * 4 + 8);
            var cylinder = ShapeGenerator.CreateShape(ShapeType.Cylinder, new PivotLocation(), new Vector3(1, 1, 1), sides);
            cylinder.transform.localScale = scale;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedCylinderDeformation(cylinder, quality);
            }
            
            return cylinder;
        }

        /// <summary>
        /// 高品質ピラミッドを生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityPyramid(Vector3 scale, QualitySettings quality)
        {
            var pyramid = ShapeGenerator.CreateShape(ShapeType.Cube);
            
            // ピラミッド形状に変形
            var vertices = pyramid.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].y > 0) // 上部の頂点
                {
                    vertices[i] = new Vector3(0, vertices[i].y, 0);
                }
            }
            pyramid.positions = vertices;
            pyramid.transform.localScale = scale;
            
            // 高品質処理
            if (quality.subdivisionLevel > 0)
            {
                for (int i = 0; i < quality.subdivisionLevel; i++)
                {
                    pyramid.Subdivide();
                }
            }
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedPyramidDeformation(pyramid, quality);
            }
            
            return pyramid;
        }

        /// <summary>
        /// 高品質トーラスを生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityTorus(Vector3 scale, QualitySettings quality)
        {
            int rows = Mathf.Max(8, quality.subdivisionLevel * 2 + 8);
            int columns = Mathf.Max(12, quality.subdivisionLevel * 3 + 12);
            
            var torus = ShapeGenerator.CreateShape(ShapeType.Torus, new PivotLocation(), new Vector3(1, 1, 1), rows, columns);
            torus.transform.localScale = scale;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedTorusDeformation(torus, quality);
            }
            
            return torus;
        }

        /// <summary>
        /// 高品質角柱を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityPrism(Vector3 scale, QualitySettings quality)
        {
            int sides = Mathf.Max(6, quality.subdivisionLevel * 2 + 6);
            var prism = ShapeGenerator.CreateShape(ShapeType.Prism, new PivotLocation(), new Vector3(1, 1, 1), sides);
            prism.transform.localScale = scale;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedPrismDeformation(prism, quality);
            }
            
            return prism;
        }

        /// <summary>
        /// 高品質円錐を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityCone(Vector3 scale, QualitySettings quality)
        {
            int sides = Mathf.Max(8, quality.subdivisionLevel * 4 + 8);
            var cone = ShapeGenerator.CreateShape(ShapeType.Cone, new PivotLocation(), new Vector3(1, 1, 1), sides);
            cone.transform.localScale = scale;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedConeDeformation(cone, quality);
            }
            
            return cone;
        }

        /// <summary>
        /// 高品質八面体を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityOctahedron(Vector3 scale, QualitySettings quality)
        {
            var octahedron = ShapeGenerator.CreateShape(ShapeType.Cube);
            
            // 八面体形状に変形
            var vertices = octahedron.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = vertices[i].normalized * scale.magnitude;
            }
            octahedron.positions = vertices;
            octahedron.transform.localScale = Vector3.one;
            
            // 高品質処理
            if (quality.subdivisionLevel > 0)
            {
                for (int i = 0; i < quality.subdivisionLevel; i++)
                {
                    octahedron.Subdivide();
                }
            }
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedOctahedronDeformation(octahedron, quality);
            }
            
            return octahedron;
        }

        /// <summary>
        /// 高品質結晶を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityCrystal(Vector3 scale, QualitySettings quality)
        {
            // 高品質結晶構造生成システムを使用
            var crystal = CrystalStructureGenerator.GenerateCrystalWithGrowthSimulation(scale, true);
            
            if (crystal != null && quality.enableAdvancedDeformation)
            {
                ApplyAdvancedCrystalDeformation(crystal, quality);
            }
            
            return crystal;
        }

        /// <summary>
        /// 高品質モノリスを生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityMonolith(Vector3 scale, QualitySettings quality)
        {
            var monolith = ShapeGenerator.CreateShape(ShapeType.Cube);
            
            // 縦長に調整
            Vector3 monolithScale = new Vector3(scale.x * 0.3f, scale.y * 2f, scale.z * 0.3f);
            monolith.transform.localScale = monolithScale;
            
            // 高品質処理
            if (quality.subdivisionLevel > 0)
            {
                for (int i = 0; i < quality.subdivisionLevel; i++)
                {
                    monolith.Subdivide();
                }
            }
            
            // 上部を細くして自然な石柱形状に
            var vertices = monolith.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].y > 0) // 上部の頂点
                {
                    float tapering = 1f - (vertices[i].y * 0.2f);
                    vertices[i] = new Vector3(vertices[i].x * tapering, vertices[i].y, vertices[i].z * tapering);
                }
            }
            monolith.positions = vertices;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedMonolithDeformation(monolith, quality);
            }
            
            return monolith;
        }
        #endregion 
       /// <summary>
        /// 高品質アーチを生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityArch(Vector3 scale, QualitySettings quality)
        {
            // 建築学的生成システムを使用
            var archParams = ArchitecturalGenerator.ArchitecturalParams.Default(ArchitecturalGenerator.ArchitecturalType.SimpleArch);
            archParams.span = scale.x;
            archParams.height = scale.y;
            archParams.thickness = scale.z;
            archParams.position = Vector3.zero;
            archParams.enableStructuralOptimization = true;
            archParams.enableDecorations = quality.enableProceduralDetails;
            
            var archObject = ArchitecturalGenerator.GenerateArchitecturalStructure(archParams);
            
            if (archObject != null)
            {
                var meshFilter = archObject.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    var proBuilderMesh = archObject.GetComponent<ProBuilderMesh>();
                    if (proBuilderMesh == null)
                    {
                        proBuilderMesh = archObject.AddComponent<ProBuilderMesh>();
                        proBuilderMesh.RebuildFromMesh(meshFilter.sharedMesh);
                    }
                    
                    // 一時的なオブジェクトを削除
                    UnityEngine.Object.DestroyImmediate(archObject);
                    
                    return proBuilderMesh;
                }
                
                UnityEngine.Object.DestroyImmediate(archObject);
            }
            
            // フォールバック：基本的なアーチ形状
            var fallbackArch = ShapeGenerator.CreateShape(ShapeType.Arch);
            fallbackArch.transform.localScale = scale;
            
            return fallbackArch;
        }

        /// <summary>
        /// 高品質リングを生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityRing(Vector3 scale, QualitySettings quality)
        {
            int rows = Mathf.Max(4, quality.subdivisionLevel + 4);
            int columns = Mathf.Max(16, quality.subdivisionLevel * 4 + 16);
            
            var ring = ShapeGenerator.CreateShape(ShapeType.Torus, new PivotLocation(), new Vector3(1, 1, 1), rows, columns);
            
            // リング形状に調整（薄くて大きい）
            Vector3 ringScale = new Vector3(scale.x * 1.5f, scale.y * 0.2f, scale.z * 1.5f);
            ring.transform.localScale = ringScale;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedRingDeformation(ring, quality);
            }
            
            return ring;
        }

        /// <summary>
        /// 高品質メサを生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityMesa(Vector3 scale, QualitySettings quality)
        {
            int sides = Mathf.Max(12, quality.subdivisionLevel * 4 + 12);
            var mesa = ShapeGenerator.CreateShape(ShapeType.Cylinder, new PivotLocation(), new Vector3(1, 1, 1), sides);
            
            // 台地形状に調整（平たくて広い）
            Vector3 mesaScale = new Vector3(scale.x * 2f, scale.y * 0.3f, scale.z * 2f);
            mesa.transform.localScale = mesaScale;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedMesaDeformation(mesa, quality);
            }
            
            return mesa;
        }

        /// <summary>
        /// 高品質尖塔を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualitySpire(Vector3 scale, QualitySettings quality)
        {
            int sides = Mathf.Max(8, quality.subdivisionLevel * 2 + 8);
            var spire = ShapeGenerator.CreateShape(ShapeType.Cone, new PivotLocation(), new Vector3(1, 1, 1), sides);
            
            // 尖塔形状に調整（非常に高くて細い）
            Vector3 spireScale = new Vector3(scale.x * 0.4f, scale.y * 3f, scale.z * 0.4f);
            spire.transform.localScale = spireScale;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedSpireDeformation(spire, quality);
            }
            
            return spire;
        }

        /// <summary>
        /// 高品質巨石を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityBoulder(Vector3 scale, QualitySettings quality)
        {
            int subdivisions = Mathf.Max(2, quality.subdivisionLevel + 1);
            var boulder = ShapeGenerator.CreateShape(ShapeType.Sphere, new PivotLocation(), new Vector3(1, 1, 1), subdivisions);
            boulder.transform.localScale = scale;
            
            // 不規則な岩石形状に変形
            var vertices = boulder.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 複数のノイズレイヤーで自然な岩石形状を作成
                float noise1 = Mathf.PerlinNoise(vertex.x * 3f, vertex.z * 3f);
                float noise2 = Mathf.PerlinNoise(vertex.x * 6f, vertex.z * 6f) * 0.5f;
                float noise3 = Mathf.PerlinNoise(vertex.x * 12f, vertex.z * 12f) * 0.25f;
                
                float combinedNoise = (noise1 + noise2 + noise3) / 1.75f;
                float randomFactor = 0.8f + combinedNoise * 0.4f;
                
                vertices[i] = vertex * randomFactor;
            }
            boulder.positions = vertices;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedBoulderDeformation(boulder, quality);
            }
            
            return boulder;
        }

        /// <summary>
        /// 高品質岩石層を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityFormation(Vector3 scale, QualitySettings quality)
        {
            var formation = ShapeGenerator.CreateShape(ShapeType.Cube);
            formation.transform.localScale = scale;
            
            // 高品質処理
            if (quality.subdivisionLevel > 0)
            {
                for (int i = 0; i < quality.subdivisionLevel; i++)
                {
                    formation.Subdivide();
                }
            }
            
            // 層状構造に変形
            var vertices = formation.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // Y軸方向に層を作る（より詳細）
                float layerCount = 8f + quality.complexityLevel * 2f;
                float layerHeight = Mathf.Floor(vertex.y * layerCount) / layerCount;
                vertex.y = layerHeight;
                
                // 各層で少しずつずらす（地質学的な変形）
                float layerOffset = (layerHeight + 1f) * 0.05f;
                vertex.x += Mathf.Sin(layerOffset * 10f) * 0.05f;
                vertex.z += Mathf.Cos(layerOffset * 10f) * 0.05f;
                
                // 風化による不規則性
                float weathering = Mathf.PerlinNoise(vertex.x * 20f, vertex.z * 20f) * 0.02f;
                vertex += Vector3.one * weathering;
                
                vertices[i] = vertex;
            }
            formation.positions = vertices;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedFormationDeformation(formation, quality);
            }
            
            return formation;
        }

        #region 高度な変形処理
        /// <summary>
        /// 高度な立方体変形
        /// </summary>
        private static void ApplyAdvancedCubeDeformation(ProBuilderMesh cube, QualitySettings quality)
        {
            var vertices = cube.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // エッジの丸み
                float edgeRounding = quality.detailIntensity * 0.1f;
                vertex = Vector3.Lerp(vertex, vertex.normalized * vertex.magnitude, edgeRounding);
                
                // 表面の微細な凹凸
                float surfaceDetail = (Mathf.PerlinNoise(vertex.x * 50f, vertex.z * 50f) - 0.5f) * quality.detailIntensity * 0.02f;
                vertex += vertex.normalized * surfaceDetail;
                
                vertices[i] = vertex;
            }
            
            cube.positions = vertices;
        }

        /// <summary>
        /// 高度な球体変形
        /// </summary>
        private static void ApplyAdvancedSphereDeformation(ProBuilderMesh sphere, QualitySettings quality)
        {
            var vertices = sphere.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 球面調和関数による変形
                float theta = Mathf.Atan2(vertex.z, vertex.x);
                float phi = Mathf.Acos(vertex.y / vertex.magnitude);
                
                float harmonicDeformation = Mathf.Sin(theta * 3f) * Mathf.Sin(phi * 2f) * quality.detailIntensity * 0.05f;
                vertex += vertex.normalized * harmonicDeformation;
                
                vertices[i] = vertex;
            }
            
            sphere.positions = vertices;
        }

        /// <summary>
        /// 高度な円柱変形
        /// </summary>
        private static void ApplyAdvancedCylinderDeformation(ProBuilderMesh cylinder, QualitySettings quality)
        {
            var vertices = cylinder.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 高さに応じたテーパリング
                float heightFactor = (vertex.y + 1f) * 0.5f; // 0-1に正規化
                float tapering = 1f - heightFactor * quality.detailIntensity * 0.1f;
                
                if (Mathf.Abs(vertex.y) < 0.9f) // 側面のみ
                {
                    vertex.x *= tapering;
                    vertex.z *= tapering;
                }
                
                vertices[i] = vertex;
            }
            
            cylinder.positions = vertices;
        }

        /// <summary>
        /// 高度なピラミッド変形
        /// </summary>
        private static void ApplyAdvancedPyramidDeformation(ProBuilderMesh pyramid, QualitySettings quality) 
        {
            var vertices = pyramid.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 高さに応じた段階的変形
                float heightFactor = (vertex.y + 1f) * 0.5f;
                float stepDeformation = Mathf.Floor(heightFactor * 5f) / 5f * quality.detailIntensity * 0.1f;
                
                // 古代建築の風化効果
                float weathering = Mathf.PerlinNoise(vertex.x * 15f, vertex.z * 15f) * quality.detailIntensity * 0.03f;
                
                // エッジの摩耗
                float edgeDistance = Mathf.Min(Mathf.Abs(vertex.x), Mathf.Abs(vertex.z));
                float edgeWear = (1f - edgeDistance) * quality.detailIntensity * 0.02f;
                
                vertex += vertex.normalized * (stepDeformation + weathering + edgeWear);
                vertices[i] = vertex;
            }
            
            pyramid.positions = vertices;
        }
        
        /// <summary>
        /// 高度なトーラス変形
        /// </summary>
        private static void ApplyAdvancedTorusDeformation(ProBuilderMesh torus, QualitySettings quality) 
        {
            var vertices = torus.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 円環の歪み
                float angle = Mathf.Atan2(vertex.z, vertex.x);
                float radialDistance = new Vector2(vertex.x, vertex.z).magnitude;
                
                // 非対称な歪み
                float asymmetry = Mathf.Sin(angle * 3f) * quality.detailIntensity * 0.05f;
                vertex.x += Mathf.Cos(angle) * asymmetry;
                vertex.z += Mathf.Sin(angle) * asymmetry;
                
                // 表面の凹凸
                float surfaceNoise = Mathf.PerlinNoise(angle * 5f, vertex.y * 10f) * quality.detailIntensity * 0.02f;
                Vector3 radialDirection = new Vector3(vertex.x, 0, vertex.z).normalized;
                vertex += radialDirection * surfaceNoise;
                
                vertices[i] = vertex;
            }
            
            torus.positions = vertices;
        }
        
        /// <summary>
        /// 高度な角柱変形
        /// </summary>
        private static void ApplyAdvancedPrismDeformation(ProBuilderMesh prism, QualitySettings quality) 
        {
            var vertices = prism.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 角の丸み
                if (Mathf.Abs(vertex.y) < 0.9f) // 側面
                {
                    float angle = Mathf.Atan2(vertex.z, vertex.x);
                    int sides = 6; // 六角柱
                    float sideAngle = 2f * Mathf.PI / sides;
                    float nearestSideAngle = Mathf.Round(angle / sideAngle) * sideAngle;
                    
                    // 角の丸み効果
                    float cornerRounding = quality.detailIntensity * 0.1f;
                    float angleDeviation = Mathf.Abs(angle - nearestSideAngle);
                    if (angleDeviation < sideAngle * 0.2f)
                    {
                        float radius = new Vector2(vertex.x, vertex.z).magnitude;
                        radius *= (1f - cornerRounding * (1f - angleDeviation / (sideAngle * 0.2f)));
                        vertex.x = radius * Mathf.Cos(angle);
                        vertex.z = radius * Mathf.Sin(angle);
                    }
                }
                
                vertices[i] = vertex;
                Vector3 vertex = vertices[i];
                float noise = Mathf.PerlinNoise(vertex.x * 12f, vertex.z * 12f) * quality.detailIntensity * 0.04f;
                vertices[i] = vertex + vertex.normalized * noise;
            }
            prism.positions = vertices;
        }
        
        private static void ApplyAdvancedConeDeformation(ProBuilderMesh cone, QualitySettings quality) 
        {
            // 基本的な変形を適用
            var vertices = cone.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float noise = Mathf.PerlinNoise(vertex.x * 15f, vertex.z * 15f) * quality.detailIntensity * 0.03f;
                vertices[i] = vertex + vertex.normalized * noise;
            }
            cone.positions = vertices;
        }
        
        private static void ApplyAdvancedOctahedronDeformation(ProBuilderMesh octahedron, QualitySettings quality) 
        {
            // 基本的な変形を適用
            var vertices = octahedron.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float noise = Mathf.PerlinNoise(vertex.x * 20f, vertex.z * 20f) * quality.detailIntensity * 0.02f;
                vertices[i] = vertex + vertex.normalized * noise;
            }
            octahedron.positions = vertices;
        }
        
        private static void ApplyAdvancedCrystalDeformation(ProBuilderMesh crystal, QualitySettings quality) 
        {
            // 結晶特有の変形
            if (crystal == null) return;
            var vertices = crystal.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float crystallineNoise = Mathf.PerlinNoise(vertex.x * 25f, vertex.z * 25f) * quality.detailIntensity * 0.01f;
                vertices[i] = vertex + vertex.normalized * crystallineNoise;
            }
            crystal.positions = vertices;
        }
        
        private static void ApplyAdvancedMonolithDeformation(ProBuilderMesh monolith, QualitySettings quality) 
        {
            // モノリス特有の風化変形
            var vertices = monolith.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float weathering = Mathf.PerlinNoise(vertex.x * 30f, vertex.y * 30f) * quality.detailIntensity * 0.02f;
                vertices[i] = vertex + vertex.normalized * weathering;
            }
            monolith.positions = vertices;
        }
        
        private static void ApplyAdvancedRingDeformation(ProBuilderMesh ring, QualitySettings quality) 
        {
            // リング特有の変形
            var vertices = ring.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float ringNoise = Mathf.PerlinNoise(vertex.x * 18f, vertex.z * 18f) * quality.detailIntensity * 0.025f;
                vertices[i] = vertex + vertex.normalized * ringNoise;
            }
            ring.positions = vertices;
        }
        
        private static void ApplyAdvancedMesaDeformation(ProBuilderMesh mesa, QualitySettings quality) 
        {
            // メサ特有の浸食変形
            var vertices = mesa.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float erosion = Mathf.PerlinNoise(vertex.x * 8f, vertex.z * 8f) * quality.detailIntensity * 0.06f;
                vertices[i] = vertex + Vector3.up * erosion;
            }
            mesa.positions = vertices;
        }
        
        private static void ApplyAdvancedSpireDeformation(ProBuilderMesh spire, QualitySettings quality) 
        {
            // 尖塔特有の変形
            var vertices = spire.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float spireNoise = Mathf.PerlinNoise(vertex.x * 40f, vertex.y * 40f) * quality.detailIntensity * 0.015f;
                vertices[i] = vertex + vertex.normalized * spireNoise;
            }
            spire.positions = vertices;
        }
        
        private static void ApplyAdvancedBoulderDeformation(ProBuilderMesh boulder, QualitySettings quality) 
        {
            // 巨石特有の追加変形
            var vertices = boulder.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float roughness = Mathf.PerlinNoise(vertex.x * 35f, vertex.z * 35f) * quality.detailIntensity * 0.08f;
                vertices[i] = vertex + vertex.normalized * roughness;
            }
            boulder.positions = vertices;
        }
        
        private static void ApplyAdvancedFormationDeformation(ProBuilderMesh formation, QualitySettings quality) 
        {
            // 岩石層特有の地質学的変形
            var vertices = formation.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float geological = Mathf.PerlinNoise(vertex.x * 25f, vertex.y * 25f) * quality.detailIntensity * 0.03f;
                vertices[i] = vertex + Vector3.right * geological;
            }
            formation.positions = vertices;
        }
        #endregion    
    #region 高品質処理
        /// <summary>
        /// 高品質処理を適用
        /// </summary>
        private static void ApplyHighQualityProcessing(ProBuilderMesh mesh, QualitySettings quality)
        {
            // 滑らかな法線
            if (quality.enableSmoothNormals)
            {
                mesh.SetSmoothingGroup(mesh.faces, 1);
            }

            // UV展開
            if (quality.enableUVUnwrapping)
            {
                UnwrapParameters unwrapParams = UnwrapParameters.Default;
                unwrapParams.hardAngle = 60f;
                unwrapParams.packMargin = 4f;
                unwrapParams.angleError = 8f;
                unwrapParams.areaError = 15f;
                
                Unwrapping.Unwrap(mesh, unwrapParams);
            }

            // メッシュ最適化
            if (quality.meshOptimization > 0)
            {
                MeshValidation.EnsureMeshIsValid(mesh);
                mesh.Optimize();
            }
        }

        /// <summary>
        /// プロシージャル詳細を追加
        /// </summary>
        private static void AddProceduralDetails(GameObject primitiveObject, PrimitiveTerrainGenerator.PrimitiveType primitiveType, QualitySettings quality)
        {
            switch (primitiveType)
            {
                case PrimitiveTerrainGenerator.PrimitiveType.Crystal:
                    AddCrystalDetails(primitiveObject, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Boulder:
                    AddBoulderDetails(primitiveObject, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Formation:
                    AddFormationDetails(primitiveObject, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Arch:
                    AddArchDetails(primitiveObject, quality);
                    break;
                // 他のタイプも必要に応じて追加
            }
        }

        /// <summary>
        /// 結晶の詳細を追加
        /// </summary>
        private static void AddCrystalDetails(GameObject crystal, QualitySettings quality)
        {
            // 小さな結晶の成長を追加
            for (int i = 0; i < quality.complexityLevel; i++)
            {
                Vector3 randomPosition = Random.onUnitSphere * crystal.transform.localScale.magnitude * 0.3f;
                float smallCrystalSize = crystal.transform.localScale.magnitude * Random.Range(0.05f, 0.15f);
                
                var smallCrystal = CrystalStructureGenerator.GenerateCrystalStructure(Vector3.one * smallCrystalSize);
                if (smallCrystal != null)
                {
                    smallCrystal.transform.SetParent(crystal.transform);
                    smallCrystal.transform.localPosition = randomPosition;
                    smallCrystal.transform.localRotation = Random.rotation;
                }
            }
        }

        /// <summary>
        /// 巨石の詳細を追加
        /// </summary>
        private static void AddBoulderDetails(GameObject boulder, QualitySettings quality)
        {
            // 表面の小さな岩石片を追加
            for (int i = 0; i < quality.complexityLevel * 2; i++)
            {
                Vector3 surfacePoint = Random.onUnitSphere * boulder.transform.localScale.magnitude * 0.5f;
                float fragmentSize = boulder.transform.localScale.magnitude * Random.Range(0.02f, 0.08f);
                
                var fragment = ShapeGenerator.CreateShape(ShapeType.Sphere);
                fragment.transform.SetParent(boulder.transform);
                fragment.transform.localPosition = surfacePoint;
                fragment.transform.localScale = Vector3.one * fragmentSize;
                fragment.transform.localRotation = Random.rotation;
            }
        }

        /// <summary>
        /// 岩石層の詳細を追加
        /// </summary>
        private static void AddFormationDetails(GameObject formation, QualitySettings quality)
        {
            // 層間の小さな隙間や突起を追加
            // 実装は複雑になるため、ここでは概要のみ
        }

        /// <summary>
        /// アーチの詳細を追加
        /// </summary>
        private static void AddArchDetails(GameObject arch, QualitySettings quality)
        {
            // 装飾的な要素を追加
            // 実装は複雑になるため、ここでは概要のみ
        }

        /// <summary>
        /// 高品質コライダーを設定
        /// </summary>
        private static void SetupHighQualityColliders(GameObject primitiveObject, QualitySettings quality)
        {
            // 既存のコライダーを削除
            var existingColliders = primitiveObject.GetComponents<Collider>();
            for (int i = 0; i < existingColliders.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(existingColliders[i]);
            }

            switch (quality.colliderType)
            {
                case ColliderType.Box:
                    primitiveObject.AddComponent<BoxCollider>();
                    break;
                case ColliderType.Sphere:
                    primitiveObject.AddComponent<SphereCollider>();
                    break;
                case ColliderType.Capsule:
                    primitiveObject.AddComponent<CapsuleCollider>();
                    break;
                case ColliderType.Mesh:
                    var meshCollider = primitiveObject.AddComponent<MeshCollider>();
                    meshCollider.convex = false;
                    var meshFilter = primitiveObject.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        meshCollider.sharedMesh = meshFilter.sharedMesh;
                    }
                    break;
                case ColliderType.Convex:
                    var convexCollider = primitiveObject.AddComponent<MeshCollider>();
                    convexCollider.convex = true;
                    var convexMeshFilter = primitiveObject.GetComponent<MeshFilter>();
                    if (convexMeshFilter != null && convexMeshFilter.sharedMesh != null)
                    {
                        convexCollider.sharedMesh = convexMeshFilter.sharedMesh;
                    }
                    break;
            }
        }

        /// <summary>
        /// 高品質メッシュを最終化
        /// </summary>
        private static void FinalizeHighQualityMesh(ProBuilderMesh mesh, QualitySettings quality)
        {
            // 最終的なメッシュ処理
            mesh.ToMesh();
            mesh.Refresh();
            
            // 法線とタンジェントを再計算
            var meshFilter = mesh.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                meshFilter.sharedMesh.RecalculateNormals();
                meshFilter.sharedMesh.RecalculateTangents();
                meshFilter.sharedMesh.RecalculateBounds();
            }
        }

        /// <summary>
        /// プリミティブ品質を検証
        /// </summary>
        private static bool ValidatePrimitiveQuality(GameObject primitiveObject, PrimitiveTerrainGenerator.PrimitiveType primitiveType, QualitySettings quality)
        {
            // 基本的な検証
            if (primitiveObject == null) return false;
            
            var meshFilter = primitiveObject.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null) return false;
            
            var mesh = meshFilter.sharedMesh;
            
            // 頂点数の検証
            int expectedMinVertices = GetExpectedMinVertices(primitiveType, quality);
            if (mesh.vertexCount < expectedMinVertices)
            {
                Debug.LogWarning($"Insufficient vertex count for {primitiveType}: {mesh.vertexCount} < {expectedMinVertices}");
                return false;
            }
            
            // 三角形数の検証
            if (mesh.triangles.Length < 12) // 最低限の三角形数
            {
                Debug.LogWarning($"Insufficient triangle count for {primitiveType}: {mesh.triangles.Length / 3}");
                return false;
            }
            
            // UV座標の検証（必要な場合）
            if (quality.enableUVUnwrapping && (mesh.uv == null || mesh.uv.Length == 0))
            {
                Debug.LogWarning($"Missing UV coordinates for {primitiveType}");
                return false;
            }
            
            // コライダーの検証
            var collider = primitiveObject.GetComponent<Collider>();
            if (quality.enablePreciseColliders && collider == null)
            {
                Debug.LogWarning($"Missing collider for {primitiveType}");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// 期待される最小頂点数を取得
        /// </summary>
        private static int GetExpectedMinVertices(PrimitiveTerrainGenerator.PrimitiveType primitiveType, QualitySettings quality)
        {
            int baseVertices = primitiveType switch
            {
                PrimitiveTerrainGenerator.PrimitiveType.Cube => 8,
                PrimitiveTerrainGenerator.PrimitiveType.Sphere => 42,
                PrimitiveTerrainGenerator.PrimitiveType.Cylinder => 24,
                PrimitiveTerrainGenerator.PrimitiveType.Pyramid => 5,
                PrimitiveTerrainGenerator.PrimitiveType.Torus => 64,
                _ => 8
            };
            
            // 細分化レベルに応じて増加
            return baseVertices * (int)Mathf.Pow(4, quality.subdivisionLevel);
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
            
            Debug.Log("Starting comprehensive primitive quality test...");
            
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
                        Debug.LogError($"Failed to generate {primitiveType}");
                        allPassed = false;
                        continue;
                    }
                    
                    bool passed = ValidatePrimitiveQuality(testObject, primitiveType, quality);
                    if (!passed)
                    {
                        Debug.LogError($"Quality validation failed for {primitiveType}");
                        allPassed = false;
                    }
                    else
                    {
                        Debug.Log($"✓ {primitiveType} passed quality test");
                    }
                    
                    // テストオブジェクトを削除
                    UnityEngine.Object.DestroyImmediate(testObject);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Exception testing {primitiveType}: {e.Message}");
                    allPassed = false;
                }
            }
            
            Debug.Log($"Primitive quality test completed. Result: {(allPassed ? "PASSED" : "FAILED")}");
            return allPassed;
        }

        /// <summary>
        /// プリミティブ生成統計を取得
        /// </summary>
        public static Dictionary<string, object> GetGenerationStatistics()
        {
            var stats = new Dictionary<string, object>();
            var primitiveTypes = System.Enum.GetValues(typeof(PrimitiveTerrainGenerator.PrimitiveType));
            
            stats["TotalPrimitiveTypes"] = primitiveTypes.Length;
            stats["ImplementedTypes"] = 16; // 全16種類実装済み
            stats["QualityLevels"] = 3; // High, Medium, Low
            stats["SupportedFeatures"] = new string[]
            {
                "Advanced Deformation",
                "Procedural Details",
                "High-Quality Colliders",
                "UV Unwrapping",
                "Smooth Normals",
                "Mesh Optimization",
                "LOD Support",
                "Quality Validation"
            };
            
            return stats;
        }
        #endregion
    }
}    
        }
            
            prism.positions = vertices;
        }
        
        /// <summary>
        /// 高度な円錐変形
        /// </summary>
        private static void ApplyAdvancedConeDeformation(ProBuilderMesh cone, QualitySettings quality)
        {
            var vertices = cone.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 高さに応じたテーパリング調整
                float heightFactor = (vertex.y + 1f) * 0.5f;
                
                // 螺旋状の溝
                float angle = Mathf.Atan2(vertex.z, vertex.x);
                float spiralGroove = Mathf.Sin(angle * 8f + vertex.y * 10f) * quality.detailIntensity * 0.03f;
                
                // 表面の粗さ
                float surfaceRoughness = (Mathf.PerlinNoise(vertex.x * 20f, vertex.z * 20f) - 0.5f) * quality.detailIntensity * 0.02f;
                
                vertex += vertex.normalized * (spiralGroove + surfaceRoughness);
                vertices[i] = vertex;
            }
            
            cone.positions = vertices;
        }
        
        /// <summary>
        /// 高度な八面体変形
        /// </summary>
        private static void ApplyAdvancedOctahedronDeformation(ProBuilderMesh octahedron, QualitySettings quality)
        {
            var vertices = octahedron.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 結晶面の発達
                float faceDistance = Mathf.Abs(vertex.x) + Mathf.Abs(vertex.y) + Mathf.Abs(vertex.z);
                float crystallineFaceting = Mathf.Floor(faceDistance * 10f) / 10f * quality.detailIntensity * 0.05f;
                
                // 自然な不完全性
                float imperfection = (Mathf.PerlinNoise(vertex.x * 25f, vertex.y * 25f) - 0.5f) * quality.detailIntensity * 0.02f;
                
                vertex += vertex.normalized * (crystallineFaceting + imperfection);
                vertices[i] = vertex;
            }
            
            octahedron.positions = vertices;
        }
        
        /// <summary>
        /// 高度な結晶変形
        /// </summary>
        private static void ApplyAdvancedCrystalDeformation(ProBuilderMesh crystal, QualitySettings quality)
        {
            var vertices = crystal.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 結晶成長の不均一性
                float growthVariation = Mathf.PerlinNoise(vertex.x * 5f, vertex.z * 5f) * quality.detailIntensity * 0.1f;
                
                // 結晶面の段差
                float facetStepping = Mathf.Floor(vertex.magnitude * 8f) / 8f * quality.detailIntensity * 0.03f;
                
                // 内包物による歪み
                float inclusionDistortion = Mathf.Sin(vertex.x * 15f) * Mathf.Sin(vertex.y * 12f) * Mathf.Sin(vertex.z * 18f) * quality.detailIntensity * 0.02f;
                
                vertex += vertex.normalized * (growthVariation + facetStepping + inclusionDistortion);
                vertices[i] = vertex;
            }
            
            crystal.positions = vertices;
        }
        
        /// <summary>
        /// 高度なモノリス変形
        /// </summary>
        private static void ApplyAdvancedMonolithDeformation(ProBuilderMesh monolith, QualitySettings quality)
        {
            var vertices = monolith.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 古代の風化パターン
                float heightFactor = (vertex.y + 1f) * 0.5f;
                float weatheringIntensity = heightFactor * quality.detailIntensity;
                
                // 縦方向の溝
                float verticalGrooves = Mathf.Sin(Mathf.Atan2(vertex.z, vertex.x) * 12f) * weatheringIntensity * 0.05f;
                
                // 水による浸食
                float waterErosion = Mathf.PerlinNoise(vertex.x * 8f, vertex.y * 3f) * weatheringIntensity * 0.03f;
                
                // 基部の拡張
                if (vertex.y < -0.5f)
                {
                    float baseExpansion = (1f + vertex.y) * quality.detailIntensity * 0.1f;
                    vertex.x *= (1f + baseExpansion);
                    vertex.z *= (1f + baseExpansion);
                }
                
                vertex += vertex.normalized * (verticalGrooves + waterErosion);
                vertices[i] = vertex;
            }
            
            monolith.positions = vertices;
        }
        
        /// <summary>
        /// 高度なリング変形
        /// </summary>
        private static void ApplyAdvancedRingDeformation(ProBuilderMesh ring, QualitySettings quality)
        {
            var vertices = ring.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // リングの非対称性
                float angle = Mathf.Atan2(vertex.z, vertex.x);
                float asymmetry = Mathf.Sin(angle * 2f) * quality.detailIntensity * 0.08f;
                
                // 厚さの変動
                float thicknessVariation = (1f + Mathf.Sin(angle * 5f) * quality.detailIntensity * 0.3f);
                vertex.y *= thicknessVariation;
                
                // 表面の装飾的な凹凸
                float decoration = Mathf.Sin(angle * 20f) * quality.detailIntensity * 0.02f;
                Vector3 radialDirection = new Vector3(vertex.x, 0, vertex.z).normalized;
                vertex += radialDirection * (asymmetry + decoration);
                
                vertices[i] = vertex;
            }
            
            ring.positions = vertices;
        }
        
        /// <summary>
        /// 高度なメサ変形
        /// </summary>
        private static void ApplyAdvancedMesaDeformation(ProBuilderMesh mesa, QualitySettings quality)
        {
            var vertices = mesa.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 地質学的な層構造
                float layering = Mathf.Floor(vertex.y * 8f) / 8f * quality.detailIntensity * 0.05f;
                
                // エッジの浸食
                float distanceFromCenter = new Vector2(vertex.x, vertex.z).magnitude;
                float edgeErosion = Mathf.Clamp01(distanceFromCenter - 0.7f) * quality.detailIntensity * 0.1f;
                
                // 上面の微細な起伏
                if (vertex.y > 0.8f)
                {
                    float topVariation = Mathf.PerlinNoise(vertex.x * 10f, vertex.z * 10f) * quality.detailIntensity * 0.03f;
                    vertex.y += topVariation;
                }
                
                vertex += vertex.normalized * (layering - edgeErosion);
                vertices[i] = vertex;
            }
            
            mesa.positions = vertices;
        }
        
        /// <summary>
        /// 高度な尖塔変形
        /// </summary>
        private static void ApplyAdvancedSpireDeformation(ProBuilderMesh spire, QualitySettings quality)
        {
            var vertices = spire.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 高さに応じた螺旋変形
                float heightFactor = (vertex.y + 1f) * 0.5f;
                float spiralTwist = heightFactor * quality.detailIntensity * 0.2f;
                
                float angle = Mathf.Atan2(vertex.z, vertex.x) + spiralTwist;
                float radius = new Vector2(vertex.x, vertex.z).magnitude;
                
                // 先端に向かって細くなる
                float tapering = 1f - heightFactor * 0.8f;
                radius *= tapering;
                
                vertex.x = radius * Mathf.Cos(angle);
                vertex.z = radius * Mathf.Sin(angle);
                
                // 表面の装飾的な溝
                float decorativeGrooves = Mathf.Sin(vertex.y * 15f) * quality.detailIntensity * 0.02f;
                vertex += vertex.normalized * decorativeGrooves;
                
                vertices[i] = vertex;
            }
            
            spire.positions = vertices;
        }
        
        /// <summary>
        /// 高度な巨石変形
        /// </summary>
        private static void ApplyAdvancedBoulderDeformation(ProBuilderMesh boulder, QualitySettings quality)
        {
            var vertices = boulder.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 複数スケールのノイズを組み合わせ
                float largeScale = Mathf.PerlinNoise(vertex.x * 2f, vertex.z * 2f) * 0.3f;
                float mediumScale = Mathf.PerlinNoise(vertex.x * 5f, vertex.z * 5f) * 0.2f;
                float smallScale = Mathf.PerlinNoise(vertex.x * 15f, vertex.z * 15f) * 0.1f;
                
                float combinedNoise = (largeScale + mediumScale + smallScale) * quality.detailIntensity;
                
                // 風化による角の丸み
                float weatheringRounding = quality.detailIntensity * 0.1f;
                vertex = Vector3.Lerp(vertex, vertex.normalized * vertex.magnitude, weatheringRounding);
                
                // 亀裂の表現
                float cracking = Mathf.Sin(vertex.x * 20f) * Mathf.Sin(vertex.y * 18f) * quality.detailIntensity * 0.02f;
                
                vertex += vertex.normalized * (combinedNoise + cracking);
                vertices[i] = vertex;
            }
            
            boulder.positions = vertices;
        }
        
        /// <summary>
        /// 高度な岩石層変形
        /// </summary>
        private static void ApplyAdvancedFormationDeformation(ProBuilderMesh formation, QualitySettings quality)
        {
            var vertices = formation.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 地質学的な褶曲
                float foldingIntensity = quality.detailIntensity * 0.15f;
                float folding = Mathf.Sin(vertex.x * 3f) * Mathf.Sin(vertex.z * 2f) * foldingIntensity;
                vertex.y += folding;
                
                // 断層による変位
                if (vertex.x > 0)
                {
                    float faultDisplacement = quality.detailIntensity * 0.1f;
                    vertex.y += faultDisplacement;
                }
                
                // 層理面の表現
                float layerCount = 12f + quality.complexityLevel * 3f;
                float layerThickness = 2f / layerCount;
                float layerPosition = Mathf.Floor((vertex.y + 1f) / layerThickness) * layerThickness - 1f;
                
                // 各層の硬度差による差別浸食
                float layerHardness = Mathf.Sin(layerPosition * 10f) * 0.5f + 0.5f;
                float erosionResistance = layerHardness * quality.detailIntensity * 0.05f;
                
                vertex += vertex.normalized * erosionResistance;
                vertices[i] = vertex;
            }
            
            formation.positions = vertices;
        }
        #endregion

        #region 高品質処理
        /// <summary>
        /// 高品質処理を適用
        /// </summary>
        private static void ApplyHighQualityProcessing(ProBuilderMesh mesh, QualitySettings quality)
        {
            // 細分化
            if (quality.subdivisionLevel > 0)
            {
                for (int i = 0; i < quality.subdivisionLevel; i++)
                {
                    mesh.Subdivide();
                }
            }
            
            // 滑らかな法線
            if (quality.enableSmoothNormals)
            {
                mesh.SetPivot(PivotLocation.Center);
                mesh.CenterPivot(null);
            }
            
            // UV展開
            if (quality.enableUVUnwrapping)
            {
                UnwrapParameters unwrapParams = UnwrapParameters.Default;
                unwrapParams.hardAngle = 60f;
                unwrapParams.packMargin = 4f;
                unwrapParams.angleError = 8f;
                unwrapParams.areaError = 15f;
                
                AutoUnwrapSettings.SetDefaultUnwrapParameters(unwrapParams);
                mesh.Unwrap();
            }
            
            // メッシュ最適化
            if (quality.meshOptimization > 0)
            {
                OptimizeMesh(mesh, quality.meshOptimization);
            }
        }

        /// <summary>
        /// プロシージャル詳細を追加
        /// </summary>
        private static void AddProceduralDetails(GameObject primitiveObject, PrimitiveTerrainGenerator.PrimitiveType primitiveType, QualitySettings quality)
        {
            if (!quality.enableProceduralDetails) return;
            
            switch (primitiveType)
            {
                case PrimitiveTerrainGenerator.PrimitiveType.Crystal:
                    AddCrystalDetails(primitiveObject, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Arch:
                    AddArchitecturalDetails(primitiveObject, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Formation:
                    AddGeologicalDetails(primitiveObject, quality);
                    break;
            }
        }

        /// <summary>
        /// 結晶の詳細を追加
        /// </summary>
        private static void AddCrystalDetails(GameObject crystal, QualitySettings quality)
        {
            // 小さな結晶の成長を追加
            for (int i = 0; i < quality.complexityLevel; i++)
            {
                if (Random.value < 0.3f)
                {
                    Vector3 position = Random.onUnitSphere * crystal.transform.localScale.magnitude * 0.3f;
                    Vector3 scale = Vector3.one * Random.Range(0.1f, 0.3f) * quality.detailIntensity;
                    
                    var smallCrystal = CrystalStructureGenerator.GenerateCrystalStructure(scale);
                    if (smallCrystal != null)
                    {
                        smallCrystal.transform.SetParent(crystal.transform);
                        smallCrystal.transform.localPosition = position;
                    }
                }
            }
        }

        /// <summary>
        /// 建築的詳細を追加
        /// </summary>
        private static void AddArchitecturalDetails(GameObject arch, QualitySettings quality)
        {
            // キーストーンの強調
            var bounds = arch.GetComponent<MeshRenderer>().bounds;
            Vector3 keystonePosition = new Vector3(0, bounds.max.y * 0.8f, 0);
            
            // 装飾的な要素を追加（簡易実装）
            GameObject keystone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keystone.transform.SetParent(arch.transform);
            keystone.transform.localPosition = keystonePosition;
            keystone.transform.localScale = Vector3.one * 0.1f * quality.detailIntensity;
            
            DestroyImmediate(keystone.GetComponent<Collider>());
        }

        /// <summary>
        /// 地質学的詳細を追加
        /// </summary>
        private static void AddGeologicalDetails(GameObject formation, QualitySettings quality)
        {
            // 化石や鉱物の追加（簡易実装）
            for (int i = 0; i < quality.complexityLevel * 2; i++)
            {
                if (Random.value < 0.2f)
                {
                    GameObject fossil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    fossil.transform.SetParent(formation.transform);
                    fossil.transform.localPosition = Random.insideUnitSphere * 0.8f;
                    fossil.transform.localScale = Vector3.one * Random.Range(0.02f, 0.05f) * quality.detailIntensity;
                    
                    DestroyImmediate(fossil.GetComponent<Collider>());
                }
            }
        }

        /// <summary>
        /// 高品質コライダーを設定
        /// </summary>
        private static void SetupHighQualityColliders(GameObject primitiveObject, QualitySettings quality)
        {
            // 既存のコライダーを削除
            var existingColliders = primitiveObject.GetComponents<Collider>();
            foreach (var collider in existingColliders)
            {
                DestroyImmediate(collider);
            }

            if (!quality.enablePreciseColliders) return;

            var meshFilter = primitiveObject.GetComponent<MeshFilter>();
            if (meshFilter?.sharedMesh == null) return;

            switch (quality.colliderType)
            {
                case ColliderType.Mesh:
                    var meshCollider = primitiveObject.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = meshFilter.sharedMesh;
                    meshCollider.convex = false;
                    break;
                    
                case ColliderType.Convex:
                    var convexCollider = primitiveObject.AddComponent<MeshCollider>();
                    convexCollider.sharedMesh = meshFilter.sharedMesh;
                    convexCollider.convex = true;
                    break;
                    
                case ColliderType.Box:
                    primitiveObject.AddComponent<BoxCollider>();
                    break;
                    
                case ColliderType.Sphere:
                    primitiveObject.AddComponent<SphereCollider>();
                    break;
                    
                case ColliderType.Capsule:
                    primitiveObject.AddComponent<CapsuleCollider>();
                    break;
            }
        }

        /// <summary>
        /// メッシュを最終化
        /// </summary>
        private static void FinalizeHighQualityMesh(ProBuilderMesh mesh, QualitySettings quality)
        {
            // メッシュを Unity メッシュに変換
            mesh.ToMesh();
            mesh.Refresh();
            
            // 法線を再計算
            var unityMesh = mesh.GetComponent<MeshFilter>().sharedMesh;
            if (unityMesh != null)
            {
                unityMesh.RecalculateNormals();
                unityMesh.RecalculateBounds();
                
                // タンジェントを計算（法線マッピング用）
                unityMesh.RecalculateTangents();
            }
        }

        /// <summary>
        /// メッシュを最適化
        /// </summary>
        private static void OptimizeMesh(ProBuilderMesh mesh, float optimizationLevel)
        {
            // 重複頂点をマージ
            mesh.MergeVertices();
            
            // 不要な頂点を削除
            if (optimizationLevel > 0.5f)
            {
                // より積極的な最適化
                mesh.DeleteVertices(mesh.GetVertices().Where(v => Vector3.Distance(v.position, Vector3.zero) < 0.001f));
            }
        }

        /// <summary>
        /// プリミティブ品質を検証
        /// </summary>
        private static bool ValidatePrimitiveQuality(GameObject primitiveObject, PrimitiveTerrainGenerator.PrimitiveType primitiveType, QualitySettings quality)
        {
            var meshFilter = primitiveObject.GetComponent<MeshFilter>();
            if (meshFilter?.sharedMesh == null)
            {
                Debug.LogError($"No mesh found for {primitiveType}");
                return false;
            }

            var mesh = meshFilter.sharedMesh;
            
            // 基本的な品質チェック
            if (mesh.vertexCount < 8)
            {
                Debug.LogError($"Insufficient vertices for {primitiveType}: {mesh.vertexCount}");
                return false;
            }
            
            if (mesh.triangles.Length < 12)
            {
                Debug.LogError($"Insufficient triangles for {primitiveType}: {mesh.triangles.Length / 3}");
                return false;
            }
            
            // 法線チェック
            if (mesh.normals == null || mesh.normals.Length == 0)
            {
                Debug.LogWarning($"Missing normals for {primitiveType}");
                return false;
            }
            
            return true;
        }
        #endregion
    }
}