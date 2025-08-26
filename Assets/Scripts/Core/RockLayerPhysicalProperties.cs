using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Vastcore.Generation.Map
{
    // 岩石の形成タイプ（外部公開）
    // NOTE: Use top-level RockFormationType defined in GeologicalFormationGenerator.cs
    // public enum RockFormationType { Sedimentary, Igneous, Metamorphic }

    // 風化・浸食の分類（外部公開）
    public enum WeatheringPattern { Chemical, Physical, Mixed }
    public enum ErosionPattern { Fluvial, Coastal, Glacial, Aeolian }

    // 断層の表現（外部公開）
    public enum FaultType { Normal, Reverse, StrikeSlip }

    [Serializable]
    public class FaultStructure
    {
        public FaultType faultType;
        public float displacement;   // m
        public float age;            // Ma
        public bool isActive;
    }

    [Serializable]
    public class StratigraphicSequence
    {
        public List<GeologicalLayer> layers = new List<GeologicalLayer>();
        public List<FaultStructure> faults = new List<FaultStructure>();
        public float dip;            // 層理面の傾斜角（度）
        public bool isOverturned;    // 転倒褶曲フラグ
    }

    // NOTE: Use top-level GeologicalLayer defined in GeologicalFormationGenerator.cs
    // [Serializable]
    // public class GeologicalLayer
    // {
    //     public string layerName;
    //     public RockFormationType formationType;
    //     public float thickness;
    //     public float hardness;
    //     public Color layerColor;
    //     public Vector3 deformation;
    //     public bool isFaulted;
    // }

    // NOTE: Use top-level GeologicalEnvironment defined in GeologicalFormationGenerator.cs
    // [Serializable]
    // public class GeologicalEnvironment
    // {
    //     public float temperature;       // 温度 (°C)
    //     public float waterDepth;        // 水深 (m)
    //     public float pressure;           // 圧力 (MPa)
    // }

    [DisallowMultipleComponent]
    public class RockLayerPhysicalProperties : MonoBehaviour
    {
        [Serializable]
        public struct RockProperties
        {
            public float baseHardness;  // モース換算の相対硬度（1-10 目安）
            public float density;       // g/cm^3 の目安
            public Color baseColor;
        }

        [Header("効果の有効化")]
        [SerializeField] private bool enableWeathering = true;
        [SerializeField] private bool enableErosion = true;

        [Header("強度パラメータ")]
        [SerializeField, Range(0f, 2f)] private float weatheringIntensity = 0.5f;
        [SerializeField, Range(0f, 2f)] private float erosionIntensity = 0.3f;

        private readonly Dictionary<RockFormationType, RockProperties> rockProperties = new();
        private System.Random rnd;

        // 初期化
        public void Initialize()
        {
            if (rnd == null) rnd = new System.Random();
            rockProperties.Clear();

            // ベース特性の設定（タイプごと）
            rockProperties[RockFormationType.Sedimentary] = new RockProperties
            {
                baseHardness = 4.0f,
                density = 2.3f,
                baseColor = new Color(0.78f, 0.68f, 0.50f, 1f)
            };
            rockProperties[RockFormationType.Igneous] = new RockProperties
            {
                baseHardness = 7.0f,
                density = 2.9f,
                baseColor = new Color(0.35f, 0.35f, 0.37f, 1f)
            };
            rockProperties[RockFormationType.Metamorphic] = new RockProperties
            {
                baseHardness = 6.5f,
                density = 2.8f,
                baseColor = new Color(0.52f, 0.44f, 0.62f, 1f)
            };
        }

        // 層に物理特性と経年変化を適用
        public void ApplyPhysicalProperties(GeologicalLayer layer, GeologicalEnvironment environmentalConditions, float age)
        {
            if (layer == null || environmentalConditions == null) return;

            var props = GetRockProperties(layer.formationType);

            // 基礎色に寄せる（視覚的ばらつきは残す）
            layer.layerColor = Color.Lerp(layer.layerColor, props.baseColor, 0.25f);

            // 環境に応じた硬度補正
            float envDelta = GetEnvironmentalHardnessDelta(layer.formationType, environmentalConditions);
            layer.hardness = Mathf.Clamp(props.baseHardness + envDelta, 1f, 10f);

            // 風化（硬度低下 + 色の退色）
            if (enableWeathering)
            {
                ApplyWeatheringEffects(layer, environmentalConditions, age);
            }

            // 浸食（厚さ減少）
            if (enableErosion)
            {
                ApplyErosionEffects(layer, environmentalConditions, age);
            }

            // 経年による微小な色変化（酸化・鉱物変質の雰囲気表現）
            ApplyColorVariation(layer, age);
        }

        // ベース特性の取得（存在しない場合はフォールバック）
        public RockProperties GetRockProperties(RockFormationType rockType)
        {
            if (rockProperties.TryGetValue(rockType, out var p)) return p;
            return new RockProperties { baseHardness = 5f, density = 2.6f, baseColor = Color.gray };
        }

        public WeatheringPattern GetWeatheringPattern(RockFormationType rockType)
        {
            return rockType switch
            {
                RockFormationType.Sedimentary => WeatheringPattern.Chemical,
                RockFormationType.Igneous => WeatheringPattern.Physical,
                RockFormationType.Metamorphic => WeatheringPattern.Mixed,
                _ => WeatheringPattern.Mixed
            };
        }

        public ErosionPattern GetErosionPattern(RockFormationType rockType)
        {
            return rockType switch
            {
                RockFormationType.Sedimentary => ErosionPattern.Fluvial,
                RockFormationType.Igneous => ErosionPattern.Glacial,
                RockFormationType.Metamorphic => ErosionPattern.Coastal,
                _ => ErosionPattern.Aeolian
            };
        }

        // 層序の生成（断層・傾斜の付与）
        public StratigraphicSequence GenerateStratigraphicSequence(List<GeologicalLayer> layers, float tectonicActivity)
        {
            var seq = new StratigraphicSequence();
            if (layers != null) seq.layers = new List<GeologicalLayer>(layers);

            float noise = (float)(rnd?.NextDouble() ?? UnityEngine.Random.value) * 10f - 5f;
            seq.dip = Mathf.Clamp(tectonicActivity * 60f + noise, 0f, 90f);
            seq.isOverturned = (seq.dip > 60f) && (tectonicActivity > 0.7f);

            // 断層の生成
            int faultCount = Mathf.Max(0, Mathf.RoundToInt(tectonicActivity * 3f));
            for (int i = 0; i < faultCount; i++)
            {
                seq.faults.Add(GenerateFault(tectonicActivity));
            }

            return seq;
        }

        // 内部計算: 環境による硬度補正（オプショナル項目は反射で安全取得）
        private float GetEnvironmentalHardnessDelta(RockFormationType type, GeologicalEnvironment env)
        {
            float delta = 0f;
            // 圧力・温度は一般に硬化（結晶化・焼結）
            delta += Mathf.Clamp01((env.pressure - 1f) * 0.5f);
            delta += Mathf.Clamp01((env.temperature - 15f) / 100f);

            // 水中環境の堆積は固結を促進（ただし堆積岩のみ）
            if (type == RockFormationType.Sedimentary && env.waterDepth > 10f)
                delta += 0.5f;

            // マグマ活動・変成度は存在時のみ使用
            float magma = GetOptionalFloat(env, "magmaActivity", 0f);
            float meta = GetOptionalFloat(env, "metamorphicGrade", 0f);

            if (type == RockFormationType.Igneous)
                delta += magma * 0.8f;
            if (type == RockFormationType.Metamorphic)
                delta += meta * 1.0f;

            return delta;
        }

        private static float GetOptionalFloat(object obj, string fieldName, float defaultValue = 0f)
        {
            if (obj == null) return defaultValue;
            var t = obj.GetType();
            var f = t.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (f != null && f.FieldType == typeof(float))
            {
                object val = f.GetValue(obj);
                if (val is float fv) return fv;
            }
            return defaultValue;
        }

        // 風化効果
        private void ApplyWeatheringEffects(GeologicalLayer layer, GeologicalEnvironment env, float age)
        {
            var pattern = GetWeatheringPattern(layer.formationType);
            float ageFactor = Mathf.Clamp01(age / 200f); // 0-200Ma を 0-1 に正規化
            float climateFactor = Mathf.Clamp01((env.temperature - 5f) / 25f) + Mathf.Clamp01(env.waterDepth / 200f);

            float intensity = weatheringIntensity * (0.5f + climateFactor) * (0.25f + ageFactor);

            switch (pattern)
            {
                case WeatheringPattern.Chemical:
                    // 化学風化: 硬度低下 + 色の黄色化
                    layer.hardness = Mathf.Max(1f, layer.hardness - 0.8f * intensity);
                    layer.layerColor = Color.Lerp(layer.layerColor, new Color(0.85f, 0.75f, 0.55f, 1f), 0.3f * intensity);
                    break;
                case WeatheringPattern.Physical:
                    // 物理風化: 硬度はわずか低下、彩度低下
                    layer.hardness = Mathf.Max(1f, layer.hardness - 0.4f * intensity);
                    layer.layerColor = Color.Lerp(layer.layerColor, Desaturate(layer.layerColor), 0.4f * intensity);
                    break;
                case WeatheringPattern.Mixed:
                    layer.hardness = Mathf.Max(1f, layer.hardness - 0.6f * intensity);
                    layer.layerColor = Color.Lerp(layer.layerColor, Desaturate(layer.layerColor), 0.25f * intensity);
                    break;
            }
        }

        // 浸食効果
        private void ApplyErosionEffects(GeologicalLayer layer, GeologicalEnvironment env, float age)
        {
            var pattern = GetErosionPattern(layer.formationType);
            float reliefFactor = 1f; // 地形起伏の代理（簡略）
            float waterFactor = Mathf.Clamp01(env.waterDepth / 50f);
            float intensity = erosionIntensity * (0.5f + reliefFactor) * (0.2f + waterFactor);

            float thicknessLoss = 0f;
            switch (pattern)
            {
                case ErosionPattern.Fluvial:
                    thicknessLoss = 0.5f * intensity;
                    break;
                case ErosionPattern.Coastal:
                    thicknessLoss = 0.6f * intensity;
                    break;
                case ErosionPattern.Glacial:
                    thicknessLoss = 0.8f * intensity;
                    break;
                case ErosionPattern.Aeolian:
                    thicknessLoss = 0.3f * intensity;
                    break;
            }

            layer.thickness = Mathf.Max(0.1f, layer.thickness - thicknessLoss);
        }

        // 経年色変化
        private void ApplyColorVariation(GeologicalLayer layer, float age)
        {
            float t = Mathf.Repeat(age * 0.03f, 1f);
            float shift = (Mathf.PerlinNoise(age * 0.1f, 0.123f) - 0.5f) * 0.05f;
            var c = layer.layerColor;
            c.r = Mathf.Clamp01(c.r + shift * 0.6f);
            c.g = Mathf.Clamp01(c.g + shift * 0.4f);
            c.b = Mathf.Clamp01(c.b - shift * 0.5f);
            layer.layerColor = Color.Lerp(layer.layerColor, c, 0.2f);
        }

        private Color Desaturate(Color c)
        {
            float gray = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            return new Color(Mathf.Lerp(c.r, gray, 0.5f), Mathf.Lerp(c.g, gray, 0.5f), Mathf.Lerp(c.b, gray, 0.5f), c.a);
        }

        private FaultStructure GenerateFault(float tectonicActivity)
        {
            float r = (float)(rnd?.NextDouble() ?? UnityEngine.Random.value);
            var type = r < 0.4f ? FaultType.Normal : (r < 0.75f ? FaultType.Reverse : FaultType.StrikeSlip);
            return new FaultStructure
            {
                faultType = type,
                displacement = Mathf.Lerp(5f, 100f, Mathf.Clamp01(tectonicActivity)) * (0.5f + (float)UnityEngine.Random.value),
                age = Mathf.Lerp(1f, 150f, UnityEngine.Random.value),
                isActive = tectonicActivity > 0.6f && UnityEngine.Random.value > 0.3f
            };
        }
    }
}