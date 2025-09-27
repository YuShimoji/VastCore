using System.Collections;
using UnityEngine;

namespace Vastcore.Generation
{
    // Temporary stub: NaturalTerrainTestRunner
    public class NaturalTerrainTestRunner : MonoBehaviour
    {
        public bool runOnStart = false;
        public int testResolution = 128;
        public float testTileSize = 1000f;
        public float testMaxHeight = 50f;
    }

    // Temporary stub: NaturalTerrainFeatures
    public class NaturalTerrainFeatures : MonoBehaviour
    {
        public bool enableRiverGeneration = true;
        public bool enableMountainGeneration = true;
        public bool enableValleyGeneration = true;
        public int maxRiversPerTile = 2;
        public int maxMountainRanges = 1;
        public float riverWidth = 10f;
        public float riverDepth = 3f;
        public float mountainHeight = 100f;
        public float valleyDepth = 20f;
    }
}
