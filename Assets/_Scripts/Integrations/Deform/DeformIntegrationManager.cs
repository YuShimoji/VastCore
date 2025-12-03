using UnityEngine;
using System;
using System.Collections.Generic;

#if DEFORM_AVAILABLE
using Deform;
#endif

namespace Vastcore.Integration.Deform
{
    /// <summary>
    /// Manages the application of Deform effects to generated meshes.
    /// Provides a bridge between Structure Generator and VastcoreDeformManager.
    /// </summary>
    public static class DeformIntegrationManager
    {
        #region Deformer Type Definitions
        
        /// <summary>
        /// Supported deformer types for quick access.
        /// </summary>
        public enum DeformerType
        {
            Bend,
            Twist,
            Taper,
            Bulge,
            Noise,
            Sine,
            Ripple,
            Wave,
            Spherify,
            Melt,
            Flare,
            Squash,
            Curve,
            Lattice,
            Magnet,
            Turbulence
        }
        
        #endregion

        #region Deformer Settings
        
        /// <summary>
        /// Common settings for deformer application.
        /// </summary>
        [Serializable]
        public struct DeformerSettings
        {
            public DeformerType type;
            public float strength;
            public Vector3 axis;
            public Vector3 center;
            public bool animate;
            public float animationSpeed;
            
            public static DeformerSettings Default(DeformerType type)
            {
                return new DeformerSettings
                {
                    type = type,
                    strength = 1.0f,
                    axis = Vector3.up,
                    center = Vector3.zero,
                    animate = false,
                    animationSpeed = 1.0f
                };
            }
        }
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Checks if the Deform package is available.
        /// </summary>
        public static bool IsDeformAvailable
        {
            get
            {
#if DEFORM_AVAILABLE
                return true;
#else
                return false;
#endif
            }
        }
        
        /// <summary>
        /// Applies a deformer to a GameObject.
        /// </summary>
        /// <param name="target">The target GameObject.</param>
        /// <param name="settings">The deformer settings to apply.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public static bool ApplyDeformer(GameObject target, DeformerSettings settings)
        {
            if (target == null)
            {
                Debug.LogError("[DeformIntegrationManager] Target GameObject is null.");
                return false;
            }

#if DEFORM_AVAILABLE
            try
            {
                // Ensure Deformable component exists
                var deformable = target.GetComponent<Deformable>();
                if (deformable == null)
                {
                    deformable = target.AddComponent<Deformable>();
                }
                
                // Add the appropriate deformer based on type
                Deformer deformer = AddDeformerComponent(target, settings.type);
                if (deformer == null)
                {
                    Debug.LogError($"[DeformIntegrationManager] Failed to add deformer of type {settings.type}");
                    return false;
                }
                
                // Apply common settings
                ApplyDeformerSettings(deformer, settings);
                
                // Register with VastcoreDeformManager
                RegisterWithManager(deformable);
                
                Debug.Log($"[DeformIntegrationManager] Applied {settings.type} deformer to {target.name}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DeformIntegrationManager] Error applying deformer: {ex.Message}");
                return false;
            }
#else
            Debug.LogWarning("[DeformIntegrationManager] Deform package is not available.");
            return false;
#endif
        }
        
        /// <summary>
        /// Removes all deformers from a GameObject.
        /// </summary>
        public static void RemoveAllDeformers(GameObject target)
        {
            if (target == null) return;

#if DEFORM_AVAILABLE
            var deformers = target.GetComponents<Deformer>();
            foreach (var deformer in deformers)
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(deformer);
                else
                    UnityEngine.Object.DestroyImmediate(deformer);
            }
            
            var deformable = target.GetComponent<Deformable>();
            if (deformable != null)
            {
                UnregisterFromManager(deformable);
                
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(deformable);
                else
                    UnityEngine.Object.DestroyImmediate(deformable);
            }
            
            Debug.Log($"[DeformIntegrationManager] Removed all deformers from {target.name}");
#endif
        }
        
        /// <summary>
        /// Gets the list of deformers on a GameObject.
        /// </summary>
        public static List<string> GetActiveDeformers(GameObject target)
        {
            var result = new List<string>();
            if (target == null) return result;

#if DEFORM_AVAILABLE
            var deformers = target.GetComponents<Deformer>();
            foreach (var deformer in deformers)
            {
                result.Add(deformer.GetType().Name);
            }
#endif
            return result;
        }
        
        #endregion

        #region Private Helpers
        
#if DEFORM_AVAILABLE
        /// <summary>
        /// Adds a deformer component based on the type.
        /// </summary>
        private static Deformer AddDeformerComponent(GameObject target, DeformerType type)
        {
            // Map DeformerType to actual Deform component types
            switch (type)
            {
                case DeformerType.Bend:
                    return target.AddComponent<BendDeformer>();
                case DeformerType.Twist:
                    return target.AddComponent<TwistDeformer>();
                case DeformerType.Taper:
                    return target.AddComponent<TaperDeformer>();
                case DeformerType.Noise:
                    return target.AddComponent<NoiseDeformer>();
                case DeformerType.Sine:
                    return target.AddComponent<SineDeformer>();
                case DeformerType.Ripple:
                    return target.AddComponent<RippleDeformer>();
                case DeformerType.Wave:
                    return target.AddComponent<WaveDeformer>();
                case DeformerType.Spherify:
                    return target.AddComponent<SpherifyDeformer>();
                case DeformerType.Squash:
                    return target.AddComponent<SquashAndStretchDeformer>();
                case DeformerType.Curve:
                    return target.AddComponent<CurveDeformer>();
                case DeformerType.Lattice:
                    return target.AddComponent<LatticeDeformer>();
                case DeformerType.Magnet:
                    return target.AddComponent<MagnetDeformer>();
                default:
                    Debug.LogWarning($"[DeformIntegrationManager] Deformer type {type} not implemented, using BendDeformer as fallback.");
                    return target.AddComponent<BendDeformer>();
            }
        }
        
        /// <summary>
        /// Applies common settings to a deformer.
        /// </summary>
        private static void ApplyDeformerSettings(Deformer deformer, DeformerSettings settings)
        {
            // Apply axis - most deformers have an Axis property
            var axisProperty = deformer.GetType().GetProperty("Axis");
            if (axisProperty != null && axisProperty.PropertyType == typeof(Vector3))
            {
                axisProperty.SetValue(deformer, settings.axis);
            }
            
            // Apply factor/strength - property name varies by deformer type
            var factorProperty = deformer.GetType().GetProperty("Factor");
            if (factorProperty != null && factorProperty.PropertyType == typeof(float))
            {
                factorProperty.SetValue(deformer, settings.strength);
            }
        }
        
        /// <summary>
        /// Registers a Deformable with VastcoreDeformManager.
        /// </summary>
        private static void RegisterWithManager(Deformable deformable)
        {
            if (Vastcore.Generation.VastcoreDeformManager.Instance != null)
            {
                Vastcore.Generation.VastcoreDeformManager.Instance.RegisterDeformable(
                    deformable,
                    Vastcore.Generation.VastcoreDeformManager.DeformQualityLevel.High);
            }
        }
        
        /// <summary>
        /// Unregisters a Deformable from VastcoreDeformManager.
        /// </summary>
        private static void UnregisterFromManager(Deformable deformable)
        {
            if (Vastcore.Generation.VastcoreDeformManager.Instance != null)
            {
                Vastcore.Generation.VastcoreDeformManager.Instance.UnregisterDeformable(deformable);
            }
        }
#endif
        
        #endregion
    }
}