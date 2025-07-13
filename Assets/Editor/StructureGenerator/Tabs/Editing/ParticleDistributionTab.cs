using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.Shapes;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Editor.Generation
{
    /// <summary>
    /// パーティクル様配置システムタブクラス
    /// Phase 4: 構造物の高度配置制御システム
    /// </summary>
    public class ParticleDistributionTab : IStructureTab
    {
        public TabCategory Category => TabCategory.Editing;
        public string DisplayName => "Distribution";
        public string Description => "サーフェス上にパーティクルやオブジェクトを分布させます。";
        public bool SupportsRealTimeUpdate => false;

        private StructureGeneratorWindow _parent;
        private Vector2 _scrollPosition;
        
        // 配置パラメータ
        private DistributionPattern distributionPattern = DistributionPattern.Linear;
        private int structureCount = 10;
        private float distributionRadius = 50f;
        private float minDistance = 5f;
        private bool enableCollisionAvoidance = true;
        
        // 回転・スケール制御
        private bool randomRotation = true;
        private Vector3 rotationRange = new Vector3(0, 360, 0);
        private bool randomScale = false;
        private Vector2 scaleRange = new Vector2(0.8f, 1.2f);
        
        // 高度配置制御
        private bool useHeightmap = false;
        private Texture2D heightmapTexture;
        private float heightMultiplier = 10f;
        private bool alignToSurface = false;
        
        // プリセット構造物
        private GameObject prefabToDistribute;
        private AdvancedStructureTab.AdvancedShapeType monumentType = AdvancedStructureTab.AdvancedShapeType.Monolith;
        
        // 配置パターン
        public enum DistributionPattern
        {
            Linear,      // 線形配置
            Circular,    // 円形配置
            Spiral,      // 螺旋配置
            Grid,        // 格子配置
            Random,      // ランダム配置
            Fractal,     // フラクタル配置
            Voronoi,     // ボロノイ配置
            Organic      // 有機的配置
        }
        
        public ParticleDistributionTab(StructureGeneratorWindow parent)
        {
            _parent = parent;
        }
        
        public void DrawGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            EditorGUILayout.LabelField(DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Description, MessageType.Info);

            // 配置パターン設定
            DrawDistributionPatternSettings();
            
            EditorGUILayout.Space();
            
            // 構造物設定
            DrawStructureSettings();
            
            EditorGUILayout.Space();
            
            // 高度制御設定
            DrawAdvancedControls();
            
            EditorGUILayout.Space();
            
            // 配置実行ボタン
            DrawDistributionButtons();

            EditorGUILayout.EndScrollView();
        }
        
        public void HandleRealTimeUpdate() { }

        public void OnSceneGUI() { }
        public void ProcessSelectedObjects() { GenerateDistribution(); }
        public void OnTabSelected() { }
        public void OnTabDeselected() { }
        
        private void DrawDistributionPatternSettings()
        {
            EditorGUILayout.LabelField("Distribution Pattern", EditorStyles.boldLabel);
            
            distributionPattern = (DistributionPattern)EditorGUILayout.EnumPopup("Pattern", distributionPattern);
            structureCount = EditorGUILayout.IntSlider("Structure Count", structureCount, 1, 100);
            distributionRadius = EditorGUILayout.Slider("Distribution Radius", distributionRadius, 10f, 200f);
            minDistance = EditorGUILayout.Slider("Min Distance", minDistance, 1f, 20f);
            enableCollisionAvoidance = EditorGUILayout.Toggle("Collision Avoidance", enableCollisionAvoidance);
            
            // パターン別の特別設定
            DrawPatternSpecificSettings();
        }
        
        private void DrawPatternSpecificSettings()
        {
            EditorGUI.indentLevel++;
            
            switch (distributionPattern)
            {
                case DistributionPattern.Linear:
                    EditorGUILayout.LabelField("Linear Pattern Settings", EditorStyles.miniBoldLabel);
                    // 線形配置の特別設定はなし
                    break;
                    
                case DistributionPattern.Circular:
                    EditorGUILayout.LabelField("Circular Pattern Settings", EditorStyles.miniBoldLabel);
                    // 円形配置の特別設定はなし
                    break;
                    
                case DistributionPattern.Spiral:
                    EditorGUILayout.LabelField("Spiral Pattern Settings", EditorStyles.miniBoldLabel);
                    spiralTurns = EditorGUILayout.Slider("Spiral Turns", spiralTurns, 1f, 10f);
                    break;
                    
                case DistributionPattern.Grid:
                    EditorGUILayout.LabelField("Grid Pattern Settings", EditorStyles.miniBoldLabel);
                    gridSize = EditorGUILayout.Vector2IntField("Grid Size", gridSize);
                    break;
                    
                case DistributionPattern.Random:
                    EditorGUILayout.LabelField("Random Pattern Settings", EditorStyles.miniBoldLabel);
                    randomSeed = EditorGUILayout.IntField("Random Seed", randomSeed);
                    break;
                    
                case DistributionPattern.Fractal:
                    EditorGUILayout.LabelField("Fractal Pattern Settings", EditorStyles.miniBoldLabel);
                    fractalIterations = EditorGUILayout.IntSlider("Iterations", fractalIterations, 1, 5);
                    fractalScale = EditorGUILayout.Slider("Scale Factor", fractalScale, 0.1f, 0.9f);
                    break;
                    
                case DistributionPattern.Voronoi:
                    EditorGUILayout.LabelField("Voronoi Pattern Settings", EditorStyles.miniBoldLabel);
                    voronoiSites = EditorGUILayout.IntSlider("Voronoi Sites", voronoiSites, 3, 20);
                    break;
                    
                case DistributionPattern.Organic:
                    EditorGUILayout.LabelField("Organic Pattern Settings", EditorStyles.miniBoldLabel);
                    organicDensity = EditorGUILayout.Slider("Organic Density", organicDensity, 0.1f, 2f);
                    break;
            }
            
            EditorGUI.indentLevel--;
        }
        
        private void DrawStructureSettings()
        {
            EditorGUILayout.LabelField("Structure Settings", EditorStyles.boldLabel);
            
            prefabToDistribute = (GameObject)EditorGUILayout.ObjectField("Prefab to Distribute", prefabToDistribute, typeof(GameObject), false);
            
            if (prefabToDistribute == null)
            {
                EditorGUILayout.HelpBox("No prefab selected. Will generate monuments of selected type.", MessageType.Info);
                monumentType = (AdvancedStructureTab.AdvancedShapeType)EditorGUILayout.EnumPopup("Monument Type", monumentType);
            }
            
            EditorGUILayout.Space();
            
            // 回転制御
            EditorGUILayout.LabelField("Rotation Control", EditorStyles.miniBoldLabel);
            randomRotation = EditorGUILayout.Toggle("Random Rotation", randomRotation);
            if (randomRotation)
            {
                rotationRange = EditorGUILayout.Vector3Field("Rotation Range", rotationRange);
            }
            
            // スケール制御
            EditorGUILayout.LabelField("Scale Control", EditorStyles.miniBoldLabel);
            randomScale = EditorGUILayout.Toggle("Random Scale", randomScale);
            if (randomScale)
            {
                scaleRange = EditorGUILayout.Vector2Field("Scale Range", scaleRange);
            }
        }
        
        private void DrawAdvancedControls()
        {
            EditorGUILayout.LabelField("Advanced Controls", EditorStyles.boldLabel);
            
            useHeightmap = EditorGUILayout.Toggle("Use Heightmap", useHeightmap);
            if (useHeightmap)
            {
                heightmapTexture = (Texture2D)EditorGUILayout.ObjectField("Heightmap Texture", heightmapTexture, typeof(Texture2D), false);
                heightMultiplier = EditorGUILayout.Slider("Height Multiplier", heightMultiplier, 0f, 50f);
            }
            
            alignToSurface = EditorGUILayout.Toggle("Align to Surface", alignToSurface);
        }
        
        private void DrawDistributionButtons()
        {
            EditorGUILayout.LabelField("Distribution Actions", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Generate Distribution", GUILayout.Height(30)))
            {
                GenerateDistribution();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Clear All Distributed Objects"))
            {
                ClearDistributedObjects();
            }
            
            if (GUILayout.Button("Preview Distribution (Gizmos)"))
            {
                PreviewDistribution();
            }
        }
        
        // パターン別の特別パラメータ
        private float spiralTurns = 3f;
        private Vector2Int gridSize = new Vector2Int(5, 5);
        private int randomSeed = 42;
        private int fractalIterations = 3;
        private float fractalScale = 0.5f;
        private int voronoiSites = 8;
        private float organicDensity = 1f;
        
        private void GenerateDistribution()
        {
            try
            {
                // 配置位置を計算
                var positions = CalculateDistributionPositions();
                
                // 構造物を配置
                PlaceStructuresAtPositions(positions);
                
                Debug.Log($"Successfully distributed {positions.Count} structures using {distributionPattern} pattern");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to generate distribution: {e.Message}");
            }
        }
        
        private List<Vector3> CalculateDistributionPositions()
        {
            var positions = new List<Vector3>();
            
            switch (distributionPattern)
            {
                case DistributionPattern.Linear:
                    positions = GenerateLinearPositions();
                    break;
                case DistributionPattern.Circular:
                    positions = GenerateCircularPositions();
                    break;
                case DistributionPattern.Spiral:
                    positions = GenerateSpiralPositions();
                    break;
                case DistributionPattern.Grid:
                    positions = GenerateGridPositions();
                    break;
                case DistributionPattern.Random:
                    positions = GenerateRandomPositions();
                    break;
                case DistributionPattern.Fractal:
                    positions = GenerateFractalPositions();
                    break;
                case DistributionPattern.Voronoi:
                    positions = GenerateVoronoiPositions();
                    break;
                case DistributionPattern.Organic:
                    positions = GenerateOrganicPositions();
                    break;
            }
            
            // 衝突回避処理
            if (enableCollisionAvoidance)
            {
                positions = ApplyCollisionAvoidance(positions);
            }
            
            // ハイトマップ適用
            if (useHeightmap && heightmapTexture != null)
            {
                positions = ApplyHeightmap(positions);
            }
            
            return positions;
        }
        
        private List<Vector3> GenerateLinearPositions()
        {
            var positions = new List<Vector3>();
            Vector3 startPos = -Vector3.forward * distributionRadius * 0.5f;
            Vector3 endPos = Vector3.forward * distributionRadius * 0.5f;
            
            for (int i = 0; i < structureCount; i++)
            {
                float t = (float)i / (structureCount - 1);
                Vector3 pos = Vector3.Lerp(startPos, endPos, t);
                positions.Add(pos);
            }
            
            return positions;
        }
        
        private List<Vector3> GenerateCircularPositions()
        {
            var positions = new List<Vector3>();
            
            for (int i = 0; i < structureCount; i++)
            {
                float angle = (float)i / structureCount * 2f * Mathf.PI;
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * distributionRadius,
                    0,
                    Mathf.Sin(angle) * distributionRadius
                );
                positions.Add(pos);
            }
            
            return positions;
        }
        
        private List<Vector3> GenerateSpiralPositions()
        {
            var positions = new List<Vector3>();
            
            for (int i = 0; i < structureCount; i++)
            {
                float t = (float)i / (structureCount - 1);
                float angle = t * spiralTurns * 2f * Mathf.PI;
                float radius = distributionRadius * t;
                
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );
                positions.Add(pos);
            }
            
            return positions;
        }
        
        private List<Vector3> GenerateGridPositions()
        {
            var positions = new List<Vector3>();
            float spacing = distributionRadius * 2f / Mathf.Max(gridSize.x, gridSize.y);
            
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int z = 0; z < gridSize.y; z++)
                {
                    if (positions.Count >= structureCount) break;
                    
                    Vector3 pos = new Vector3(
                        (x - gridSize.x * 0.5f) * spacing,
                        0,
                        (z - gridSize.y * 0.5f) * spacing
                    );
                    positions.Add(pos);
                }
                if (positions.Count >= structureCount) break;
            }
            
            return positions;
        }
        
        private List<Vector3> GenerateRandomPositions()
        {
            var positions = new List<Vector3>();
            Random.InitState(randomSeed);
            
            for (int i = 0; i < structureCount; i++)
            {
                Vector3 pos = new Vector3(
                    Random.Range(-distributionRadius, distributionRadius),
                    0,
                    Random.Range(-distributionRadius, distributionRadius)
                );
                positions.Add(pos);
            }
            
            return positions;
        }
        
        private List<Vector3> GenerateFractalPositions()
        {
            var positions = new List<Vector3>();
            
            // フラクタル配置の実装（簡単な再帰的配置）
            GenerateFractalRecursive(Vector3.zero, distributionRadius, fractalIterations, positions);
            
            // 必要数に調整
            while (positions.Count > structureCount)
            {
                positions.RemoveAt(positions.Count - 1);
            }
            
            return positions;
        }
        
        private void GenerateFractalRecursive(Vector3 center, float radius, int iterations, List<Vector3> positions)
        {
            if (iterations <= 0 || positions.Count >= structureCount) return;
            
            positions.Add(center);
            
            if (iterations > 1)
            {
                float newRadius = radius * fractalScale;
                Vector3[] offsets = {
                    Vector3.forward * radius,
                    Vector3.back * radius,
                    Vector3.left * radius,
                    Vector3.right * radius
                };
                
                foreach (var offset in offsets)
                {
                    GenerateFractalRecursive(center + offset, newRadius, iterations - 1, positions);
                }
            }
        }
        
        private List<Vector3> GenerateVoronoiPositions()
        {
            var positions = new List<Vector3>();
            
            // ボロノイ図の簡単な実装
            var sites = new List<Vector3>();
            Random.InitState(randomSeed);
            
            // サイト生成
            for (int i = 0; i < voronoiSites; i++)
            {
                Vector3 site = new Vector3(
                    Random.Range(-distributionRadius, distributionRadius),
                    0,
                    Random.Range(-distributionRadius, distributionRadius)
                );
                sites.Add(site);
            }
            
            // 各サイト周辺に構造物を配置
            int structuresPerSite = structureCount / voronoiSites;
            foreach (var site in sites)
            {
                for (int i = 0; i < structuresPerSite && positions.Count < structureCount; i++)
                {
                    Vector3 pos = site + Random.insideUnitSphere * (distributionRadius / voronoiSites);
                    pos.y = 0;
                    positions.Add(pos);
                }
            }
            
            return positions;
        }
        
        private List<Vector3> GenerateOrganicPositions()
        {
            var positions = new List<Vector3>();
            Random.InitState(randomSeed);
            
            // 有機的配置（Poisson Disc Sampling風）
            for (int attempts = 0; attempts < structureCount * 10 && positions.Count < structureCount; attempts++)
            {
                Vector3 candidate = new Vector3(
                    Random.Range(-distributionRadius, distributionRadius),
                    0,
                    Random.Range(-distributionRadius, distributionRadius)
                );
                
                bool validPosition = true;
                float organicMinDistance = minDistance * organicDensity;
                
                foreach (var existingPos in positions)
                {
                    if (Vector3.Distance(candidate, existingPos) < organicMinDistance)
                    {
                        validPosition = false;
                        break;
                    }
                }
                
                if (validPosition)
                {
                    positions.Add(candidate);
                }
            }
            
            return positions;
        }
        
        private List<Vector3> ApplyCollisionAvoidance(List<Vector3> positions)
        {
            var adjustedPositions = new List<Vector3>(positions);
            
            for (int i = 0; i < adjustedPositions.Count; i++)
            {
                for (int j = i + 1; j < adjustedPositions.Count; j++)
                {
                    Vector3 diff = adjustedPositions[j] - adjustedPositions[i];
                    float distance = diff.magnitude;
                    
                    if (distance < minDistance && distance > 0)
                    {
                        Vector3 pushDirection = diff.normalized;
                        float pushAmount = (minDistance - distance) * 0.5f;
                        
                        adjustedPositions[i] -= pushDirection * pushAmount;
                        adjustedPositions[j] += pushDirection * pushAmount;
                    }
                }
            }
            
            return adjustedPositions;
        }
        
        private List<Vector3> ApplyHeightmap(List<Vector3> positions)
        {
            var heightmapPositions = new List<Vector3>();
            
            foreach (var pos in positions)
            {
                Vector2 uv = new Vector2(
                    (pos.x + distributionRadius) / (distributionRadius * 2f),
                    (pos.z + distributionRadius) / (distributionRadius * 2f)
                );
                
                uv.x = Mathf.Clamp01(uv.x);
                uv.y = Mathf.Clamp01(uv.y);
                
                Color heightColor = heightmapTexture.GetPixelBilinear(uv.x, uv.y);
                float height = heightColor.grayscale * heightMultiplier;
                
                Vector3 adjustedPos = new Vector3(pos.x, height, pos.z);
                heightmapPositions.Add(adjustedPos);
            }
            
            return heightmapPositions;
        }
        
        private void PlaceStructuresAtPositions(List<Vector3> positions)
        {
            var parent = new GameObject($"{distributionPattern}Distribution");
            
            foreach (var pos in positions)
            {
                GameObject newStructure;
                
                if (prefabToDistribute != null)
                {
                    newStructure = (GameObject)PrefabUtility.InstantiatePrefab(prefabToDistribute);
                    newStructure.transform.position = pos;
                }
                else
                {
                    newStructure = CreateMonumentInstance(monumentType);
                    newStructure.transform.position = pos;
                }
                
                if (newStructure != null)
                {
                    newStructure.transform.SetParent(parent.transform, true);
                    
                    // 回転とスケール
                    if (randomRotation)
                    {
                        newStructure.transform.rotation = Quaternion.Euler(
                            Random.Range(-rotationRange.x, rotationRange.x),
                            Random.Range(-rotationRange.y, rotationRange.y),
                            Random.Range(-rotationRange.z, rotationRange.z)
                        );
                    }
                    if (randomScale)
                    {
                        float scale = Random.Range(scaleRange.x, scaleRange.y);
                        newStructure.transform.localScale = Vector3.one * scale;
                    }
                    
                    // 地形に合わせる
                    if (alignToSurface)
                    {
                        AlignToSurface(newStructure);
                    }
                }
            }
        }
        
        private GameObject CreateMonumentInstance(AdvancedStructureTab.AdvancedShapeType type)
        {
            // AdvancedStructureTabのインスタンスを作成して、モノリス生成を呼び出す
            var advancedTab = new AdvancedStructureTab(_parent);
            // 注意: この方法は、パラメータがデフォルト値で良い場合にのみ機能します。
            // 本来は、より洗練された共有生成ロジックが必要です。
            
            GameObject monument = null;
            
            // `CreateMonolith`などのメソッドはprivateなので、直接呼び出せません。
            // ここでは、概念的なプレースホルダとして実装します。
            // 実際の使用には、AdvancedStructureTabの生成ロジックをリファクタリングして、
            // 外部から呼び出し可能にする必要があります。
            
            switch (type)
            {
                case AdvancedStructureTab.AdvancedShapeType.Monolith:
                    monument = ShapeGenerator.CreateShape(ShapeType.Cube).gameObject;
                    monument.transform.localScale = new Vector3(0.5f, 4f, 0.5f);
                    break;
                case AdvancedStructureTab.AdvancedShapeType.TwistedTower:
                    monument = ShapeGenerator.CreateShape(ShapeType.Cylinder).gameObject;
                    monument.transform.localScale = new Vector3(1f, 5f, 1f);
                    break;
                case AdvancedStructureTab.AdvancedShapeType.ProceduralColumn:
                    monument = ShapeGenerator.CreateShape(ShapeType.Sphere).gameObject;
                    monument.transform.localScale = Vector3.one * 2f;
                    break;
            }
            
            if (monument != null)
            {
                monument.name = $"{type}Instance";
            }
            
            return monument;
        }
        
        private void AlignToSurface(GameObject obj)
        {
            // 地面に向かってレイキャストして表面に整列
            if (Physics.Raycast(obj.transform.position + Vector3.up * 100f, Vector3.down, out RaycastHit hit))
            {
                obj.transform.position = hit.point;
                obj.transform.up = hit.normal;
            }
        }
        
        private void ClearDistributedObjects()
        {
            GameObject[] distributedObjects = GameObject.FindGameObjectsWithTag("DistributedStructure");
            foreach (var obj in distributedObjects)
            {
                GameObject.DestroyImmediate(obj);
            }
            
            GameObject parentObject = GameObject.Find("Distributed Structures");
            if (parentObject != null)
            {
                GameObject.DestroyImmediate(parentObject);
            }
            
            Debug.Log("Cleared all distributed objects");
        }
        
        private void PreviewDistribution()
        {
            // プレビュー用のギズモ描画（実装は後で）
            Debug.Log("Distribution preview will be shown in Scene view");
        }
    }
} 