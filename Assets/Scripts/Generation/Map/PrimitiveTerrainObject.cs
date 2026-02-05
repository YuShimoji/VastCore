using UnityEngine;
using System.Collections.Generic;
using Vastcore.Core;
using Vastcore.Generation;

namespace Vastcore.Generation
{
    /// <summary>
    /// Primitive Terrain Object
    /// </summary>
    public class PrimitiveTerrainObject : MonoBehaviour, IPoolable
    {
        [Header("Primitive Info")]
        public GenerationPrimitiveType primitiveType;
        public Vector3 originalScale;
        public float generationTime;

        [Header("Interaction Settings")]
        public bool isClimbable = false;
        public bool isGrindable = false;
        public bool isDestructible = false;
        public float destructionThreshold = 100f;

        [Header("LOD Settings")]
        public bool enableLOD = true;
        public float lodDistance0 = 50f;
        public float lodDistance1 = 100f;
        public float lodDistance2 = 200f;

        [Header("Memory Management")]
        public bool isPooled = false;
        public float lastAccessTime;

        private Collider objectCollider;

        private void Awake()
        {
            objectCollider = GetComponent<Collider>();
            lastAccessTime = Time.time;
        }

        public void OnSpawn() { }
        public void OnDespawn() { }

        // IPoolable interface implementation
        public void OnSpawnFromPool()
        {
            isPooled = true;
            lastAccessTime = Time.time;
            OnSpawn();
        }

        public void OnReturnToPool()
        {
            isPooled = false;
            OnDespawn();
        }

        public bool IsAvailable => !isPooled;

        public void InitializeFromPool() => OnSpawnFromPool();
        public void InitializeFromPool(GenerationPrimitiveType type, Vector3 position, Vector3 scale)
        {
            InitializeFromPool();
            this.primitiveType = type;
            this.transform.position = position;
            this.transform.localScale = scale;
            this.originalScale = scale;
        }
        public void PrepareForPool() => OnReturnToPool();

        [Header("Legacy Support")]
        public Vector3 scale
        {
            get => originalScale;
            set => originalScale = value;
        }
        public bool hasCollision
        {
            get => objectCollider != null && objectCollider.enabled;
            set
            {
                if (objectCollider != null)
                {
                    objectCollider.enabled = value;
                }
            }
        }

        public struct LODStatistics
        {
            public int visibleObjects;
            public int totalObjects;
            public int[] lodCounts;
            public LODStatistics() { visibleObjects = 0; totalObjects = 0; lodCounts = new int[3]; }
        }

        public static LODStatistics GetGlobalLODStatistics()
        {
            return new LODStatistics(); // Stub implementation
        }
    }

}
