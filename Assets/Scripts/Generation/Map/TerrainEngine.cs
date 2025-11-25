using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Generation
{
    /// <summary>
    /// 地形生成エンジンのメインクラス
    /// デザイナーテンプレートとプロシージャル生成を統合管理
    /// </summary>
    public class TerrainEngine : MonoBehaviour
    {
        #region シングルトン
        private static TerrainEngine instance;
        public static TerrainEngine Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<TerrainEngine>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("TerrainEngine");
                        instance = go.AddComponent<TerrainEngine>();
                    }
                }
                return instance;
            }
        }
        #endregion

        [Header("エンジン設定")]
        public bool enableTerrainEngine = true;
        public TerrainEngineMode generationMode = TerrainEngineMode.Hybrid;

        [Header("ジオメトリ設定")]
        public TerrainGeometryType geometryType = TerrainGeometryType.UnityTerrain;
        public bool enableBoxTerrainSupport = false;
        public Material boxTerrainMaterial;

        [Header("テンプレート設定")]
        public List<DesignerTerrainTemplate> availableTemplates = new List<DesignerTerrainTemplate>();
        public bool autoRegisterTemplates = true;

        [Header("プロシージャル設定")]
        public BiomeSystem biomeSystem;
        public ClimateSystem climateSystem;

        [Header("パフォーマンス設定")]
        public int maxConcurrentGenerations = 4;
        public bool useMultithreading = false;

        // 内部状態
        private Dictionary<Vector2Int, TerrainTileComponent> activeTiles = new Dictionary<Vector2Int, TerrainTileComponent>();
        private Queue<TerrainGenerationTask> generationQueue = new Queue<TerrainGenerationTask>();
        private HashSet<Vector2Int> processingTiles = new HashSet<Vector2Int>();
        private bool isInitialized = false;

        // コンポーネント参照
        private BoxTerrainGenerator boxTerrainGenerator;

        #region Unity Lifecycle

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeEngine();
        }

        private void Update()
        {
            if (!enableTerrainEngine || !isInitialized) return;

            ProcessGenerationQueue();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region 初期化

        /// <summary>
        /// エンジンを初期化
        /// </summary>
        public void InitializeEngine()
        {
            if (isInitialized) return;

            try
            {
                // 依存コンポーネントの初期化
                InitializeBiomeSystem();
                InitializeClimateSystem();
                InitializeBoxTerrainGenerator();

                // デザイナーテンプレートの登録
                if (autoRegisterTemplates)
                {
                    RegisterAllTemplates();
                }

                isInitialized = true;
                Debug.Log("TerrainEngine initialized successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"TerrainEngine initialization failed: {e.Message}");
            }
        }

        /// <summary>
        /// バイオームシステムを初期化
        /// </summary>
        private void InitializeBiomeSystem()
        {
            if (biomeSystem == null)
            {
                biomeSystem = GetComponent<BiomeSystem>();
                if (biomeSystem == null)
                {
                    biomeSystem = gameObject.AddComponent<BiomeSystem>();
                }
            }
            biomeSystem.Initialize();
        }

        /// <summary>
        /// 気候システムを初期化
        /// </summary>
        private void InitializeClimateSystem()
        {
            if (climateSystem == null)
            {
                climateSystem = GetComponent<ClimateSystem>();
                if (climateSystem == null)
                {
                    climateSystem = gameObject.AddComponent<ClimateSystem>();
                }
            }
            climateSystem.Initialize();
        }

        /// <summary>
        /// 箱型地形ジェネレーターを初期化
        /// </summary>
        private void InitializeBoxTerrainGenerator()
        {
            if (enableBoxTerrainSupport && geometryType == TerrainGeometryType.BoxTerrain)
            {
                boxTerrainGenerator = GetComponent<BoxTerrainGenerator>();
                if (boxTerrainGenerator == null)
                {
                    boxTerrainGenerator = gameObject.AddComponent<BoxTerrainGenerator>();
                }

                // 設定の適用
                if (boxTerrainGenerator != null && boxTerrainMaterial != null)
                {
                    boxTerrainGenerator.topMaterial = boxTerrainMaterial;
                    boxTerrainGenerator.bottomMaterial = boxTerrainMaterial;
                    boxTerrainGenerator.sideMaterial = boxTerrainMaterial;
                }
            }
        }

        /// <summary>
        /// 全テンプレートを登録
        /// </summary>
        private void RegisterAllTemplates()
        {
            foreach (var template in availableTemplates)
            {
                if (template != null)
                {
                    // BiomeSpecificTerrainGenerator.RegisterTemplate(template);
                }
            }
        }

        #endregion

        #region 地形生成API

        /// <summary>
        /// 指定座標の地形タイルを生成
        /// </summary>
        public TerrainTileComponent GenerateTerrainTile(Vector2Int coordinate, Vector3 worldPosition)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("TerrainEngine not initialized");
                return null;
            }

            // 既にアクティブなタイルがあるかチェック
            if (activeTiles.ContainsKey(coordinate))
            {
                return activeTiles[coordinate];
            }

            // 生成タスクを作成
            var task = new TerrainGenerationTask
            {
                coordinate = coordinate,
                worldPosition = worldPosition,
                priority = TerrainGenerationPriority.Normal,
                generationMode = generationMode
            };

            // キューに追加
            generationQueue.Enqueue(task);

            return null; // 非同期生成のためnullを返す
        }

        /// <summary>
        /// 地形タイルを同期生成
        /// </summary>
        public TerrainTileComponent GenerateTerrainTileSync(Vector2Int coordinate, Vector3 worldPosition)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("TerrainEngine not initialized");
                return null;
            }

            // 既にアクティブなタイルがあるかチェック
            if (activeTiles.ContainsKey(coordinate))
            {
                return activeTiles[coordinate];
            }

            // 直接生成
            TerrainTileComponent tile = CreateTerrainTile(coordinate, worldPosition);
            GenerateTileContent(tile, generationMode);

            activeTiles[coordinate] = tile;
            return tile;
        }

        /// <summary>
        /// 地形タイルを削除
        /// </summary>
        public void RemoveTerrainTile(Vector2Int coordinate)
        {
            if (activeTiles.ContainsKey(coordinate))
            {
                var tile = activeTiles[coordinate];
                Destroy(tile.gameObject);
                activeTiles.Remove(coordinate);
            }
        }

        #endregion

        #region 生成処理

        /// <summary>
        /// 生成キューを処理
        /// </summary>
        private void ProcessGenerationQueue()
        {
            int processedCount = 0;

            while (generationQueue.Count > 0 && processedCount < maxConcurrentGenerations)
            {
                var task = generationQueue.Dequeue();

                if (!processingTiles.Contains(task.coordinate))
                {
                    processingTiles.Add(task.coordinate);
                    StartCoroutine(GenerateTileAsync(task));
                    processedCount++;
                }
            }
        }

        /// <summary>
        /// 非同期地形生成
        /// </summary>
        private System.Collections.IEnumerator GenerateTileAsync(TerrainGenerationTask task)
        {
            // タイル作成
            TerrainTileComponent tile = CreateTerrainTile(task.coordinate, task.worldPosition);

            // コンテンツ生成（重い処理なのでコルーチン内で）
            yield return StartCoroutine(GenerateTileContentAsync(tile, task.generationMode));

            // 完了処理
            activeTiles[task.coordinate] = tile;
            processingTiles.Remove(task.coordinate);

            Debug.Log($"Terrain tile generated: {task.coordinate}");
        }

        /// <summary>
        /// タイルコンテンツを生成（同期）
        /// </summary>
        private void GenerateTileContent(TerrainTileComponent tile, TerrainEngineMode mode)
        {
            switch (mode)
            {
                case TerrainEngineMode.TemplateOnly:
                    GenerateFromTemplates(tile);
                    break;
                case TerrainEngineMode.ProceduralOnly:
                    GenerateProcedural(tile);
                    break;
                case TerrainEngineMode.Hybrid:
                    GenerateHybrid(tile);
                    break;
            }
        }

        /// <summary>
        /// タイルコンテンツを生成（非同期）
        /// </summary>
        private System.Collections.IEnumerator GenerateTileContentAsync(TerrainTileComponent tile, TerrainEngineMode mode)
        {
            switch (mode)
            {
                case TerrainEngineMode.TemplateOnly:
                    GenerateFromTemplates(tile);
                    break;
                case TerrainEngineMode.ProceduralOnly:
                    GenerateProcedural(tile);
                    break;
                case TerrainEngineMode.Hybrid:
                    GenerateHybrid(tile);
                    break;
            }

            yield return null;
        }

        /// <summary>
        /// テンプレートベース生成
        /// </summary>
        private void GenerateFromTemplates(TerrainTileComponent tile)
        {
            // バイオーム判定
            BiomeType biomeType = DetermineBiomeAtPosition(tile.worldPosition);

            // テンプレート適用
            // var template = BiomeSpecificTerrainGenerator.GetRandomTemplateForBiome(biomeType, tile.worldPosition);
            // if (template != null)
            // {
            //     TerrainSynthesizer.SynthesizeTerrain(tile.heightData, template, tile.worldPosition);
            // }
            // else
            // {
                // フォールバック：ベース地形生成
                GenerateBaseTerrain(tile);
            // }
        }

        /// <summary>
        /// プロシージャル生成
        /// </summary>
        private void GenerateProcedural(TerrainTileComponent tile)
        {
            // バイオーム固有のプロシージャル生成
            BiomeType biomeType = DetermineBiomeAtPosition(tile.worldPosition);
            var biomeDefinition = biomeSystem?.GetBiomeDefinition(biomeType);

            if (biomeDefinition != null)
            {
                // バイオーム定義に基づくプロシージャル生成
                // BiomeSpecificTerrainGenerator.GenerateProceduralTerrain(tile.heightData, biomeDefinition, tile.worldPosition);
            }
            else
            {
                // フォールバック：ベース地形生成
                GenerateBaseTerrain(tile);
            }
        }

        /// <summary>
        /// ハイブリッド生成
        /// </summary>
        private void GenerateHybrid(TerrainTileComponent tile)
        {
            // まずテンプレートを試行
            GenerateFromTemplates(tile);

            // バイオーム固有の修正を追加
            BiomeType biomeType = DetermineBiomeAtPosition(tile.worldPosition);
            var biomeDefinition = biomeSystem?.GetBiomeDefinition(biomeType);

            if (biomeDefinition != null && biomeDefinition.terrainModifiers != null)
            {
                // BiomeSpecificTerrainGenerator.ApplyTerrainModifiers(tile.heightData, biomeDefinition.terrainModifiers);
            }
        }

        /// <summary>
        /// ベース地形生成
        /// </summary>
        private void GenerateBaseTerrain(TerrainTileComponent tile)
        {
            // シンプルなノイズベース地形
            int width = tile.heightData.GetLength(0);
            int height = tile.heightData.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float worldX = tile.worldPosition.x + x;
                    float worldZ = tile.worldPosition.z + y;

                    float noise1 = Mathf.PerlinNoise(worldX * 0.01f, worldZ * 0.01f);
                    float noise2 = Mathf.PerlinNoise(worldX * 0.02f, worldZ * 0.02f) * 0.5f;

                    tile.heightData[x, y] = (noise1 + noise2) * 50f;
                }
            }
        }

        #endregion

        #region ユーティリティ

        /// <summary>
        /// タイルオブジェクトを作成
        /// </summary>
        private TerrainTileComponent CreateTerrainTile(Vector2Int coordinate, Vector3 worldPosition)
        {
            GameObject tileObject = new GameObject($"TerrainTile_{coordinate.x}_{coordinate.y}");
            tileObject.transform.position = worldPosition;

            TerrainTileComponent tile = tileObject.AddComponent<TerrainTileComponent>();
            InitializeTerrainTile(tile, coordinate, worldPosition);

            return tile;
        }

        /// <summary>
        /// TerrainTile を初期化
        /// </summary>
        private void InitializeTerrainTile(TerrainTileComponent tile, Vector2Int coordinate, Vector3 worldPosition)
        {
            tile.coordinate = coordinate;
            tile.worldPosition = worldPosition;

            // ジオメトリタイプに応じた初期化
            switch (geometryType)
            {
                case TerrainGeometryType.UnityTerrain:
                    // Unity Terrainコンポーネントの初期化
                    tile.heightData = new float[256, 256]; // 256x256の地形データ
                    break;

                case TerrainGeometryType.BoxTerrain:
                    // 箱型地形の場合、BoxTerrainGeneratorで処理
                    if (boxTerrainGenerator != null)
                    {
                        boxTerrainGenerator.GenerateBoxTerrain(worldPosition);
                    }
                    tile.heightData = new float[1, 1]; // 最小データ
                    break;

                case TerrainGeometryType.MeshTerrain:
                case TerrainGeometryType.CustomMesh:
                    // メッシュベース地形の初期化（将来拡張）
                    tile.heightData = new float[256, 256];
                    break;
            }
        }

        /// <summary>
        /// 指定位置のバイオームを判定
        /// </summary>
        private BiomeType DetermineBiomeAtPosition(Vector3 worldPosition)
        {
            if (biomeSystem != null)
            {
                return biomeSystem.DetermineBiome(worldPosition);
            }

            // フォールバック：位置に基づく簡易判定
            float temperature = Mathf.PerlinNoise(worldPosition.x * 0.001f, worldPosition.z * 0.001f) * 50f;
            float moisture = Mathf.PerlinNoise(worldPosition.x * 0.0015f + 100f, worldPosition.z * 0.0015f + 100f) * 2000f;

            if (moisture < 300f) return BiomeType.Desert;
            if (temperature < 10f) return BiomeType.Mountain;
            if (moisture > 1000f) return BiomeType.Forest;

            return BiomeType.Grassland;
        }

        /// <summary>
        /// アクティブなタイルを取得
        /// </summary>
        public Dictionary<Vector2Int, TerrainTileComponent> GetActiveTiles()
        {
            return new Dictionary<Vector2Int, TerrainTileComponent>(activeTiles);
        }

        /// <summary>
        /// 指定座標のタイルを取得
        /// </summary>
        public TerrainTileComponent GetTileAt(Vector2Int coordinate)
        {
            return activeTiles.ContainsKey(coordinate) ? activeTiles[coordinate] : null;
        }

        /// <summary>
        /// クリーンアップ
        /// </summary>
        private void Cleanup()
        {
            foreach (var tile in activeTiles.Values)
            {
                if (tile != null && tile.gameObject != null)
                {
                    Destroy(tile.gameObject);
                }
            }

            activeTiles.Clear();
            generationQueue.Clear();
            processingTiles.Clear();
        }

        #endregion
    }

    /// <summary>
    /// 地形のジオメトリタイプ
    /// </summary>
    public enum TerrainGeometryType
    {
        UnityTerrain,      // Unity Terrainコンポーネント
        MeshTerrain,       // 3Dメッシュベース地形
        BoxTerrain,        // 箱型地形（六面体）
        CustomMesh         // カスタムメッシュ
    }

    /// <summary>
    /// 高度な TerrainEngine 用の生成モード
    /// テンプレート / プロシージャル / ハイブリッド
    /// </summary>
    public enum TerrainEngineMode
    {
        TemplateOnly,       // デザイナーテンプレートのみ
        ProceduralOnly,     // プロシージャルのみ
        Hybrid              // ハイブリッド（テンプレート優先＋プロシージュアル修正）
    }

    /// <summary>
    /// 地形生成タスク
    /// </summary>
    public class TerrainGenerationTask
    {
        public Vector2Int coordinate;
        public Vector3 worldPosition;
        public TerrainGenerationPriority priority;
        public TerrainEngineMode generationMode;
    }

    /// <summary>
    /// 地形生成優先度
    /// </summary>
    public enum TerrainGenerationPriority
    {
        Low,
        Normal,
        High,
        Immediate
    }
}
