using System;
using System.Collections;
using UnityEngine;

namespace Vastcore.Core.Interfaces
{
    [Serializable]
    public struct TerrainRecoveryRequest
    {
        public float terrainSize;
        public int resolution;
        public float heightScale;
        public float noiseScale;
        public int octaves;
        public float persistence;
        public float lacunarity;
        public Vector2 noiseOffset;
    }

    public interface ITerrainRecoveryService
    {
        IEnumerator RecoverTerrainGeneration(
            TerrainRecoveryRequest request,
            Action<GameObject> onSuccess,
            Action onFailure);

        bool ValidateTerrain(GameObject terrain);
    }
}
