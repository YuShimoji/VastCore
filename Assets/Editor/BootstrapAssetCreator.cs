using UnityEditor;
using UnityEngine;
using Vastcore.Terrain.Config;
using Vastcore.Terrain.DualGrid;
using Vastcore.Terrain.Erosion;
using Vastcore.Player;

namespace Vastcore.Editor
{
    /// <summary>
    /// エンジン動作に必要なブートストラップアセットを一括生成する Editor ユーティリティ。
    /// メニュー: Vastcore > Bootstrap > Create All Required Assets
    /// </summary>
    public static class BootstrapAssetCreator
    {
        private const string k_AssetRoot = "Assets/Resources/Bootstrap";

        [MenuItem("Vastcore/Bootstrap/Create All Required Assets")]
        public static void CreateAllAssets()
        {
            EnsureDirectory(k_AssetRoot);

            var noiseSettings = CreateNoiseHeightmapSettings();
            var erosionSettings = CreateErosionSettings();
            var terrainConfig = CreateTerrainGenerationConfig(noiseSettings, erosionSettings);
            var stampDef = CreatePrefabStampDefinition();
            var playerPrefab = CreatePlayerPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BootstrapAssetCreator] All bootstrap assets created in {k_AssetRoot}/");

            EditorUtility.DisplayDialog(
                "Bootstrap Assets Created",
                $"以下のアセットを {k_AssetRoot}/ に生成しました:\n\n" +
                "1. NoiseHeightmap_Default — ハイトマップ設定\n" +
                "2. Erosion_Default — エロージョン設定\n" +
                "3. TerrainConfig_Default — 地形生成設定\n" +
                "4. Stamp_Cube — テスト用スタンプ定義\n" +
                "5. Player_Minimal — 最小プレイヤーPrefab\n\n" +
                "使い方:\n" +
                "- TerrainGridBootstrap の config に TerrainConfig_Default をアサイン\n" +
                "- TerrainWithStampsBootstrap の config + stampDefinition をアサイン\n" +
                "- VastcoreGameManager の PlayerPrefab に Player_Minimal をアサイン\n" +
                "- Play で動作確認",
                "OK");
        }

        [MenuItem("Vastcore/Bootstrap/Create Terrain Config Only")]
        public static void CreateTerrainConfigOnly()
        {
            EnsureDirectory(k_AssetRoot);
            var noiseSettings = CreateNoiseHeightmapSettings();
            var erosionSettings = CreateErosionSettings();
            CreateTerrainGenerationConfig(noiseSettings, erosionSettings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BootstrapAssetCreator] Terrain config assets created.");
        }

        private static NoiseHeightmapSettings CreateNoiseHeightmapSettings()
        {
            string path = $"{k_AssetRoot}/NoiseHeightmap_Default.asset";
            var existing = AssetDatabase.LoadAssetAtPath<NoiseHeightmapSettings>(path);
            if (existing != null)
            {
                Debug.Log($"[Bootstrap] {path} already exists, skipping.");
                return existing;
            }

            var settings = ScriptableObject.CreateInstance<NoiseHeightmapSettings>();
            settings.seed = 12345;
            settings.scale = 200f;
            settings.octaves = 5;
            settings.lacunarity = 2f;
            settings.gain = 0.5f;
            settings.offset = Vector2.zero;
            settings.domainWarp = true;
            settings.warpStrength = 15f;
            settings.warpFrequency = 0.008f;

            AssetDatabase.CreateAsset(settings, path);
            Debug.Log($"[Bootstrap] Created {path}");
            return settings;
        }

        private static ErosionSettings CreateErosionSettings()
        {
            string path = $"{k_AssetRoot}/Erosion_Default.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ErosionSettings>(path);
            if (existing != null)
            {
                Debug.Log($"[Bootstrap] {path} already exists, skipping.");
                return existing;
            }

            var settings = ScriptableObject.CreateInstance<ErosionSettings>();
            settings.enabled = true;
            settings.erosionSeed = 42;
            settings.enableHydraulic = true;
            settings.hydraulicIterations = 30000;
            settings.erosionRate = 0.3f;
            settings.depositionRate = 0.3f;
            settings.enableThermal = true;
            settings.thermalIterations = 30;
            settings.talusAngle = 0.6f;

            AssetDatabase.CreateAsset(settings, path);
            Debug.Log($"[Bootstrap] Created {path}");
            return settings;
        }

        private static TerrainGenerationConfig CreateTerrainGenerationConfig(
            NoiseHeightmapSettings _noiseSettings, ErosionSettings _erosionSettings)
        {
            string path = $"{k_AssetRoot}/TerrainConfig_Default.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TerrainGenerationConfig>(path);
            if (existing != null)
            {
                Debug.Log($"[Bootstrap] {path} already exists, skipping.");
                return existing;
            }

            var config = ScriptableObject.CreateInstance<TerrainGenerationConfig>();
            config.heightmapSettings = _noiseSettings;
            config.resolution = 129; // 小さめで高速
            config.worldSize = 256f;
            config.heightScale = 80f;
            config.erosionSettings = _erosionSettings;

            AssetDatabase.CreateAsset(config, path);
            Debug.Log($"[Bootstrap] Created {path}");
            return config;
        }

        private static PrefabStampDefinition CreatePrefabStampDefinition()
        {
            string path = $"{k_AssetRoot}/Stamp_Cube.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PrefabStampDefinition>(path);
            if (existing != null)
            {
                Debug.Log($"[Bootstrap] {path} already exists, skipping.");
                return existing;
            }

            // テスト用 Cube Prefab を作成
            string prefabPath = $"{k_AssetRoot}/StampPrefab_Cube.prefab";
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject stampPrefab;
            if (existingPrefab != null)
            {
                stampPrefab = existingPrefab;
            }
            else
            {
                var tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tempCube.name = "StampCube";
                tempCube.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                stampPrefab = PrefabUtility.SaveAsPrefabAsset(tempCube, prefabPath);
                Object.DestroyImmediate(tempCube);
            }

            var def = ScriptableObject.CreateInstance<PrefabStampDefinition>();
            // Reflection で private field に Prefab を設定
            var prefabField = typeof(PrefabStampDefinition).GetField("m_Prefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            prefabField?.SetValue(def, stampPrefab);

            var nameField = typeof(PrefabStampDefinition).GetField("m_DisplayName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            nameField?.SetValue(def, "TestCube");

            AssetDatabase.CreateAsset(def, path);
            Debug.Log($"[Bootstrap] Created {path} + {prefabPath}");
            return def;
        }

        private static GameObject CreatePlayerPrefab()
        {
            string path = $"{k_AssetRoot}/Player_Minimal.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                Debug.Log($"[Bootstrap] {path} already exists, skipping.");
                return existing;
            }

            // Capsule + AdvancedPlayerController + Camera
            var playerGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerGo.name = "Player_Minimal";
            playerGo.tag = "Player";

            // CharacterController は AdvancedPlayerController の RequireComponent で自動追加
            var controller = playerGo.AddComponent<AdvancedPlayerController>();
            controller.moveSpeed = 10f;

            // カメラを子オブジェクトとして追加
            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(playerGo.transform);
            camGo.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            var cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";

            // AdvancedPlayerController.cameraTransform を設定
            controller.cameraTransform = camGo.transform;

            // Prefab として保存
            var prefab = PrefabUtility.SaveAsPrefabAsset(playerGo, path);
            Object.DestroyImmediate(playerGo);

            Debug.Log($"[Bootstrap] Created {path}");
            return prefab;
        }

        private static void EnsureDirectory(string _path)
        {
            if (!AssetDatabase.IsValidFolder(_path))
            {
                string[] parts = _path.Split('/');
                string current = parts[0]; // "Assets"
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }
        }
    }
}
