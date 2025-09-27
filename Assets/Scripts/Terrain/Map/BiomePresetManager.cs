using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Vastcore.Generation
{
    /// <summary>
    /// バイオームプリセット管理システム
    /// プリセットの保存・読み込み・管理を行う
    /// </summary>
    public class BiomePresetManager : MonoBehaviour
    {
        // シングルトンインスタンス
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

        [Header("保存設定")]
        [SerializeField] private string presetSavePath = "Assets/Data/BiomePresets";
        [SerializeField] private string presetFileExtension = ".biome";
        
        // 利用可能なプリセットのキャッシュ
        private List<BiomePreset> availablePresets = new List<BiomePreset>();
        
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
            if (_instance == this)
            {
                Initialize();
            }
        }
        
        /// <summary>
        /// プリセットマネージャーを初期化
        /// </summary>
        public void Initialize()
        {
            // 保存ディレクトリの確認
            EnsureDirectoryExists();
            
            // 利用可能なプリセットを読み込み
            RefreshAvailablePresets();
            
            // プリセットが存在しない場合はデフォルトを作成
            if (availablePresets.Count == 0)
            {
                CreateDefaultPresets();
            }
        }
        
        /// <summary>
        /// 保存ディレクトリが存在することを確認
        /// </summary>
        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(presetSavePath))
            {
                Directory.CreateDirectory(presetSavePath);
            }
        }
        
        /// <summary>
        /// プリセットを保存
        /// </summary>
        public bool SavePreset(BiomePreset preset)
        {
            if (preset == null) return false;
            try
            {
                string fileName = SanitizeFileName(preset.presetName) + presetFileExtension;
                string fullPath = Path.Combine(presetSavePath, fileName);
                
                // 既に存在する場合は上書き
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
                
                // プリセットを保存
                using (FileStream stream = File.Create(fullPath))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, preset);
                }
                
                // キャッシュを更新
                RefreshAvailablePresets();
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"プリセットの保存に失敗しました: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// プリセットを読み込み
        /// </summary>
        public BiomePreset LoadPreset(string presetName)
        {
            if (string.IsNullOrEmpty(presetName)) return null;
            
            string fileName = SanitizeFileName(presetName) + presetFileExtension;
            string fullPath = Path.Combine(presetSavePath, fileName);
            
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"プリセットが見つかりません: {fullPath}");
                return null;
            }
            
            try
            {
                using (FileStream stream = File.OpenRead(fullPath))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    return (BiomePreset)formatter.Deserialize(stream);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"プリセットの読み込みに失敗しました: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// プリセットを削除
        /// </summary>
        public bool DeletePreset(string presetName)
        {
            if (string.IsNullOrEmpty(presetName)) return false;
            
            string fileName = SanitizeFileName(presetName) + presetFileExtension;
            string fullPath = Path.Combine(presetSavePath, fileName);
            
            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                    RefreshAvailablePresets();
                    return true;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"プリセットの削除に失敗しました: {e.Message}");
                    return false;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 利用可能なプリセット一覧を更新
        /// </summary>
        public void RefreshAvailablePresets()
        {
            availablePresets.Clear();
            
            if (!Directory.Exists(presetSavePath))
            {
                Directory.CreateDirectory(presetSavePath);
                return;
            }
            
            string[] presetFiles = Directory.GetFiles(presetSavePath, "*" + presetFileExtension);
            
            foreach (string filePath in presetFiles)
            {
                try
                {
                    using (FileStream stream = File.OpenRead(filePath))
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        var preset = (BiomePreset)formatter.Deserialize(stream);
                        availablePresets.Add(preset);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"プリセットの読み込みに失敗しました ({filePath}): {e.Message}");
                }
            }
        }
        
        /// <summary>
        /// 利用可能なプリセット名一覧を取得
        /// </summary>
        public List<string> GetAvailablePresetNames()
        {
            return availablePresets.Where(p => p != null).Select(p => p.presetName).ToList();
        }
        
        /// <summary>
        /// プリセットが存在するかチェック
        /// </summary>
        public bool PresetExists(string presetName)
        {
            if (string.IsNullOrEmpty(presetName)) return false;
            return availablePresets.Any(p => p != null && p.presetName == presetName);
        }
        
        /// <summary>
        /// プリセットを地形に適用
        /// </summary>
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
        
        /// <summary>
        /// プリセットを地形に適用
        /// </summary>
        public void ApplyPresetToTerrain(BiomePreset preset, TerrainTile targetTile)
        {
            if (preset == null || targetTile == null)
            {
                Debug.LogError("ApplyPresetToTerrain: プリセットまたはタイルがnullです");
                return;
            }
            
            try
            {
                // タイルに適用バイオームを設定
                targetTile.appliedBiome = preset;
                
                // 環境設定の適用
                ApplyEnvironmentSettings(preset.environmentSettings);
                
                // 材質設定の適用
                ApplyMaterialSettings(preset.materialSettings, targetTile);
                
                Debug.Log($"BiomePreset applied to terrain: {preset.presetName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ApplyPresetToTerrain failed: {e.Message}");
            }
        }
        
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
            if (File.Exists(fullPath)) return;
            
            var preset = ScriptableObject.CreateInstance<BiomePreset>();
            preset.presetName = name;
            preset.description = description;
            preset.moisture = moisture;
            preset.temperature = temperature;
            preset.fertility = fertility;
            preset.rockiness = rockiness;
            
            var terrainParams = MeshGenerator.TerrainGenerationParams.Default();
            switch (name.ToLower())
            {
                case "mountains":
                    terrainParams.maxHeight = 400f;
                    terrainParams.noiseScale = 0.003f;
                    terrainParams.octaves = 10;
                    break;
                case "desert":
                    terrainParams.maxHeight = 100f;
                    terrainParams.enableTerracing = false;
                    terrainParams.enableErosion = true;
                    terrainParams.erosionStrength = 0.5f;
                    break;
                case "plains":
                    terrainParams.maxHeight = 50f;
                    terrainParams.noiseScale = 0.01f;
                    terrainParams.octaves = 4;
                    break;
            }
            preset.terrainParams = terrainParams;
            preset.InitializeDefault();
            
            SavePreset(preset);
        }
        #endregion
        
        #region 設定適用
        private void ApplyEnvironmentSettings(EnvironmentSettings settings)
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
        }
        
        private void ApplyMaterialSettings(MaterialSettings settings, TerrainTile tile)
        {
            if (tile?.terrainObject == null) return;
            var renderer = tile.terrainObject.GetComponent<MeshRenderer>();
            if (renderer != null && settings.terrainMaterial != null)
            {
                renderer.material = settings.terrainMaterial;
                if (renderer.material.HasProperty("_Color"))
                {
                    renderer.material.color = settings.terrainTint;
                }
            }
            // 環境のアンビエントライトは MaterialSettings に定義されている
            RenderSettings.ambientLight = settings.ambientColor;
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
        #endregion
    }
}
