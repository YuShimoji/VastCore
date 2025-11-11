using UnityEngine;
using System.Collections.Generic;

#if DEFORM_AVAILABLE
using Deform;
#endif

namespace Vastcore.Generation
{
    /// <summary>
    /// Deform統合インターフェース
    /// </summary>
    public interface IDeformIntegration
    {
        void ApplyDeformPreset(GameObject target, DeformPreset preset);
        void RemoveDeformFromObject(GameObject target);
        bool HasDeformableComponent(GameObject target);
        object GetDeformable(GameObject target);
    }

    /// <summary>
    /// Deform統合ベースクラス
    /// VastcoreのStructure GeneratorとDeformパッケージの統合を提供
    /// </summary>
    public abstract class DeformIntegrationBase : MonoBehaviour, IDeformIntegration
    {
        [Header("Deform設定")]
        [SerializeField] protected bool enableDeformIntegration = true;
        [SerializeField] protected Vastcore.Core.DeformPresetLibrary deformPresetLibrary;

        protected virtual void Awake()
        {
            InitializeDeformIntegration();
        }

        /// <summary>
        /// Deform統合の初期化
        /// </summary>
        protected virtual void InitializeDeformIntegration()
        {
            if (!enableDeformIntegration) return;

#if DEFORM_AVAILABLE
            // Deformマネージャーに登録
            if (Vastcore.Core.VastcoreDeformManager.Instance != null)
            {
                // 統合準備完了
                Debug.Log("Deform integration initialized");
            }
#endif
        }

        /// <summary>
        /// Deformプリセットをオブジェクトに適用
        /// </summary>
        public virtual void ApplyDeformPreset(GameObject target, DeformPreset preset)
        {
            if (!enableDeformIntegration || target == null || preset == null) return;

#if DEFORM_AVAILABLE
            // Deformableコンポーネントの確保
            var deformable = EnsureDeformableComponent(target);

            // プリセット適用
            ApplyPresetToDeformable(deformable, preset);

            // マネージャーに登録
            Vastcore.Core.VastcoreDeformManager.Instance?.RegisterDeformable(deformable);
#endif
        }

        /// <summary>
        /// オブジェクトからDeformを削除
        /// </summary>
        public virtual void RemoveDeformFromObject(GameObject target)
        {
            if (target == null) return;

#if DEFORM_AVAILABLE
            var deformable = target.GetComponent<Deformable>();
            if (deformable != null)
            {
                Vastcore.Core.VastcoreDeformManager.Instance?.UnregisterDeformable(deformable);
                DestroyImmediate(deformable);
            }
#endif
        }

        /// <summary>
        /// Deformableコンポーネントの有無を確認
        /// </summary>
        public virtual bool HasDeformableComponent(GameObject target)
        {
#if DEFORM_AVAILABLE
            return target != null && target.GetComponent<Deformable>() != null;
#else
            return false;
#endif
        }

        /// <summary>
        /// Deformableコンポーネントを取得
        /// </summary>
        public virtual object GetDeformable(GameObject target)
        {
#if DEFORM_AVAILABLE
            return target?.GetComponent<Deformable>();
#else
            return null;
#endif
        }

#if DEFORM_AVAILABLE
        /// <summary>
        /// Deformableコンポーネントを確保
        /// </summary>
        protected virtual Deformable EnsureDeformableComponent(GameObject target)
        {
            var deformable = target.GetComponent<Deformable>();
            if (deformable == null)
            {
                deformable = target.AddComponent<Deformable>();

                // メッシュフィルターがある場合、メッシュを設定
                var meshFilter = target.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    deformable.Mesh = meshFilter.sharedMesh;
                }
            }
            return deformable;
        }

        /// <summary>
        /// プリセットをDeformableに適用
        /// </summary>
        protected virtual void ApplyPresetToDeformable(Deformable deformable, DeformPreset preset)
        {
            // 実装はサブクラスで
            Debug.LogWarning("ApplyPresetToDeformable not implemented in base class");
        }
#endif
    }

    /// <summary>
    /// Deformプリセットデータクラス
    /// </summary>
    [System.Serializable]
    public class DeformPreset
    {
        public string presetName;
        public DeformPresetType presetType;
        public float intensity = 1f;
        public bool enabled = true;

        // 共通パラメータ
        public Vector3 axis = Vector3.up;
        public bool useLocalAxis = false;

        // タイプ固有パラメータ
        public BendDeformPreset bendPreset;
        public NoiseDeformPreset noisePreset;
        public ScaleDeformPreset scalePreset;

        public enum DeformPresetType
        {
            Bend,
            Noise,
            Scale,
            Twist,
            Spherify,
            Custom
        }
    }

    /// <summary>
    /// Bend変形プリセット
    /// </summary>
    [System.Serializable]
    public class BendDeformPreset
    {
        public float angle = 45f;
        public AnimationCurve bendCurve = AnimationCurve.Linear(0, 0, 1, 1);
    }

    /// <summary>
    /// Noise変形プリセット
    /// </summary>
    [System.Serializable]
    public class NoiseDeformPreset
    {
        public float frequency = 1f;
        public float amplitude = 1f;
        public int octaves = 3;
    }

    /// <summary>
    /// Scale変形プリセット
    /// </summary>
    [System.Serializable]
    public class ScaleDeformPreset
    {
        public Vector3 scaleMultiplier = Vector3.one;
        public AnimationCurve scaleCurve = AnimationCurve.Linear(0, 0, 1, 1);
    }

    /// <summary>
    /// 変形マスクシステム
    /// Deformの適用を特定の領域に制限
    /// </summary>
    [System.Serializable]
    public class DeformMask
    {
        public enum MaskType
        {
            None,
            Box,
            Sphere,
            Cylinder,
            Custom
        }

        public MaskType maskType = MaskType.None;
        public Vector3 maskPosition = Vector3.zero;
        public Vector3 maskSize = Vector3.one;
        public float maskRadius = 1f;
        public float maskHeight = 1f;
        public Texture2D customMaskTexture;
        public AnimationCurve maskFalloffCurve = AnimationCurve.Linear(0, 0, 1, 1);
        public bool invertMask = false;

        /// <summary>
        /// 指定位置のマスク値を計算
        /// </summary>
        public float GetMaskValue(Vector3 position)
        {
            if (maskType == MaskType.None) return 1f;

            float maskValue = 0f;

            switch (maskType)
            {
                case MaskType.Box:
                    maskValue = CalculateBoxMask(position);
                    break;
                case MaskType.Sphere:
                    maskValue = CalculateSphereMask(position);
                    break;
                case MaskType.Cylinder:
                    maskValue = CalculateCylinderMask(position);
                    break;
                case MaskType.Custom:
                    maskValue = CalculateCustomMask(position);
                    break;
            }

            if (invertMask) maskValue = 1f - maskValue;

            return maskFalloffCurve.Evaluate(maskValue);
        }

        private float CalculateBoxMask(Vector3 position)
        {
            Vector3 localPos = position - maskPosition;
            Vector3 halfSize = maskSize * 0.5f;

            float x = Mathf.Clamp01((halfSize.x - Mathf.Abs(localPos.x)) / halfSize.x);
            float y = Mathf.Clamp01((halfSize.y - Mathf.Abs(localPos.y)) / halfSize.y);
            float z = Mathf.Clamp01((halfSize.z - Mathf.Abs(localPos.z)) / halfSize.z);

            return Mathf.Min(x, Mathf.Min(y, z));
        }

        private float CalculateSphereMask(Vector3 position)
        {
            float distance = Vector3.Distance(position, maskPosition);
            return Mathf.Clamp01((maskRadius - distance) / maskRadius);
        }

        private float CalculateCylinderMask(Vector3 position)
        {
            Vector3 localPos = position - maskPosition;
            localPos.y = 0; // Y軸を無視

            float radialDistance = localPos.magnitude;
            float radialMask = Mathf.Clamp01((maskRadius - radialDistance) / maskRadius);

            float heightDistance = Mathf.Abs((position - maskPosition).y);
            float heightMask = Mathf.Clamp01((maskHeight * 0.5f - heightDistance) / (maskHeight * 0.5f));

            return Mathf.Min(radialMask, heightMask);
        }

        private float CalculateCustomMask(Vector3 position)
        {
            if (customMaskTexture == null) return 1f;

            // 簡易実装：位置に基づくUV計算
            float u = (position.x - maskPosition.x) / maskSize.x + 0.5f;
            float v = (position.z - maskPosition.z) / maskSize.z + 0.5f;

            if (u < 0 || u > 1 || v < 0 || v > 1) return 0f;

            Color pixel = customMaskTexture.GetPixelBilinear(u, v);
            return pixel.r; // Rチャンネルを使用
        }
    }

    /// <summary>
    /// アニメーション変形システム
    /// 時間経過で変形をアニメーション化
    /// </summary>
    [System.Serializable]
    public class DeformAnimation
    {
        public bool enableAnimation = false;
        public float animationDuration = 2f;
        public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public bool loopAnimation = false;
        public float loopDelay = 1f;

        [System.NonSerialized] private float animationStartTime;
        [System.NonSerialized] private bool isAnimating;
        [System.NonSerialized] private float currentLoopDelay;

        /// <summary>
        /// アニメーションを開始
        /// </summary>
        public void StartAnimation()
        {
            animationStartTime = Time.time;
            isAnimating = true;
            currentLoopDelay = 0f;
        }

        /// <summary>
        /// アニメーションを停止
        /// </summary>
        public void StopAnimation()
        {
            isAnimating = false;
        }

        /// <summary>
        /// 現在のアニメーション強度を取得
        /// </summary>
        public float GetAnimationStrength()
        {
            if (!enableAnimation || !isAnimating) return 1f;

            float elapsedTime = Time.time - animationStartTime - currentLoopDelay;
            if (elapsedTime < 0) return 0f;

            float normalizedTime = elapsedTime / animationDuration;

            if (loopAnimation && normalizedTime >= 1f)
            {
                // ループ処理
                currentLoopDelay += animationDuration + loopDelay;
                animationStartTime = Time.time;
                normalizedTime = 0f;
            }
            else if (!loopAnimation && normalizedTime >= 1f)
            {
                // アニメーション終了
                isAnimating = false;
                return 1f;
            }

            return animationCurve.Evaluate(normalizedTime);
        }

        /// <summary>
        /// アニメーションが実行中か
        /// </summary>
        public bool IsAnimating() => isAnimating;
    }

    /// <summary>
    /// 高度なDeform統合クラス
    /// マスクとアニメーションをサポート
    /// </summary>
    public class AdvancedDeformIntegration : DeformIntegrationBase
    {
        [Header("マスク設定")]
        [SerializeField] protected DeformMask deformMask = new DeformMask();

        [Header("アニメーション設定")]
        [SerializeField] protected DeformAnimation deformAnimation = new DeformAnimation();

        protected override void Awake()
        {
            base.Awake();
            InitializeAdvancedFeatures();
        }

        /// <summary>
        /// 高度な機能を初期化
        /// </summary>
        private void InitializeAdvancedFeatures()
        {
            if (deformAnimation.enableAnimation)
            {
                deformAnimation.StartAnimation();
            }
        }

        public override void ApplyDeformPreset(GameObject target, DeformPreset preset)
        {
            if (!enableDeformIntegration || target == null || preset == null) return;

#if DEFORM_AVAILABLE
            // マスクとアニメーションを適用したプリセットを作成
            var maskedPreset = CreateMaskedPreset(preset);
            var animatedPreset = ApplyAnimationToPreset(maskedPreset);

            base.ApplyDeformPreset(target, animatedPreset);
#endif
        }

        /// <summary>
        /// マスクを適用したプリセットを作成
        /// </summary>
        private DeformPreset CreateMaskedPreset(DeformPreset original)
        {
#if DEFORM_AVAILABLE
            var maskedPreset = new DeformPreset
            {
                presetName = original.presetName + "_Masked",
                presetType = original.presetType,
                intensity = original.intensity * deformMask.GetMaskValue(transform.position),
                enabled = original.enabled,
                axis = original.axis,
                useLocalAxis = original.useLocalAxis
            };

            // タイプ固有パラメータのコピー
            maskedPreset.bendPreset = original.bendPreset != null ? new BendDeformPreset
            {
                angle = original.bendPreset.angle,
                bendCurve = original.bendPreset.bendCurve
            } : null;

            maskedPreset.noisePreset = original.noisePreset != null ? new NoiseDeformPreset
            {
                frequency = original.noisePreset.frequency,
                amplitude = original.noisePreset.amplitude,
                octaves = original.noisePreset.octaves
            } : null;

            maskedPreset.scalePreset = original.scalePreset != null ? new ScaleDeformPreset
            {
                scaleMultiplier = original.scalePreset.scaleMultiplier,
                scaleCurve = original.scalePreset.scaleCurve
            } : null;

            return maskedPreset;
#else
            return original;
#endif
        }

        /// <summary>
        /// アニメーションをプリセットに適用
        /// </summary>
        private DeformPreset ApplyAnimationToPreset(DeformPreset preset)
        {
            if (!deformAnimation.enableAnimation) return preset;

            float animationStrength = deformAnimation.GetAnimationStrength();

            var animatedPreset = new DeformPreset
            {
                presetName = preset.presetName + "_Animated",
                presetType = preset.presetType,
                intensity = preset.intensity * animationStrength,
                enabled = preset.enabled,
                axis = preset.axis,
                useLocalAxis = preset.useLocalAxis
            };

            // アニメーション強度をタイプ固有パラメータに適用
            if (preset.bendPreset != null)
            {
                animatedPreset.bendPreset = new BendDeformPreset
                {
                    angle = preset.bendPreset.angle * animationStrength,
                    bendCurve = preset.bendPreset.bendCurve
                };
            }

            if (preset.noisePreset != null)
            {
                animatedPreset.noisePreset = new NoiseDeformPreset
                {
                    frequency = preset.noisePreset.frequency,
                    amplitude = preset.noisePreset.amplitude * animationStrength,
                    octaves = preset.noisePreset.octaves
                };
            }

            if (preset.scalePreset != null)
            {
                animatedPreset.scalePreset = new ScaleDeformPreset
                {
                    scaleMultiplier = Vector3.Lerp(Vector3.one, preset.scalePreset.scaleMultiplier, animationStrength),
                    scaleCurve = preset.scalePreset.scaleCurve
                };
            }

            return animatedPreset;
        }

        /// <summary>
        /// マスク設定を取得
        /// </summary>
        public DeformMask GetDeformMask() => deformMask;

        /// <summary>
        /// アニメーション設定を取得
        /// </summary>
        public DeformAnimation GetDeformAnimation() => deformAnimation;

        /// <summary>
        /// マスクを更新
        /// </summary>
        public void UpdateMask(DeformMask newMask)
        {
            deformMask = newMask;
        }

        /// <summary>
        /// アニメーションを更新
        /// </summary>
        public void UpdateAnimation(DeformAnimation newAnimation)
        {
            deformAnimation = newAnimation;
            if (deformAnimation.enableAnimation && !deformAnimation.IsAnimating())
            {
                deformAnimation.StartAnimation();
            }
        }
    }
}
