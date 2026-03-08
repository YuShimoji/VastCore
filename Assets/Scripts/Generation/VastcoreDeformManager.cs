#if DEFORM_AVAILABLE
using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    /// <summary>
    /// Manages Deformable objects and their quality levels.
    /// Recreated stub to fix missing reference.
    /// </summary>
    public class VastcoreDeformManager : MonoBehaviour
    {
        private static VastcoreDeformManager instance;
        public static VastcoreDeformManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<VastcoreDeformManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("VastcoreDeformManager");
                        instance = go.AddComponent<VastcoreDeformManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        public enum DeformQualityLevel
        {
            Low,
            Medium,
            High,
            Ultra
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private readonly HashSet<object> registeredDeformables = new HashSet<object>();

        public int RegisteredCount => registeredDeformables.Count;

        public void RegisterDeformable(object deformable, DeformQualityLevel quality = DeformQualityLevel.Medium)
        {
            if (deformable != null)
                registeredDeformables.Add(deformable);
        }

        public void UnregisterDeformable(object deformable)
        {
            if (deformable != null)
                registeredDeformables.Remove(deformable);
        }
    }
}
#endif

