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
        public TerrainGenerationMode generationMode = TerrainGenerationMode.Hybrid;

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
        private Dictionary<Vector2Int, TerrainTile> activeTiles = new Dictionary<Vector2Int, TerrainTile>();
        private Queue<TerrainGenerationTask> generationQueue = new Queue<TerrainGenerationTask>();
        private HashSet<Vector2Int> processingTiles = new HashSet<Vector2Int>();
        private bool isInitialized = false;

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
        /// 全テンプレートを登録
        /// </summary>
        private void RegisterAllTemplates()
        {
            foreach (var template in availableTemplates)
            {
                if (template != null)
                {
                    BiomeSpecificTerrainGenerator.RegisterTemplate(template);
                }
            }
        }

        #endregion

        #region 地形生成API

        /// <summary>
        /// 指定座標の地形タイルを生成
        /// </summary>
        public TerrainTile GenerateTerrainTile(Vector2Int coordinate, Vector3 worldPosition)
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
        public TerrainTile GenerateTerrainTileSync(Vector2Int coordinate, Vector3 worldPosition)
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
            TerrainTile tile = CreateTerrainTile(coordinate, worldPosition);
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
            TerrainTile tile = CreateTerrainTile(task.coordinate, task.worldPosition);

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
        private void GenerateTileContent(TerrainTile tile, TerrainGenerationMode mode)
        {
            switch (mode)
            {
                case TerrainGenerationMode.TemplateOnly:
                    GenerateFromTemplates(tile);
                    break;
                case TerrainGenerationMode.ProceduralOnly:
                    GenerateProcedural(tile);
                    break;
                case TerrainGenerationMode.Hybrid:
                    GenerateHybrid(tile);
                    break;
            }
        }

        /// <summary>
        /// タイルコンテンツを生成（非同期）
        /// </summary>
        private System.Collections.IEnumerator GenerateTileContentAsync(TerrainTile tile, TerrainGenerationMode mode)
        {
            switch (mode)
            {
                case TerrainGenerationMode.TemplateOnly:
                    GenerateFromTemplates(tile);
                    break;
                case TerrainGenerationMode.ProceduralOnly:
                    GenerateProcedural(tile);
                    break;
                case TerrainGenerationMode.Hybrid:
                    GenerateHybrid(tile);
                    break;
            }

            yield return null;
        }

        /// <summary>
        /// テンプレートベース生成
        /// </summary>
        private void GenerateFromTemplates(TerrainTile tile)
        {
            // バイオーム判定
            BiomeType biomeType = DetermineBiomeAtPosition(tile.worldPosition);

            // テンプレート適用
            var template = BiomeSpecificTerrainGenerator.GetRandomTemplateForBiome(biomeType, tile.worldPosition);
            if (template != null)
            {
                TerrainSynthesizer.SynthesizeTerrain(tile.heightData, template, tile.worldPosition);
            }
            else
            {
                // フォールバック：ベース地形生成
                GenerateBaseTerrain(tile);
            }
        }

        /// <summary>
        /// プロシージャル生成
        /// </summary>
        private void GenerateProcedural(TerrainTile tile)
        {
            // バイオーム判定
            BiomeType biomeType = DetermineBiomeAtPosition(tile.worldPosition);
            var biomeDefinition = biomeSystem?.GetBiomeDefinition(biomeType);

            // プロシージャル生成
            BiomeSpecificTerrainGenerator.GenerateTerrainForBiome(
                tile.heightData,
                biomeType,
                biomeDefinition,
                tile.worldPosition
            );
        }

        /// <summary>
        /// ハイブリッド生成
        /// </summary>
        private void GenerateHybrid(TerrainTile tile)
        {
            // まずテンプレートを試行
            GenerateFromTemplates(tile);

            // バイオーム固有の修正を追加
            BiomeType biomeType = DetermineBiomeAtPosition(tile.worldPosition);
            var biomeDefinition = biomeSystem?.GetBiomeDefinition(biomeType);

            if (biomeDefinition != null && biomeDefinition.terrainModifiers != null)
            {
                BiomeSpecificTerrainGenerator.ApplyTerrainModifiers(tile.heightData, biomeDefinition.terrainModifiers);
            }
        }

        /// <summary>
        /// ベース地形生成
        /// </summary>
        private void GenerateBaseTerrain(TerrainTile tile)
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
        private TerrainTile CreateTerrainTile(Vector2Int coordinate, Vector3 worldPosition)
        {
            GameObject tileObject = new GameObject($"TerrainTile_{coordinate.x}_{coordinate.y}");
            tileObject.transform.position = worldPosition;

            TerrainTile tile = tileObject.AddComponent<TerrainTile>();
            tile.coordinate = coordinate;
            tile.worldPosition = worldPosition;

            // 地形データの初期化（仮定値）
            tile.heightData = new float[256, 256]; // 256x256の地形データ

            return tile;
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
        public Dictionary<Vector2Int, TerrainTile> GetActiveTiles()
        {
            return new Dictionary<Vector2Int, TerrainTile>(activeTiles);
        }

        /// <summary>
        /// 指定座標のタイルを取得
        /// </summary>
        public TerrainTile GetTileAt(Vector2Int coordinate)
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
    /// 地形生成モード
    /// </summary>
    public enum TerrainGenerationMode
    {
        TemplateOnly,       // デザイナーテンプレートのみ
        ProceduralOnly,     // プロシージャルのみ
        Hybrid             // ハイブリッド（テンプレート優先＋プロシージャル修正）
    }

    /// <summary>
    /// 地形生成タスク
    /// </summary>
    public class TerrainGenerationTask
    {
        public Vector2Int coordinate;
        public Vector3 worldPosition;
        public TerrainGenerationPriority priority;
        public TerrainGenerationMode generationMode;
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
