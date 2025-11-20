using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using Vastcore.Generation.Map;

namespace Vastcore.Generation
{
    /// <summary>
    /// プリミティブ地形生成システム
    /// ProBuilderを使用して16種類のプリミティブ地形を生成
    /// </summary>
    public static class PrimitiveTerrainGenerator
    {
        #region プリミティブタイプ定義
        public enum PrimitiveType
        {
            // 基本プリミティブ
            Cube,           // 巨大立方体
            Sphere,         // 巨大球体
            Cylinder,       // 巨大円柱
            Pyramid,        // 巨大ピラミッド
            
            // 複合プリミティブ
            Torus,          // 巨大トーラス（ドーナツ型）
            Prism,          // 巨大角柱
            Cone,           // 巨大円錐
            Octahedron,     // 巨大八面体
            
            // 特殊プリミティブ
            Crystal,        // 結晶構造
            Monolith,       // モノリス（石柱）
            Arch,           // アーチ構造
            Ring,           // リング構造
            
            // 地形統合プリミティブ
            Mesa,           // メサ（台地）
            Spire,          // 尖塔
            Boulder,        // 巨石
            Formation       // 岩石層
        }
        #endregion

        #region プリミティブ生成設定
        [System.Serializable]
        public struct PrimitiveGenerationParams
        {
            [Header("基本設定")]
            public PrimitiveType primitiveType;
            public Vector3 position;
            public Vector3 scale;
            public Quaternion rotation;
            
            [Header("形状変形")]
            public bool enableDeformation;
            public Vector3 deformationRange;
            public float noiseIntensity;
            public int subdivisionLevel;
            
            [Header("材質設定")]
            public Material material;
            public Color colorVariation;
            public bool randomizeMaterial;
            
            [Header("物理設定")]
            public bool generateCollider;
            public bool isClimbable;
            public bool isGrindable;
            
            public static PrimitiveGenerationParams Default(PrimitiveType type)
            {
                return new PrimitiveGenerationParams
                {
                    primitiveType = type,
                    position = Vector3.zero,
                    scale = Vector3.one * 100f, // デフォルト100mサイズ
                    rotation = Quaternion.identity,
                    enableDeformation = true,
                    deformationRange = Vector3.one * 0.1f,
                    noiseIntensity = 0.05f,
                    subdivisionLevel = 2,
                    material = null,
                    colorVariation = Color.white,
                    randomizeMaterial = false,
                    generateCollider = true,
                    isClimbable = true,
                    isGrindable = true
                };
            }
        }
        #endregion

        /// <summary>
        /// プリミティブ地形オブジェクトを生成
        /// </summary>
        public static GameObject GeneratePrimitiveTerrain(PrimitiveGenerationParams parameters)
        {
            try
            {
                // プリミティブ生成器を取得
                var generator = PrimitiveGeneratorFactory.CreateGenerator(parameters.primitiveType);
                
                // 基本プリミティブメッシュを生成
                ProBuilderMesh proBuilderMesh = generator.GeneratePrimitive(parameters.scale);
                
                if (proBuilderMesh == null)
                {
                    Debug.LogError($"Failed to generate base primitive: {parameters.primitiveType}");
                    return null;
                }

                // GameObjectを設定
                GameObject primitiveObject = proBuilderMesh.gameObject;
                primitiveObject.name = $"Primitive_{parameters.primitiveType}";
                primitiveObject.transform.position = parameters.position;
                primitiveObject.transform.rotation = parameters.rotation;

                // 形状変形を適用
                if (parameters.enableDeformation)
                {
                    PrimitiveModifier.ApplyDeformation(proBuilderMesh, parameters.deformationRange, parameters.noiseIntensity);
                }

                // 細分化を適用
                if (parameters.subdivisionLevel > 0)
                {
                    PrimitiveModifier.ApplySubdivision(proBuilderMesh, parameters.subdivisionLevel);
                }

                // メッシュを最終化
                proBuilderMesh.ToMesh();
                proBuilderMesh.Refresh();

                // マテリアルを設定
                PrimitiveConfigurator.SetupMaterial(primitiveObject, parameters);

                // コライダーを生成
                if (parameters.generateCollider)
                {
                    PrimitiveConfigurator.GenerateCollider(primitiveObject, parameters);
                }

                // インタラクション設定を追加
                PrimitiveConfigurator.SetupInteractionComponents(primitiveObject, parameters);

                Debug.Log($"Successfully generated primitive terrain: {parameters.primitiveType} at {parameters.position}");
                return primitiveObject;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error generating primitive terrain {parameters.primitiveType}: {e.Message}");
                return null;
            }
        }   
     #region 基本プリミティブ生成
        /// <summary>
        /// 基本プリミティブメッシュを生成
        /// </summary>
        private static ProBuilderMesh GenerateBasePrimitive(PrimitiveType type, Vector3 scale)
        {
            ProBuilderMesh mesh = null;

            switch (type)
            {
                case PrimitiveType.Cube:
                    mesh = GenerateScaledCube(scale);
                    break;
                case PrimitiveType.Sphere:
                    mesh = GenerateScaledSphere(scale);
                    break;
                case PrimitiveType.Cylinder:
                    mesh = GenerateScaledCylinder(scale);
                    break;
                case PrimitiveType.Pyramid:
                    mesh = GenerateScaledPyramid(scale);
                    break;
                case PrimitiveType.Torus:
                    mesh = GenerateScaledTorus(scale);
                    break;
                case PrimitiveType.Prism:
                    mesh = GenerateScaledPrism(scale);
                    break;
                case PrimitiveType.Cone:
                    mesh = GenerateScaledCone(scale);
                    break;
                case PrimitiveType.Octahedron:
                    mesh = GenerateScaledOctahedron(scale);
                    break;
                case PrimitiveType.Crystal:
                    mesh = GenerateCrystalStructure(scale);
                    break;
                case PrimitiveType.Monolith:
                    mesh = GenerateMonolith(scale);
                    break;
                case PrimitiveType.Arch:
                    mesh = GenerateArch(scale);
                    break;
                case PrimitiveType.Ring:
                    mesh = GenerateRing(scale);
                    break;
                case PrimitiveType.Mesa:
                    mesh = GenerateMesa(scale);
                    break;
                case PrimitiveType.Spire:
                    mesh = GenerateSpire(scale);
                    break;
                case PrimitiveType.Boulder:
                    mesh = GenerateBoulder(scale);
                    break;
                case PrimitiveType.Formation:
                    mesh = GenerateFormation(scale);
                    break;
                default:
                    Debug.LogWarning($"Primitive type {type} not implemented, using cube");
                    mesh = GenerateScaledCube(scale);
                    break;
            }

            return mesh;
        }

        /// <summary>
        /// スケール済み立方体を生成
        /// </summary>
        private static ProBuilderMesh GenerateScaledCube(Vector3 scale)
        {
            var cube = ShapeGenerator.CreateShape(ShapeType.Cube);
            cube.transform.localScale = scale;
            return cube;
        }

        /// <summary>
        /// スケール済み球体を生成
        /// </summary>
        private static ProBuilderMesh GenerateScaledSphere(Vector3 scale)
        {
            var sphere = ShapeGenerator.CreateShape(ShapeType.Sphere);
            sphere.transform.localScale = scale;
            return sphere;
        }

        /// <summary>
        /// スケール済み円柱を生成
        /// </summary>
        private static ProBuilderMesh GenerateScaledCylinder(Vector3 scale)
        {
            var cylinder = ShapeGenerator.CreateShape(ShapeType.Cylinder);
            cylinder.transform.localScale = scale;
            return cylinder;
        }

        /// <summary>
        /// スケール済みピラミッドを生成
        /// </summary>
        private static ProBuilderMesh GenerateScaledPyramid(Vector3 scale)
        {
            // ProBuilderにはピラミッドがないので、カスタム生成
            var pyramid = ShapeGenerator.CreateShape(ShapeType.Cube);
            
            // 上部の頂点を中央に移動してピラミッド形状を作成
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
            
            return pyramid;
        }

        /// <summary>
        /// スケール済みトーラスを生成
        /// </summary>
        private static ProBuilderMesh GenerateScaledTorus(Vector3 scale)
        {
            var torus = ShapeGenerator.CreateShape(ShapeType.Torus);
            torus.transform.localScale = scale;
            return torus;
        }

        /// <summary>
        /// スケール済み角柱を生成
        /// </summary>
        private static ProBuilderMesh GenerateScaledPrism(Vector3 scale)
        {
            var prism = ShapeGenerator.CreateShape(ShapeType.Prism);
            prism.transform.localScale = scale;
            return prism;
        }

        /// <summary>
        /// スケール済み円錐を生成
        /// </summary>
        private static ProBuilderMesh GenerateScaledCone(Vector3 scale)
        {
            var cone = ShapeGenerator.CreateShape(ShapeType.Cone);
            cone.transform.localScale = scale;
            return cone;
        }

        /// <summary>
        /// スケール済み八面体を生成
        /// </summary>
        private static ProBuilderMesh GenerateScaledOctahedron(Vector3 scale)
        {
            // 八面体はカスタム生成
            var octahedron = ShapeGenerator.CreateShape(ShapeType.Cube);
            
            // 立方体を八面体に変形
            var vertices = octahedron.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = vertices[i].normalized * scale.magnitude;
            }
            octahedron.positions = vertices;
            octahedron.transform.localScale = Vector3.one;
            
            return octahedron;
        }
        #endregion

        #region 複合・特殊プリミティブ生成
        /// <summary>
        /// 結晶構造を生成（高品質版）
        /// </summary>
        private static ProBuilderMesh GenerateCrystalStructure(Vector3 scale)
        {
            // 新しい高品質結晶構造生成システムを使用
            // return CrystalStructureGenerator.GenerateCrystalWithGrowthSimulation(scale, true);
            // フォールバック：基本的な結晶形状
            var crystal = ShapeGenerator.CreateShape(ShapeType.Cube);
            crystal.transform.localScale = scale;
            return crystal;
        }

        /// <summary>
        /// モノリス（石柱）を生成
        /// </summary>
        private static ProBuilderMesh GenerateMonolith(Vector3 scale)
        {
            // 縦長の石柱構造
            var monolith = ShapeGenerator.CreateShape(ShapeType.Cube);
            
            // 縦長に調整
            Vector3 monolithScale = new Vector3(scale.x * 0.3f, scale.y * 2f, scale.z * 0.3f);
            monolith.transform.localScale = monolithScale;
            
            // 上部を少し細くして自然な石柱形状に
            var vertices = monolith.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].y > 0) // 上部の頂点
                {
                    vertices[i] = new Vector3(vertices[i].x * 0.8f, vertices[i].y, vertices[i].z * 0.8f);
                }
            }
            monolith.positions = vertices;
            
            return monolith;
        }

        /// <summary>
        /// アーチ構造を生成（新しい建築学的システムを使用）
        /// </summary>
        private static ProBuilderMesh GenerateArch(Vector3 scale)
        {
            // 新しい建築学的生成システムを使用してより高品質なアーチを生成
            // var archParams = ArchitecturalGenerator.ArchitecturalParams.Default(ArchitecturalGenerator.ArchitecturalType.SimpleArch);
            // archParams.span = scale.x;
            // archParams.height = scale.y;
            // archParams.thickness = scale.z;
            // archParams.position = Vector3.zero;
            
            // 建築構造を生成
            // var archObject = ArchitecturalGenerator.GenerateArchitecturalStructure(archParams);
            
            // if (archObject != null)
            // {
            //     // 生成されたオブジェクトからメッシュを取得
            //     var meshFilter = archObject.GetComponent<MeshFilter>();
            //     if (meshFilter != null && meshFilter.sharedMesh != null)
            //     {
            //         // ProBuilderMeshに変換
            //         var proBuilderMesh = archObject.GetComponent<ProBuilderMesh>();
            //         if (proBuilderMesh == null)
            //         {
            //             proBuilderMesh = archObject.AddComponent<ProBuilderMesh>();
            //             // TODO: RebuildFromMesh機能はProBuilder API変更により一時的に無効化
            //             Debug.LogWarning($"RebuildFromMesh feature is temporarily disabled due to ProBuilder API changes.");
            //         }
            //         
            //         // 一時的なオブジェクトを削除
            //         UnityEngine.Object.DestroyImmediate(archObject);
            //         
            //         return proBuilderMesh;
            //     }
            //     
            //     // フォールバック：オブジェクトが正しく生成されなかった場合
            //     UnityEngine.Object.DestroyImmediate(archObject);
            // }
            
            // フォールバック：基本的なアーチ形状
            var fallbackArch = ShapeGenerator.CreateShape(ShapeType.Arch);
            fallbackArch.transform.localScale = scale;
            
            return fallbackArch;
        }

        /// <summary>
        /// リング構造を生成
        /// </summary>
        private static ProBuilderMesh GenerateRing(Vector3 scale)
        {
            // トーラスをベースにしてリング形状を作成
            var ring = ShapeGenerator.CreateShape(ShapeType.Torus);
            
            // リング形状に調整（薄くて大きい）
            Vector3 ringScale = new Vector3(scale.x * 1.5f, scale.y * 0.2f, scale.z * 1.5f);
            ring.transform.localScale = ringScale;
            
            return ring;
        }

        /// <summary>
        /// メサ（台地）を生成
        /// </summary>
        private static ProBuilderMesh GenerateMesa(Vector3 scale)
        {
            // 台地状の地形プリミティブ
            var mesa = ShapeGenerator.CreateShape(ShapeType.Cylinder);
            
            // 台地形状に調整（平たくて広い）
            Vector3 mesaScale = new Vector3(scale.x * 2f, scale.y * 0.3f, scale.z * 2f);
            mesa.transform.localScale = mesaScale;
            
            // 上部を平らにして台地らしくする
            var vertices = mesa.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].y > 0) // 上部の頂点
                {
                    vertices[i] = new Vector3(vertices[i].x, vertices[i].y, vertices[i].z);
                }
            }
            mesa.positions = vertices;
            
            return mesa;
        }

        /// <summary>
        /// 尖塔を生成
        /// </summary>
        private static ProBuilderMesh GenerateSpire(Vector3 scale)
        {
            // 非常に高い円錐形状
            var spire = ShapeGenerator.CreateShape(ShapeType.Cone);
            
            // 尖塔形状に調整（非常に高くて細い）
            Vector3 spireScale = new Vector3(scale.x * 0.4f, scale.y * 3f, scale.z * 0.4f);
            spire.transform.localScale = spireScale;
            
            return spire;
        }

        /// <summary>
        /// 巨石を生成
        /// </summary>
        private static ProBuilderMesh GenerateBoulder(Vector3 scale)
        {
            // 不規則な岩石形状
            var boulder = ShapeGenerator.CreateShape(ShapeType.Sphere);
            
            // 不規則な形状に変形
            var vertices = boulder.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // ランダムな変形を加えて岩石らしくする
                float randomFactor = 1f + (Mathf.PerlinNoise(vertex.x * 5f, vertex.z * 5f) - 0.5f) * 0.3f;
                vertices[i] = vertex * randomFactor;
            }
            boulder.positions = vertices;
            boulder.transform.localScale = scale;
            
            return boulder;
        }

        /// <summary>
        /// 岩石層を生成
        /// </summary>
        private static ProBuilderMesh GenerateFormation(Vector3 scale)
        {
            // 層状の岩石構造
            var formation = ShapeGenerator.CreateShape(ShapeType.Cube);
            
            // 層状構造に変形
            var vertices = formation.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // Y軸方向に層を作る
                float layerHeight = Mathf.Floor(vertex.y * 5f) / 5f;
                vertex.y = layerHeight;
                
                // 各層で少しずつずらす
                float layerOffset = (layerHeight + 1f) * 0.1f;
                vertex.x += Mathf.Sin(layerOffset) * 0.1f;
                vertex.z += Mathf.Cos(layerOffset) * 0.1f;
                
                vertices[i] = vertex;
            }
            formation.positions = vertices;
            formation.transform.localScale = scale;
            
            return formation;
        }
        #endregion  
      #region 形状変形処理
        /// <summary>
        /// ノイズベースの形状変形を適用
        /// </summary>
        private static void ApplyDeformation(ProBuilderMesh mesh, PrimitiveGenerationParams parameters)
        {
            var vertices = mesh.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // Perlinノイズによる変形
                float noiseX = Mathf.PerlinNoise(vertex.x * 0.1f, vertex.z * 0.1f);
                float noiseY = Mathf.PerlinNoise(vertex.y * 0.1f, vertex.x * 0.1f);
                float noiseZ = Mathf.PerlinNoise(vertex.z * 0.1f, vertex.y * 0.1f);
                
                Vector3 deformation = new Vector3(noiseX - 0.5f, noiseY - 0.5f, noiseZ - 0.5f) * parameters.noiseIntensity;
                deformation = Vector3.Scale(deformation, parameters.deformationRange);
                
                vertices[i] = vertex + deformation;
            }
            
            mesh.positions = vertices;
        }

        /// <summary>
        /// メッシュの細分化を適用
        /// </summary>
        private static void ApplySubdivision(ProBuilderMesh mesh, int subdivisionLevel)
        {
            for (int i = 0; i < subdivisionLevel; i++)
            {
                // TODO: Subdivide機能はProBuilder API変更により一時的に無効化
                Debug.LogWarning($"Subdivide feature is temporarily disabled due to ProBuilder API changes. Requested level: {subdivisionLevel}");
            }
        }
        #endregion     
   #region マテリアル・コライダー設定
        /// <summary>
        /// マテリアルを設定
        /// </summary>
        private static void SetupMaterial(GameObject primitiveObject, PrimitiveGenerationParams parameters)
        {
            var renderer = primitiveObject.GetComponent<MeshRenderer>();
            if (renderer != null && parameters.material != null)
            {
                renderer.material = parameters.material;
                
                // 色のバリエーションを適用
                if (parameters.randomizeMaterial)
                {
                    var materialInstance = new Material(parameters.material);
                    materialInstance.color = parameters.colorVariation;
                    renderer.material = materialInstance;
                }
            }
        }

        /// <summary>
        /// コライダーを生成
        /// </summary>
        private static void GenerateCollider(GameObject primitiveObject, PrimitiveGenerationParams parameters)
        {
            // 既存のコライダーを削除
            var existingCollider = primitiveObject.GetComponent<Collider>();
            if (existingCollider != null)
            {
                Object.DestroyImmediate(existingCollider);
            }

            // メッシュコライダーを追加
            var meshCollider = primitiveObject.AddComponent<MeshCollider>();
            meshCollider.convex = false; // 大きなオブジェクトなので非凸型
            
            var meshFilter = primitiveObject.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                meshCollider.sharedMesh = meshFilter.sharedMesh;
            }
        }

        /// <summary>
        /// インタラクション用コンポーネントを設定
        /// </summary>
        private static void SetupInteractionComponents(GameObject primitiveObject, PrimitiveGenerationParams parameters)
        {
            // プリミティブ地形オブジェクトコンポーネントを追加
            // var primitiveComponent = primitiveObject.AddComponent<PrimitiveTerrainObject>();
            // primitiveComponent.primitiveType = (GenerationPrimitiveType)(int)parameters.primitiveType;
            // primitiveComponent.isClimbable = parameters.isClimbable;
            // primitiveComponent.isGrindable = parameters.isGrindable;
            // primitiveComponent.hasCollision = parameters.generateCollider;
            
            // 適切なレイヤーを設定
            primitiveObject.layer = LayerMask.NameToLayer("Default"); // 必要に応じて専用レイヤーを作成
        }
        #endregion
        #region ユーティリティ関数
        /// <summary>
        /// プリミティブタイプに応じたデフォルトスケールを取得
        /// </summary>
        public static Vector3 GetDefaultScale(PrimitiveType type)
        {
            return PrimitiveGeneratorFactory.GetDefaultScale(type);
        }

        /// <summary>
        /// ランダムなプリミティブタイプを取得
        /// </summary>
        public static PrimitiveType GetRandomPrimitiveType()
        {
            var values = System.Enum.GetValues(typeof(PrimitiveType));
            return (PrimitiveType)values.GetValue(Random.Range(0, values.Length));
        }

        /// <summary>
        /// プリミティブタイプの説明を取得
        /// </summary>
        public static string GetPrimitiveDescription(PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Cube: return "巨大な立方体構造物";
                case PrimitiveType.Sphere: return "巨大な球体構造物";
                case PrimitiveType.Cylinder: return "巨大な円柱構造物";
                case PrimitiveType.Pyramid: return "巨大なピラミッド構造物";
                case PrimitiveType.Torus: return "巨大なドーナツ型構造物";
                case PrimitiveType.Prism: return "巨大な角柱構造物";
                case PrimitiveType.Cone: return "巨大な円錐構造物";
                case PrimitiveType.Octahedron: return "巨大な八面体構造物";
                case PrimitiveType.Crystal: return "結晶構造物";
                case PrimitiveType.Monolith: return "モノリス石柱";
                case PrimitiveType.Arch: return "アーチ構造物";
                case PrimitiveType.Ring: return "リング構造物";
                case PrimitiveType.Mesa: return "メサ台地";
                case PrimitiveType.Spire: return "尖塔構造物";
                case PrimitiveType.Boulder: return "巨石";
                case PrimitiveType.Formation: return "岩石層";
                default: return "不明な構造物";
            }
        }

        /// <summary>
        /// 複合形状を生成（複数のプリミティブを組み合わせ）
        /// </summary>
        public static GameObject GenerateCompoundPrimitive(PrimitiveType[] types, Vector3[] offsets, Vector3[] scales, Vector3 position)
        {
            if (types.Length != offsets.Length || types.Length != scales.Length)
            {
                Debug.LogError("Arrays must have the same length for compound primitive generation");
                return null;
            }

            GameObject compound = new GameObject("CompoundPrimitive");
            compound.transform.position = position;

            for (int i = 0; i < types.Length; i++)
            {
                var parameters = PrimitiveGenerationParams.Default(types[i]);
                parameters.position = position + offsets[i];
                parameters.scale = scales[i];
                parameters.generateCollider = (i == 0); // 最初のオブジェクトのみコライダー生成

                var primitive = GeneratePrimitiveTerrain(parameters);
                if (primitive != null)
                {
                    primitive.transform.SetParent(compound.transform);
                }
            }

            // 複合オブジェクト全体にコライダーを追加
            var meshCollider = compound.AddComponent<MeshCollider>();
            meshCollider.convex = false;

            return compound;
        }

        /// <summary>
        /// 形状変形の強度を調整
        /// </summary>
        public static void ApplyAdvancedDeformation(ProBuilderMesh mesh, float intensity, int seed = 0)
        {
            Random.InitState(seed);
            var vertices = mesh.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 複数のノイズレイヤーを組み合わせ
                float noise1 = Mathf.PerlinNoise(vertex.x * 0.05f, vertex.z * 0.05f);
                float noise2 = Mathf.PerlinNoise(vertex.x * 0.1f, vertex.z * 0.1f) * 0.5f;
                float noise3 = Mathf.PerlinNoise(vertex.x * 0.2f, vertex.z * 0.2f) * 0.25f;
                
                float combinedNoise = (noise1 + noise2 + noise3) / 1.75f;
                
                // 法線方向に変形
                Vector3 normal = vertex.normalized;
                vertices[i] = vertex + normal * (combinedNoise - 0.5f) * intensity;
            }
            
            mesh.positions = vertices;
        }
        #endregion
    }
}