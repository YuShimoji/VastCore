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
        Deformable GetDeformable(GameObject target);
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
        public virtual Deformable GetDeformable(GameObject target)
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
}
