using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// Stub class for ClimateTerrainFeedbackSystem to resolve compilation errors.
    /// Handles interaction between climate and terrain erosion/vegetation.
    /// </summary>
    public class ClimateTerrainFeedbackSystem : MonoBehaviour
    {
        public VegetationData GetVegetationAt(Vector3 position)
        {
            return new VegetationData { density = 0.5f, type = 0 };
        }

        public ErosionData GetErosionAt(Vector3 position)
        {
            return new ErosionData { waterErosion = 0f, windErosion = 0f };
        }

        public void SetFeedbackIntensity(float vegetationIntensity, float erosionIntensity, float weatheringIntensity)
        {
            // Stub
        }

        public void ResetFeedbackData()
        {
            // Stub
        }
    }

    [System.Serializable]
    public struct VegetationData
    {
        public float density;
        public int type; // Placeholder for vegetation type enum
    }

    [System.Serializable]
    public struct ErosionData
    {
        public float waterErosion;
        public float windErosion;
    }
}
