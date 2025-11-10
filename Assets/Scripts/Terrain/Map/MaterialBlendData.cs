using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Generation
{
    /// <summary>
    /// マテリアルブレンドデータ
    /// 動的ブレンディングシステムで使用される各タイルのブレンド状態を管理
    /// </summary>
    [System.Serializable]
    public class MaterialBlendData
    {
        #region 基本データ
        [Header("タイル情報")]
        public TerrainTile associatedTile;
        public Vector2Int tileCoordinate;
        public float creationTime;
        public float lastUpdateTime;
        
        [Header("アクティブな遷移")]
        public Dictionary<string, BlendTransition> activeTransitions = new Dictionary<string, BlendTransition>();
        public Dictionary<string, ColorBlendTransition> activeColorTransitions = new Dictionary<string, ColorBlendTransition>();
        #endregion

        #region LOD関連
        [Header("LOD状態")]
        public int currentLODLevel = 0;
        public int targetLODLevel = 0;
        public float currentLODScale = 1f;
        public float targetLODScale = 1f;
        public bool isLODTransitioning = false;
        #endregion

        #region 環境関連
        [Header("環境状態")]
        public EnvironmentalConditions currentEnvironmentalConditions = new EnvironmentalConditions();
        public Color currentTemperatureColor = Color.white;
        public float currentMoistureSaturation = 1f;
        public float currentTimeBrightness = 1f;
        public float currentWindEffect = 0f;
        public float currentPrecipitationEffect = 0f;
        #endregion

        #region 季節関連
        [Header("季節状態")]
        public Season currentSeason = Season.Spring;
        public Season targetSeason = Season.Spring;
        public Color currentSeasonalColor = Color.white;
        public float currentSeasonalBrightness = 1f;
        public float currentSeasonalSaturation = 1f;
        public bool isSeasonTransitioning = false;
        public float seasonTransitionProgress = 0f;
        #endregion

        #region バイオーム関連
        [Header("バイオーム状態")]
        public BiomePreset currentBiomePreset;
        public BiomePreset targetBiomePreset;
        public Color currentBiomeColor = Color.white;
        public Color currentBiomeAmbient = Color.gray;
        public bool isBiomeTransitioning = false;
        public float biomeTransitionProgress = 0f;
        #endregion

        #region テクスチャ関連
        [Header("テクスチャ状態")]
        public TerrainTextureType[] currentTextureTypes = new TerrainTextureType[4];
        public float[] currentTextureWeights = new float[4];
        public TerrainTextureType[] targetTextureTypes = new TerrainTextureType[4];
        public float[] targetTextureWeights = new float[4];
        public bool isTextureTransitioning = false;
        public float textureTransitionProgress = 0f;
        #endregion

        #region パフォーマンス統計
        [Header("統計情報")]
        public int totalTransitions = 0;
        public int completedTransitions = 0;
        public float averageTransitionTime = 0f;
        public float totalBlendTime = 0f;
        public int framesSinceLastUpdate = 0;
        #endregion

        #region コンストラクタ
        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public MaterialBlendData()
        {
            Initialize();
        }
        
        /// <summary>
        /// タイル指定コンストラクタ
        /// </summary>
        public MaterialBlendData(TerrainTile tile)
        {
            associatedTile = tile;
            if (tile != null)
            {
                tileCoordinate = tile.coordinate;
            }
            Initialize();
        }
        
        /// <summary>
        /// 初期化
        /// </summary>
        private void Initialize()
        {
            creationTime = Time.time;
            lastUpdateTime = creationTime;
            
            // デフォルト値を設定
            currentLODLevel = 0;
            targetLODLevel = 0;
            currentLODScale = 1f;
            targetLODScale = 1f;
            
            currentTemperatureColor = Color.white;
            currentMoistureSaturation = 1f;
            currentTimeBrightness = 1f;
            
            currentSeason = Season.Spring;
            targetSeason = Season.Spring;
            currentSeasonalColor = Color.white;
            currentSeasonalBrightness = 1f;
            currentSeasonalSaturation = 1f;
            
            currentBiomeColor = Color.white;
            currentBiomeAmbient = Color.gray;
            
            // テクスチャ配列を初期化
            for (int i = 0; i < currentTextureTypes.Length; i++)
            {
                currentTextureTypes[i] = TerrainTextureType.Grass;
                currentTextureWeights[i] = i == 0 ? 1f : 0f;
                targetTextureTypes[i] = TerrainTextureType.Grass;
                targetTextureWeights[i] = i == 0 ? 1f : 0f;
            }
        }
        #endregion

        #region 遷移管理
        /// <summary>
        /// 新しい遷移を追加
        /// </summary>
        public void AddTransition(string propertyName, float fromValue, float toValue, float speed)
        {
            var transition = new BlendTransition
            {
                propertyName = propertyName,
                fromValue = fromValue,
                toValue = toValue,
                currentValue = fromValue,
                speed = speed,
                startTime = Time.time,
                isComplete = false
            };
            
            activeTransitions[propertyName] = transition;
            totalTransitions++;
        }
        
        /// <summary>
        /// 新しい色遷移を追加
        /// </summary>
        public void AddColorTransition(string propertyName, Color fromColor, Color toColor, float speed)
        {
            var transition = new ColorBlendTransition
            {
                propertyName = propertyName,
                fromColor = fromColor,
                toColor = toColor,
                currentColor = fromColor,
                speed = speed,
                startTime = Time.time,
                isComplete = false
            };
            
            activeColorTransitions[propertyName] = transition;
            totalTransitions++;
        }
        
        /// <summary>
        /// 遷移を更新
        /// </summary>
        public void UpdateTransitions(float deltaTime)
        {
            lastUpdateTime = Time.time;
            framesSinceLastUpdate = 0;
            
            // Float遷移を更新
            foreach (var kvp in activeTransitions.ToArray())
            {
                var transition = kvp.Value;
                if (!transition.isComplete)
                {
                    UpdateFloatTransition(ref transition);
                    activeTransitions[kvp.Key] = transition;
                    
                    if (transition.isComplete)
                    {
                        completedTransitions++;
                        OnTransitionCompleted(transition.propertyName);
                    }
                }
            }
            
            // Color遷移を更新
            foreach (var kvp in activeColorTransitions.ToArray())
            {
                var transition = kvp.Value;
                if (!transition.isComplete)
                {
                    UpdateColorTransition(ref transition);
                    activeColorTransitions[kvp.Key] = transition;
                    
                    if (transition.isComplete)
                    {
                        completedTransitions++;
                        OnTransitionCompleted(transition.propertyName);
                    }
                }
            }
            
            // 統計を更新
            UpdateStatistics();
        }
        
        /// <summary>
        /// Float遷移を更新
        /// </summary>
        private void UpdateFloatTransition(ref BlendTransition transition)
        {
            float elapsed = Time.time - transition.startTime;
            float progress = elapsed * transition.speed;
            
            if (progress >= 1f)
            {
                transition.currentValue = transition.toValue;
                transition.isComplete = true;
            }
            else
            {
                transition.currentValue = Mathf.Lerp(transition.fromValue, transition.toValue, progress);
            }
        }
        
        /// <summary>
        /// Color遷移を更新
        /// </summary>
        private void UpdateColorTransition(ref ColorBlendTransition transition)
        {
            float elapsed = Time.time - transition.startTime;
            float progress = elapsed * transition.speed;
            
            if (progress >= 1f)
            {
                transition.currentColor = transition.toColor;
                transition.isComplete = true;
            }
            else
            {
                transition.currentColor = Color.Lerp(transition.fromColor, transition.toColor, progress);
            }
        }
        
        /// <summary>
        /// 遷移完了時の処理
        /// </summary>
        private void OnTransitionCompleted(string propertyName)
        {
            // 特定のプロパティに応じた後処理
            switch (propertyName)
            {
                case "LOD_Scale":
                    isLODTransitioning = false;
                    currentLODLevel = targetLODLevel;
                    break;
                    
                case "Seasonal_Color":
                case "Seasonal_Brightness":
                case "Seasonal_Saturation":
                    CheckSeasonTransitionComplete();
                    break;
                    
                case "Biome_Color":
                case "Biome_Ambient":
                    CheckBiomeTransitionComplete();
                    break;
            }
        }
        
        /// <summary>
        /// 季節遷移の完了をチェック
        /// </summary>
        private void CheckSeasonTransitionComplete()
        {
            bool allSeasonalTransitionsComplete = 
                (!activeColorTransitions.ContainsKey("Seasonal_Color") || activeColorTransitions["Seasonal_Color"].isComplete) &&
                (!activeTransitions.ContainsKey("Seasonal_Brightness") || activeTransitions["Seasonal_Brightness"].isComplete) &&
                (!activeTransitions.ContainsKey("Seasonal_Saturation") || activeTransitions["Seasonal_Saturation"].isComplete);
            
            if (allSeasonalTransitionsComplete)
            {
                isSeasonTransitioning = false;
                currentSeason = targetSeason;
                seasonTransitionProgress = 1f;
            }
        }
        
        /// <summary>
        /// バイオーム遷移の完了をチェック
        /// </summary>
        private void CheckBiomeTransitionComplete()
        {
            bool allBiomeTransitionsComplete = 
                (!activeColorTransitions.ContainsKey("Biome_Color") || activeColorTransitions["Biome_Color"].isComplete) &&
                (!activeColorTransitions.ContainsKey("Biome_Ambient") || activeColorTransitions["Biome_Ambient"].isComplete);
            
            if (allBiomeTransitionsComplete)
            {
                isBiomeTransitioning = false;
                currentBiomePreset = targetBiomePreset;
                biomeTransitionProgress = 1f;
            }
        }
        #endregion

        #region 状態管理
        /// <summary>
        /// アクティブな遷移があるかチェック
        /// </summary>
        public bool HasActiveTransitions()
        {
            return activeTransitions.Count > 0 || activeColorTransitions.Count > 0;
        }
        
        /// <summary>
        /// 特定のプロパティが遷移中かチェック
        /// </summary>
        public bool IsPropertyTransitioning(string propertyName)
        {
            return (activeTransitions.ContainsKey(propertyName) && !activeTransitions[propertyName].isComplete) ||
                   (activeColorTransitions.ContainsKey(propertyName) && !activeColorTransitions[propertyName].isComplete);
        }
        
        /// <summary>
        /// 遷移の進行状況を取得
        /// </summary>
        public float GetTransitionProgress(string propertyName)
        {
            if (activeTransitions.ContainsKey(propertyName))
            {
                var transition = activeTransitions[propertyName];
                float elapsed = Time.time - transition.startTime;
                return Mathf.Clamp01(elapsed * transition.speed);
            }
            
            if (activeColorTransitions.ContainsKey(propertyName))
            {
                var transition = activeColorTransitions[propertyName];
                float elapsed = Time.time - transition.startTime;
                return Mathf.Clamp01(elapsed * transition.speed);
            }
            
            return 1f; // 遷移していない場合は完了とみなす
        }
        
        /// <summary>
        /// 完了した遷移をクリーンアップ
        /// </summary>
        public void CleanupCompletedTransitions()
        {
            var completedFloatTransitions = new List<string>();
            foreach (var kvp in activeTransitions)
            {
                if (kvp.Value.isComplete)
                {
                    completedFloatTransitions.Add(kvp.Key);
                }
            }
            
            foreach (var key in completedFloatTransitions)
            {
                activeTransitions.Remove(key);
            }
            
            var completedColorTransitions = new List<string>();
            foreach (var kvp in activeColorTransitions)
            {
                if (kvp.Value.isComplete)
                {
                    completedColorTransitions.Add(kvp.Key);
                }
            }
            
            foreach (var key in completedColorTransitions)
            {
                activeColorTransitions.Remove(key);
            }
        }
        #endregion

        #region 統計管理
        /// <summary>
        /// 統計を更新
        /// </summary>
        private void UpdateStatistics()
        {
            framesSinceLastUpdate++;
            
            if (totalTransitions > 0)
            {
                float completionRate = (float)completedTransitions / totalTransitions;
                averageTransitionTime = (Time.time - creationTime) * completionRate;
            }
            
            totalBlendTime = Time.time - creationTime;
        }
        
        /// <summary>
        /// 統計情報を取得
        /// </summary>
        public MaterialBlendStatistics GetStatistics()
        {
            return new MaterialBlendStatistics
            {
                totalTransitions = totalTransitions,
                completedTransitions = completedTransitions,
                activeTransitions = activeTransitions.Count + activeColorTransitions.Count,
                averageTransitionTime = averageTransitionTime,
                totalBlendTime = totalBlendTime,
                completionRate = totalTransitions > 0 ? (float)completedTransitions / totalTransitions : 1f,
                framesSinceLastUpdate = framesSinceLastUpdate
            };
        }
        
        /// <summary>
        /// デバッグ情報を取得
        /// </summary>
        public string GetDebugInfo()
        {
            return $"BlendData[{tileCoordinate}]: Active={activeTransitions.Count + activeColorTransitions.Count}, " +
                   $"Completed={completedTransitions}/{totalTransitions}, " +
                   $"LOD={currentLODLevel}, Season={currentSeason}, " +
                   $"Biome={currentBiomePreset?.presetName ?? "None"}";
        }
        #endregion

        #region クリーンアップ
        /// <summary>
        /// すべてのデータをクリーンアップ
        /// </summary>
        public void Cleanup()
        {
            activeTransitions.Clear();
            activeColorTransitions.Clear();
            
            associatedTile = null;
            currentBiomePreset = null;
            targetBiomePreset = null;
        }
        #endregion
    }
    
    /// <summary>
    /// ブレンド遷移データ（Float値用）
    /// </summary>
    [System.Serializable]
    public struct BlendTransition
    {
        public string propertyName;
        public float fromValue;
        public float toValue;
        public float currentValue;
        public float speed;
        public float startTime;
        public bool isComplete;
        
        /// <summary>
        /// 遷移の進行状況を取得
        /// </summary>
        public float GetProgress()
        {
            if (isComplete) return 1f;
            
            float elapsed = Time.time - startTime;
            return Mathf.Clamp01(elapsed * speed);
        }
        
        /// <summary>
        /// 残り時間を取得
        /// </summary>
        public float GetRemainingTime()
        {
            if (isComplete) return 0f;
            
            float progress = GetProgress();
            return (1f - progress) / speed;
        }
    }
    
    /// <summary>
    /// ブレンド遷移データ（Color値用）
    /// </summary>
    [System.Serializable]
    public struct ColorBlendTransition
    {
        public string propertyName;
        public Color fromColor;
        public Color toColor;
        public Color currentColor;
        public float speed;
        public float startTime;
        public bool isComplete;
        
        /// <summary>
        /// 遷移の進行状況を取得
        /// </summary>
        public float GetProgress()
        {
            if (isComplete) return 1f;
            
            float elapsed = Time.time - startTime;
            return Mathf.Clamp01(elapsed * speed);
        }
        
        /// <summary>
        /// 残り時間を取得
        /// </summary>
        public float GetRemainingTime()
        {
            if (isComplete) return 0f;
            
            float progress = GetProgress();
            return (1f - progress) / speed;
        }
    }
    
    /// <summary>
    /// マテリアルブレンドリクエスト
    /// </summary>
    [System.Serializable]
    public class MaterialBlendRequest
    {
        public TerrainTile tile;
        public MaterialBlendType blendType;
        public object blendData;
        public int priority;
        public float requestTime;
        public bool isUrgent;
        
        /// <summary>
        /// リクエストの年齢を取得
        /// </summary>
        public float GetAge()
        {
            return Time.time - requestTime;
        }
    }
    
    /// <summary>
    /// マテリアルブレンド統計
    /// </summary>
    [System.Serializable]
    public struct MaterialBlendStatistics
    {
        public int totalTransitions;
        public int completedTransitions;
        public int activeTransitions;
        public float averageTransitionTime;
        public float totalBlendTime;
        public float completionRate;
        public int framesSinceLastUpdate;
    }
    
    /// <summary>
    /// マテリアルブレンドタイプ
    /// </summary>
    public enum MaterialBlendType
    {
        DistanceLOD,        // 距離ベースLOD
        Environmental,      // 環境変化
        Seasonal,          // 季節変化
        Biome,             // バイオーム変化
        Texture,           // テクスチャ変化
        Lighting,          // 照明変化
        Weather            // 天候変化
    }
}