using System.Collections.Generic;
using UnityEngine;
using Vastcore.Utils;
using System.Reflection;

#if DEFORM_AVAILABLE
using Deform;
#endif

namespace Vastcore.Core
{
    /// <summary>
    /// Deform変形プリセットライブラリ
    /// 地質学的、建築的、有機的な変形パターンを管理
    /// </summary>
    [CreateAssetMenu(fileName = "DeformPresetLibrary", menuName = "Vastcore/Deform Preset Library")]
    public class DeformPresetLibrary : ScriptableObject
    {
        [Header("地質学的変形プリセット")]
        [SerializeField] private List<GeologicalPreset> geologicalPresets = new List<GeologicalPreset>();
        
        [Header("建築的変形プリセット")]
        [SerializeField] private List<ArchitecturalPreset> architecturalPresets = new List<ArchitecturalPreset>();
        
        [Header("有機的変形プリセット")]
        [SerializeField] private List<OrganicPreset> organicPresets = new List<OrganicPreset>();
        
        /// <summary>
        /// 地質学的変形プリセット
        /// </summary>
        [System.Serializable]
        public class GeologicalPreset
        {
            [Header("基本設定")]
            public string presetName;
            public GeologicalDeformType deformType;
            public float intensity = 1f;
            public bool enabled = true;
            
            [Header("ノイズ設定")]
            public float noiseScale = 1f;
            public int octaves = 3;
            public float persistence = 0.5f;
            
            [Header("変形パラメータ")]
            public Vector3 deformDirection = Vector3.up;
            public AnimationCurve intensityCurve = AnimationCurve.Linear(0, 0, 1, 1);
            public float frequency = 1f;
            
            public bool useMask = false;
            public MaskType maskType = MaskType.Sphere;
            public Vector3 maskCenter = Vector3.zero;
            public float maskRadius = 1f;
        }
        
        /// <summary>
        /// 建築的変形プリセット
        /// </summary>
        [System.Serializable]
        public class ArchitecturalPreset
        {
            [Header("基本設定")]
            public string presetName;
            public ArchitecturalDeformType deformType;
            public float intensity = 1f;
            public bool enabled = true;
            
            [Header("経年変化")]
            public float ageIntensity = 0.5f;
            public float weatheringFactor = 0.3f;
            public bool enableCracking = true;
            
            [Header("構造変形")]
            public Vector3 structuralStress = Vector3.zero;
            public float settlementAmount = 0f;
            public bool enableSagging = false;
            
            [Header("装飾変形")]
            public bool enableOrnamentation = false;
            public float ornamentComplexity = 0.5f;
            public AnimationCurve ornamentProfile = AnimationCurve.Linear(0, 0, 1, 1);
        }
        
        /// <summary>
        /// 有機的変形プリセット
        /// </summary>
        [System.Serializable]
        public class OrganicPreset
        {
            [Header("基本設定")]
            public string presetName;
            public OrganicDeformType deformType;
            public float intensity = 1f;
            public bool enabled = true;
            
            [Header("成長パターン")]
            public Vector3 growthDirection = Vector3.up;
            public float growthRate = 1f;
            public AnimationCurve growthCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            
            [Header("流体効果")]
            public Vector3 flowDirection = Vector3.right;
            public float flowStrength = 1f;
            public float turbulence = 0.5f;
            
            [Header("季節変化")]
            public bool enableSeasonalChange = false;
            public float seasonalAmplitude = 0.2f;
            public float seasonalFrequency = 1f;
        }
        
        /// <summary>
        /// 地質学的変形タイプ
        /// </summary>
        public enum GeologicalDeformType
        {
            Erosion,           // 侵食
            Weathering,        // 風化
            TectonicStress,    // 地殻変動
            SedimentLayers,    // 堆積層
            CrystalGrowth,     // 結晶成長
            VolcanicFlow,      // 火山流
            Folding,           // 褶曲
            Faulting          // 断層
        }
        
        /// <summary>
        /// 建築的変形タイプ
        /// </summary>
        public enum ArchitecturalDeformType
        {
            AgeDeterioration,  // 経年劣化
            StructuralStress,  // 構造応力
            WeatherDamage,     // 気象損傷
            OrganicGrowth,     // 植物成長
            FoundationSettle,  // 基礎沈下
            ArtisticCurve,     // 芸術的曲線
            Ornamentation,     // 装飾
            Restoration       // 修復
        }
        
        /// <summary>
        /// 有機的変形タイプ
        /// </summary>
        public enum OrganicDeformType
        {
            NaturalGrowth,     // 自然成長
            FlowingWater,      // 水流
            WindCarving,       // 風彫
            RootPenetration,   // 根浸透
            AnimalWear,        // 動物摩耗
            SeasonalChange,    // 季節変化
            BiologicalErosion, // 生物侵食
            Symbiosis         // 共生
        }
        
        /// <summary>
        /// マスクタイプ
        /// </summary>
        public enum MaskType
        {
            Sphere,
            Box,
            VerticalGradient,
            VertexColor
        }
        
        /// <summary>
        /// 初期化時にデフォルトプリセットを作成
        /// </summary>
        private void OnEnable()
        {
            if (geologicalPresets.Count == 0)
            {
                CreateDefaultGeologicalPresets();
            }
            
            if (architecturalPresets.Count == 0)
            {
                CreateDefaultArchitecturalPresets();
            }
            
            if (organicPresets.Count == 0)
            {
                CreateDefaultOrganicPresets();
            }
        }
        
        /// <summary>
        /// デフォルト地質学的プリセットを作成
        /// </summary>
        private void CreateDefaultGeologicalPresets()
        {
            geologicalPresets.Add(new GeologicalPreset
            {
                presetName = "風化侵食",
                deformType = GeologicalDeformType.Erosion,
                intensity = 0.3f,
                noiseScale = 2f,
                octaves = 4,
                persistence = 0.6f,
                frequency = 1.5f
            });
            
            geologicalPresets.Add(new GeologicalPreset
            {
                presetName = "結晶成長",
                deformType = GeologicalDeformType.CrystalGrowth,
                intensity = 0.8f,
                noiseScale = 0.5f,
                octaves = 2,
                persistence = 0.3f,
                deformDirection = Vector3.up
            });
            
            geologicalPresets.Add(new GeologicalPreset
            {
                presetName = "地殻変動",
                deformType = GeologicalDeformType.TectonicStress,
                intensity = 1.2f,
                noiseScale = 5f,
                octaves = 3,
                persistence = 0.7f,
                deformDirection = new Vector3(0.3f, 1f, 0.1f)
            });
        }
        
        /// <summary>
        /// デフォルト建築的プリセットを作成
        /// </summary>
        private void CreateDefaultArchitecturalPresets()
        {
            architecturalPresets.Add(new ArchitecturalPreset
            {
                presetName = "古代遺跡",
                deformType = ArchitecturalDeformType.AgeDeterioration,
                intensity = 0.7f,
                ageIntensity = 0.8f,
                weatheringFactor = 0.6f,
                enableCracking = true
            });
            
            architecturalPresets.Add(new ArchitecturalPreset
            {
                presetName = "植物侵食",
                deformType = ArchitecturalDeformType.OrganicGrowth,
                intensity = 0.5f,
                ageIntensity = 0.4f,
                weatheringFactor = 0.3f,
                enableCracking = false
            });
            
            architecturalPresets.Add(new ArchitecturalPreset
            {
                presetName = "芸術的曲線",
                deformType = ArchitecturalDeformType.ArtisticCurve,
                intensity = 0.4f,
                enableOrnamentation = true,
                ornamentComplexity = 0.7f
            });
        }
        
        /// <summary>
        /// デフォルト有機的プリセットを作成
        /// </summary>
        private void CreateDefaultOrganicPresets()
        {
            organicPresets.Add(new OrganicPreset
            {
                presetName = "自然成長",
                deformType = OrganicDeformType.NaturalGrowth,
                intensity = 0.6f,
                growthDirection = Vector3.up,
                growthRate = 1.2f
            });
            
            organicPresets.Add(new OrganicPreset
            {
                presetName = "水流侵食",
                deformType = OrganicDeformType.FlowingWater,
                intensity = 0.8f,
                flowDirection = new Vector3(1f, -0.2f, 0.3f),
                flowStrength = 1.5f,
                turbulence = 0.7f
            });
            
            organicPresets.Add(new OrganicPreset
            {
                presetName = "風の彫刻",
                deformType = OrganicDeformType.WindCarving,
                intensity = 0.4f,
                flowDirection = Vector3.right,
                flowStrength = 0.8f,
                turbulence = 0.9f
            });
        }
        
        /// <summary>
        /// 地質学的プリセットを取得
        /// </summary>
        public GeologicalPreset GetGeologicalPreset(string presetName)
        {
            return geologicalPresets.Find(p => p.presetName == presetName);
        }
        
        /// <summary>
        /// 建築的プリセットを取得
        /// </summary>
        public ArchitecturalPreset GetArchitecturalPreset(string presetName)
        {
            return architecturalPresets.Find(p => p.presetName == presetName);
        }
        
        /// <summary>
        /// 有機的プリセットを取得
        /// </summary>
        public OrganicPreset GetOrganicPreset(string presetName)
        {
            return organicPresets.Find(p => p.presetName == presetName);
        }
        
        /// <summary>
        /// タイプ別地質学的プリセットを取得
        /// </summary>
        public List<GeologicalPreset> GetGeologicalPresetsByType(GeologicalDeformType type)
        {
            return geologicalPresets.FindAll(p => p.deformType == type && p.enabled);
        }
        
        /// <summary>
        /// タイプ別建築的プリセットを取得
        /// </summary>
        public List<ArchitecturalPreset> GetArchitecturalPresetsByType(ArchitecturalDeformType type)
        {
            return architecturalPresets.FindAll(p => p.deformType == type && p.enabled);
        }
        
        /// <summary>
        /// タイプ別有機的プリセットを取得
        /// </summary>
        public List<OrganicPreset> GetOrganicPresetsByType(OrganicDeformType type)
        {
            return organicPresets.FindAll(p => p.deformType == type && p.enabled);
        }
        
        /// <summary>
        /// プリセットをGameObjectに適用
        /// </summary>
        public void ApplyPresetToGameObject(GameObject target, string presetName, float intensityMultiplier = 1f)
        {
            var geologicalPreset = GetGeologicalPreset(presetName);
            if (geologicalPreset != null)
            {
                ApplyGeologicalPreset(target, geologicalPreset, intensityMultiplier);
                return;
            }
            
            var architecturalPreset = GetArchitecturalPreset(presetName);
            if (architecturalPreset != null)
            {
                ApplyArchitecturalPreset(target, architecturalPreset, intensityMultiplier);
                return;
            }
            
            var organicPreset = GetOrganicPreset(presetName);
            if (organicPreset != null)
            {
                ApplyOrganicPreset(target, organicPreset, intensityMultiplier);
                return;
            }
            
            VastcoreLogger.Instance.LogWarning("DeformPresetLibrary", $"Preset not found: {presetName}");
        }
        
        /// <summary>
        /// 地質学的プリセットを適用
        /// </summary>
        private void ApplyGeologicalPreset(GameObject target, GeologicalPreset preset, float intensityMultiplier)
        {
#if DEFORM_AVAILABLE
            // 安全なプロパティ設定用ヘルパー（Deform のバージョン差異を吸収）
            void TrySetProperty(object obj, string prop, object value)
            {
                if (obj == null || string.IsNullOrEmpty(prop)) return;
                var t = obj.GetType();
                var pi = t.GetProperty(prop, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi == null || !pi.CanWrite) return;
                var pt = pi.PropertyType;
                try
                {
                    if (pt == typeof(float))
                    {
                        float f = value is float fv ? fv : (value is Vector3 v3 ? v3.x : System.Convert.ToSingle(value));
                        pi.SetValue(obj, f);
                    }
                    else if (pt == typeof(Vector3))
                    {
                        Vector3 v = value is Vector3 v3 ? v3 : (value is float fv ? Vector3.one * fv : Vector3.one);
                        pi.SetValue(obj, v);
                    }
                    else
                    {
                        // そのまま代入を試みる
                        pi.SetValue(obj, value);
                    }
                }
                catch { /* バージョン差異で失敗しても無視 */ }
            }

            void SetAny(object obj, object value, params string[] candidates)
            {
                foreach (var name in candidates)
                {
                    var t = obj?.GetType();
                    var pi = t?.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (pi != null && pi.CanWrite)
                    {
                        TrySetProperty(obj, name, value);
                        return;
                    }
                }
            }

            switch (preset.deformType)
            {
                case GeologicalDeformType.Erosion:
                    var noiseDeformer = target.AddComponent<NoiseDeformer>();
                    SetAny(noiseDeformer, preset.intensity * intensityMultiplier, "Factor", "Strength", "Amplitude", "Intensity");
                    break;
                
                case GeologicalDeformType.CrystalGrowth:
                    var scaleDeformer = target.AddComponent<ScaleDeformer>();
                    var scaleVec = Vector3.one * (1f + preset.intensity * intensityMultiplier);
                    SetAny(scaleDeformer, scaleVec, "Scale", "Factor", "ScaleFactor");
                    break;
                
                case GeologicalDeformType.TectonicStress:
                    var bendDeformer = target.AddComponent<BendDeformer>();
                    SetAny(bendDeformer, preset.intensity * intensityMultiplier, "Factor", "Angle", "Strength", "Intensity");
                    break;
            }
#else
            VastcoreLogger.Instance.LogDebug("DeformPresetLibrary", $"Applied geological preset (dummy): {preset.deformType}");
#endif
        }
        
        /// <summary>
        /// 建築的プリセットを適用
        /// </summary>
        private void ApplyArchitecturalPreset(GameObject target, ArchitecturalPreset preset, float intensityMultiplier)
        {
#if DEFORM_AVAILABLE
            // ヘルパー（上と同一ロジック）
            void TrySetProperty(object obj, string prop, object value)
            {
                if (obj == null || string.IsNullOrEmpty(prop)) return;
                var t = obj.GetType();
                var pi = t.GetProperty(prop, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi == null || !pi.CanWrite) return;
                var pt = pi.PropertyType;
                try
                {
                    if (pt == typeof(float))
                    {
                        float f = value is float fv ? fv : (value is Vector3 v3 ? v3.x : System.Convert.ToSingle(value));
                        pi.SetValue(obj, f);
                    }
                    else if (pt == typeof(Vector3))
                    {
                        Vector3 v = value is Vector3 v3 ? v3 : (value is float fv ? Vector3.one * fv : Vector3.one);
                        pi.SetValue(obj, v);
                    }
                    else
                    {
                        pi.SetValue(obj, value);
                    }
                }
                catch { }
            }
            void SetAny(object obj, object value, params string[] candidates)
            {
                foreach (var name in candidates)
                {
                    var t = obj?.GetType();
                    var pi = t?.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (pi != null && pi.CanWrite)
                    {
                        TrySetProperty(obj, name, value);
                        return;
                    }
                }
            }
            switch (preset.deformType)
            {
                case ArchitecturalDeformType.AgeDeterioration:
                    var rippleDeformer = target.AddComponent<RippleDeformer>();
                    SetAny(rippleDeformer, preset.intensity * intensityMultiplier, "Factor", "Strength", "Amplitude", "Intensity");
                    break;
                
                case ArchitecturalDeformType.ArtisticCurve:
                    var twistDeformer = target.AddComponent<TwistDeformer>();
                    SetAny(twistDeformer, preset.intensity * intensityMultiplier * 30f, "Factor", "Angle", "Strength", "Intensity");
                    break;
            }
#else
            VastcoreLogger.Instance.LogDebug("DeformPresetLibrary", $"Applied architectural preset (dummy): {preset.deformType}");
#endif
        }
        
        /// <summary>
        /// 有機的プリセットを適用
        /// </summary>
        private void ApplyOrganicPreset(GameObject target, OrganicPreset preset, float intensityMultiplier)
        {
#if DEFORM_AVAILABLE
            // ヘルパー（上と同一ロジック）
            void TrySetProperty(object obj, string prop, object value)
            {
                if (obj == null || string.IsNullOrEmpty(prop)) return;
                var t = obj.GetType();
                var pi = t.GetProperty(prop, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi == null || !pi.CanWrite) return;
                var pt = pi.PropertyType;
                try
                {
                    if (pt == typeof(float))
                    {
                        float f = value is float fv ? fv : (value is Vector3 v3 ? v3.x : System.Convert.ToSingle(value));
                        pi.SetValue(obj, f);
                    }
                    else if (pt == typeof(Vector3))
                    {
                        Vector3 v = value is Vector3 v3 ? v3 : (value is float fv ? Vector3.one * fv : Vector3.one);
                        pi.SetValue(obj, v);
                    }
                    else
                    {
                        pi.SetValue(obj, value);
                    }
                }
                catch { }
            }
            void SetAny(object obj, object value, params string[] candidates)
            {
                foreach (var name in candidates)
                {
                    var t = obj?.GetType();
                    var pi = t?.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (pi != null && pi.CanWrite)
                    {
                        TrySetProperty(obj, name, value);
                        return;
                    }
                }
            }
            switch (preset.deformType)
            {
                case OrganicDeformType.NaturalGrowth:
                    var inflateDeformer = target.AddComponent<InflateDeformer>();
                    SetAny(inflateDeformer, preset.intensity * intensityMultiplier, "Factor", "Amount", "Strength", "Intensity");
                    break;
                
                case OrganicDeformType.FlowingWater:
                    var waveDeformer = target.AddComponent<RippleDeformer>();
                    SetAny(waveDeformer, preset.intensity * intensityMultiplier, "Factor", "Strength", "Amplitude", "Intensity");
                    SetAny(waveDeformer, preset.flowStrength, "Frequency", "Speed", "Rate");
                    break;
            }
#else
            VastcoreLogger.Instance.LogDebug("DeformPresetLibrary", $"Applied organic preset (dummy): {preset.deformType}");
#endif
    }
}
}
