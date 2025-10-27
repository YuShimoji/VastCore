<<<<<<< HEAD
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Vastcore.Generation
{
    /// <summary>
    /// バイオームプリセット管理システム
    /// プリセットの保存・読み込み・管理を行う
    /// </summary>
    public class BiomePresetManager : MonoBehaviour
    {
        #region シングルトン

        private static BiomePresetManager _instance;
        public static BiomePresetManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<BiomePresetManager>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("BiomePresetManager");
                        _instance = obj.AddComponent<BiomePresetManager>();
                        DontDestroyOnLoad(obj);
                    }
                }
                return _instance;
            }
        }

        #endregion

        [Header("プリセット管理設定")]
        public string presetSavePath = "Assets/Data/BiomePresets/";
        public string presetFileExtension = ".asset";
        public bool autoLoadPresetsOnStart = true;
        public bool createDefaultPresets = true;

        [Header("現在のプリセット")]
        public BiomePreset currentPreset;

        [Header("利用可能なプリセット")]
        public List<BiomePreset> availablePresets = new List<BiomePreset>();

        public System.Action<BiomePreset> OnPresetLoaded;
        public System.Action<BiomePreset> OnPresetSaved;
        public System.Action<List<BiomePreset>> OnPresetsRefreshed;

        private Dictionary<string, BiomePreset> presetCache = new Dictionary<string, BiomePreset>();
        private bool isInitialized;

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            Initialize();
        }

        #endregion

        #region 初期化

        public void Initialize()
        {
            if (isInitialized) return;

            try
            {
                EnsureDirectoryExists();

                if (createDefaultPresets)
                {
                    CreateDefaultPresets();
                }

                if (autoLoadPresetsOnStart)
                {
                    RefreshAvailablePresets();
                }

                isInitialized = true;
                Debug.Log($"BiomePresetManager initialized. Found {availablePresets.Count} presets.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"BiomePresetManager initialization failed: {e.Message}");
            }
        }

        private void EnsureDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(presetSavePath))
                {
                    Directory.CreateDirectory(presetSavePath);
                    Debug.Log($"Created BiomePreset directory: {presetSavePath}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"EnsureDirectoryExists failed: {e.Message}");
            }
        }

        #endregion

        #region プリセット保存・読み込み

        public bool SavePreset(BiomePreset preset)
        {
            if (preset == null)
            {
                Debug.LogError("SavePreset: プリセットがnullです");
                return false;
            }

            try
            {
                if (!preset.ValidatePreset())
                {
                    Debug.LogError("SavePreset: プリセットの妥当性検証に失敗しました");
                    return false;
                }

                string fileName = SanitizeFileName(preset.presetName) + presetFileExtension;
                string fullPath = Path.Combine(presetSavePath, fileName);

#if UNITY_EDITOR
                if (!AssetDatabase.IsValidFolder(presetSavePath.TrimEnd('/')))
                {
                    CreateFolderRecursive(presetSavePath.TrimEnd('/'));
                }

                if (AssetDatabase.Contains(preset))
                {
                    EditorUtility.SetDirty(preset);
                }
                else
                {
                    AssetDatabase.CreateAsset(preset, fullPath);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
#else
                Debug.LogWarning("SavePreset: エディタ外でのプリセット保存は未対応です");
                return false;
#endif

                presetCache[preset.presetName] = preset;
                if (!availablePresets.Contains(preset))
                {
                    availablePresets.Add(preset);
                }

                OnPresetSaved?.Invoke(preset);
                Debug.Log($"BiomePreset saved: {preset.presetName} at {fullPath}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"SavePreset failed: {e.Message}");
                return false;
            }
        }

        public BiomePreset LoadPreset(string presetName)
        {
            if (string.IsNullOrEmpty(presetName))
            {
                Debug.LogError("LoadPreset: プリセット名が無効です");
                return null;
            }

            try
            {
                if (presetCache.TryGetValue(presetName, out var cachedPreset) && cachedPreset != null)
                {
                    currentPreset = cachedPreset;
                    OnPresetLoaded?.Invoke(cachedPreset);
                    return cachedPreset;
                }

                string fileName = SanitizeFileName(presetName) + presetFileExtension;
                string fullPath = Path.Combine(presetSavePath, fileName);

#if UNITY_EDITOR
                var preset = AssetDatabase.LoadAssetAtPath<BiomePreset>(fullPath);
#else
                var preset = Resources.Load<BiomePreset>(Path.GetFileNameWithoutExtension(fileName));
#endif

                if (preset == null)
                {
                    Debug.LogWarning($"LoadPreset: プリセットが見つかりません: {fullPath}");
                    return null;
                }

                presetCache[presetName] = preset;
                currentPreset = preset;
                if (!availablePresets.Contains(preset))
                {
                    availablePresets.Add(preset);
                }

                OnPresetLoaded?.Invoke(preset);
                Debug.Log($"BiomePreset loaded: {presetName}");
                return preset;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"LoadPreset failed: {e.Message}");
                return null;
            }
        }

        public bool DeletePreset(string presetName)
        {
            if (string.IsNullOrEmpty(presetName))
            {
                Debug.LogError("DeletePreset: プリセット名が無効です");
                return false;
            }

            try
            {
                string fileName = SanitizeFileName(presetName) + presetFileExtension;
                string fullPath = Path.Combine(presetSavePath, fileName);

#if UNITY_EDITOR
                if (AssetDatabase.DeleteAsset(fullPath))
                {
                    AssetDatabase.Refresh();
                }
#else
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
#endif

                presetCache.Remove(presetName);
                availablePresets.RemoveAll(p => p != null && p.presetName == presetName);

                Debug.Log($"BiomePreset deleted: {presetName}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"DeletePreset failed: {e.Message}");
                return false;
            }
        }

        #endregion

        #region プリセット管理

        public void RefreshAvailablePresets()
        {
            try
            {
                availablePresets.Clear();
                presetCache.Clear();

                if (!Directory.Exists(presetSavePath))
                {
                    Debug.LogWarning($"RefreshAvailablePresets: ディレクトリが存在しません: {presetSavePath}");
                    return;
                }

                string[] presetFiles = Directory.GetFiles(presetSavePath, "*" + presetFileExtension);

                foreach (string filePath in presetFiles)
                {
#if UNITY_EDITOR
                    var preset = AssetDatabase.LoadAssetAtPath<BiomePreset>(filePath);
#else
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    var preset = Resources.Load<BiomePreset>(fileName);
#endif

                    if (preset != null && preset.ValidatePreset())
                    {
                        availablePresets.Add(preset);
                        presetCache[preset.presetName] = preset;
                    }
                }

                availablePresets = availablePresets.Where(p => p != null).OrderBy(p => p.presetName).ToList();
                OnPresetsRefreshed?.Invoke(availablePresets);

                Debug.Log($"RefreshAvailablePresets: {availablePresets.Count} presets loaded");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"RefreshAvailablePresets failed: {e.Message}");
            }
        }

        public List<string> GetAvailablePresetNames()
        {
            return availablePresets.Where(p => p != null).Select(p => p.presetName).ToList();
        }

        public bool PresetExists(string presetName)
        {
            if (string.IsNullOrEmpty(presetName)) return false;
            return availablePresets.Any(p => p != null && p.presetName == presetName);
        }

        public void ApplyPresetToTerrain(string presetName, TerrainTile targetTile)
        {
            var preset = LoadPreset(presetName);
            if (preset == null)
            {
                Debug.LogError($"ApplyPresetToTerrain: プリセットが見つかりません: {presetName}");
                return;
            }

            ApplyPresetToTerrain(preset, targetTile);
        }

        public void ApplyPresetToTerrain(BiomePreset preset, TerrainTile targetTile)
        {
            if (preset == null || targetTile == null)
            {
                Debug.LogError("ApplyPresetToTerrain: プリセットまたはタイルがnullです");
                return;
            }

            try
            {
                targetTile.appliedBiome = preset;
                ApplyEnvironmentSettings(preset.environmentSettings);
                ApplyMaterialSettings(preset.materialSettings, targetTile);

                Debug.Log($"BiomePreset applied to terrain: {preset.presetName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ApplyPresetToTerrain failed: {e.Message}");
            }
        }

        #endregion

        #region デフォルトプリセット作成

        private void CreateDefaultPresets()
        {
            try
            {
                CreateDefaultPreset("Plains", "広大な平原バイオーム", 0.4f, 0.6f, 0.7f, 0.2f);
                CreateDefaultPreset("Mountains", "険しい山岳バイオーム", 0.3f, 0.3f, 0.3f, 0.9f);
                CreateDefaultPreset("Desert", "乾燥した砂漠バイオーム", 0.1f, 0.8f, 0.2f, 0.6f);
                CreateDefaultPreset("Forest", "緑豊かな森林バイオーム", 0.8f, 0.5f, 0.9f, 0.3f);
                CreateDefaultPreset("Tundra", "寒冷なツンドラバイオーム", 0.6f, 0.1f, 0.2f, 0.7f);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CreateDefaultPresets failed: {e.Message}");
            }
        }

        private void CreateDefaultPreset(string name, string description, float moisture, float temperature, float fertility, float rockiness)
        {
            string fileName = SanitizeFileName(name) + presetFileExtension;
            string fullPath = Path.Combine(presetSavePath, fileName);

#if UNITY_EDITOR
            var existingPreset = AssetDatabase.LoadAssetAtPath<BiomePreset>(fullPath);
            if (existingPreset != null)
            {
                return;
            }

            var preset = CreateInstance<BiomePreset>();
            preset.presetName = name;
            preset.description = description;
            preset.moisture = moisture;
            preset.temperature = temperature;
            preset.fertility = fertility;
            preset.rockiness = rockiness;
            preset.InitializeDefault();

            SavePreset(preset);
#else
            if (File.Exists(fullPath))
            {
                return;
            }
#endif
        }

        #endregion

        #region 設定適用

        private void ApplyEnvironmentSettings(EnvironmentSettings settings)
        {
            try
            {
                var sun = FindObjectOfType<Light>();
                if (sun != null && sun.type == LightType.Directional)
                {
                    sun.color = settings.sunColor;
                    sun.intensity = settings.sunIntensity;
                    sun.transform.rotation = Quaternion.Euler(settings.sunRotation, 30f, 0f);
                }

                RenderSettings.fog = settings.enableFog;
                if (settings.enableFog)
                {
                    RenderSettings.fogColor = settings.fogColor;
                    RenderSettings.fogStartDistance = settings.fogStartDistance;
                    RenderSettings.fogEndDistance = settings.fogEndDistance;
                }

                RenderSettings.ambientLight = settings.fogColor;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ApplyEnvironmentSettings failed: {e.Message}");
            }
        }

        private void ApplyMaterialSettings(MaterialSettings settings, TerrainTile tile)
        {
            if (tile?.terrainObject == null) return;

            try
            {
                var renderer = tile.terrainObject.GetComponent<MeshRenderer>();
                if (renderer != null && settings.terrainMaterial != null)
                {
                    renderer.material = settings.terrainMaterial;

                    if (renderer.material.HasProperty("_Color"))
                    {
                        renderer.material.color = settings.terrainTint;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ApplyMaterialSettings failed: {e.Message}");
            }
        }

        #endregion

        #region ユーティリティ

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "Unnamed";

            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = fileName;

            foreach (char c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }

            return sanitized;
        }

#if UNITY_EDITOR
        private void CreateFolderRecursive(string path)
        {
            var segments = path.Split('/');
            string currentPath = segments[0];

            if (!AssetDatabase.IsValidFolder(currentPath))
            {
                AssetDatabase.CreateFolder("", currentPath);
            }

            for (int i = 1; i < segments.Length; i++)
            {
                string parent = currentPath;
                currentPath = Path.Combine(currentPath, segments[i]).Replace("\\", "/");
                if (!AssetDatabase.IsValidFolder(currentPath))
                {
                    AssetDatabase.CreateFolder(parent, segments[i]);
                }
            }
        }
#endif

        #endregion
    }
}
=======
>>>>>>> 386c3b806d99895c652c4a4763bab04a3d0867da
