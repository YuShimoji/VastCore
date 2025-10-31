using UnityEngine;
using System.Collections.Generic;
using Vastcore.Core;

namespace Vastcore.Generation
{
    /// <summary>
    /// 地形変形プリセット
    /// 異なる地形タイプに対する最適化された変形設定を定義
    /// </summary>
    [CreateAssetMenu(fileName = "DeformationPreset", menuName = "Vastcore/Deformation Preset", order = 1)]
    public class DeformationPreset : ScriptableObject
    {
        [Header("基本設定")]
        [SerializeField] private string presetName = "Default Preset";
        [SerializeField] private GenerationPrimitiveType targetTerrainType;
        [SerializeField] private VastcoreDeformManager.DeformQualityLevel defaultQualityLevel = VastcoreDeformManager.DeformQualityLevel.High;

        [Header("ノイズ変形設定")]
        [SerializeField] private bool useNoiseDeformation = true;
        [SerializeField] private float noiseIntensity = 0.1f;
        [SerializeField] private float noiseFrequency = 1f;
        [SerializeField] private bool animateNoise = false;
        [SerializeField] private float noiseAnimationDuration = 1f;

        [Header("ディスプレイス変形設定")]
        [SerializeField] private bool useDisplaceDeformation = false;
        [SerializeField] private float displaceStrength = 0.5f;
        [SerializeField] private Texture2D displaceMap;
        [SerializeField] private bool animateDisplace = false;
        [SerializeField] private float displaceAnimationDuration = 1f;

        [Header("アニメーション設定")]
        [SerializeField] private bool enableTerrainSpecificDeformation = true;
        [SerializeField] private float deformationDelay = 0f;

        /// <summary>
        /// プリセット名を取得
        /// </summary>
        public string PresetName => presetName;

        /// <summary>
        /// 対象地形タイプを取得
        /// </summary>
        public GenerationPrimitiveType TargetTerrainType => targetTerrainType;

        /// <summary>
        /// デフォルト品質レベルを取得
        /// </summary>
        public VastcoreDeformManager.DeformQualityLevel DefaultQualityLevel => defaultQualityLevel;

        /// <summary>
        /// プリセットを適用
        /// </summary>
        public void ApplyToTerrainObject(PrimitiveTerrainObject terrainObject)
        {
            if (terrainObject == null) return;

            // 品質レベル設定
            terrainObject.UpdateDeformSettings(true, defaultQualityLevel);

            // 遅延適用
            if (deformationDelay > 0f)
            {
                terrainObject.StartCoroutine(DelayedApplyDeformation(terrainObject));
            }
            else
            {
                ApplyDeformationImmediate(terrainObject);
            }
        }

        /// <summary>
        /// 遅延適用コルーチン
        /// </summary>
        private System.Collections.IEnumerator DelayedApplyDeformation(PrimitiveTerrainObject terrainObject)
        {
            yield return new WaitForSeconds(deformationDelay);
            ApplyDeformationImmediate(terrainObject);
        }

        /// <summary>
        /// 変形を即時適用
        /// </summary>
        private void ApplyDeformationImmediate(PrimitiveTerrainObject terrainObject)
        {
            // 地形固有変形を適用
            if (enableTerrainSpecificDeformation)
            {
                terrainObject.ApplyTerrainSpecificDeformation();
                return; // 地形固有変形が優先
            }

            // カスタム変形を適用
            if (useNoiseDeformation)
            {
                if (animateNoise)
                {
                    terrainObject.ApplyNoiseDeformationAnimated(noiseIntensity, noiseFrequency, noiseAnimationDuration);
                }
                else
                {
                    terrainObject.ApplyNoiseDeformation(noiseIntensity, noiseFrequency);
                }
            }

            if (useDisplaceDeformation)
            {
                if (animateDisplace)
                {
                    terrainObject.ApplyDisplaceDeformationAnimated(displaceStrength, displaceMap, displaceAnimationDuration);
                }
                else
                {
                    terrainObject.ApplyDisplaceDeformation(displaceStrength, displaceMap);
                }
            }
        }

        /// <summary>
        /// デフォルトプリセットを作成
        /// </summary>
        public static DeformationPreset CreateDefaultPreset(GenerationPrimitiveType terrainType)
        {
            var preset = CreateInstance<DeformationPreset>();
            preset.presetName = $"{terrainType} Default";
            preset.targetTerrainType = terrainType;
            preset.enableTerrainSpecificDeformation = true;

            // 地形タイプに応じたデフォルト設定
            switch (terrainType)
            {
                case GenerationPrimitiveType.Crystal:
                    preset.noiseIntensity = 0.15f;
                    preset.noiseFrequency = 2.0f;
                    break;
                case GenerationPrimitiveType.Boulder:
                    preset.noiseIntensity = 0.2f;
                    preset.noiseFrequency = 1.5f;
                    break;
                case GenerationPrimitiveType.Mesa:
                    preset.noiseIntensity = 0.08f;
                    preset.noiseFrequency = 0.8f;
                    break;
                case GenerationPrimitiveType.Spire:
                    preset.noiseIntensity = 0.1f;
                    preset.noiseFrequency = 1.2f;
                    break;
                case GenerationPrimitiveType.Formation:
                    preset.noiseIntensity = 0.12f;
                    preset.noiseFrequency = 0.7f;
                    break;
                default:
                    preset.noiseIntensity = 0.05f;
                    preset.noiseFrequency = 0.5f;
                    break;
            }

            return preset;
        }
    }
}
