using UnityEngine;

namespace Vastcore.Generation
{
    // テストから直接参照できるようトップレベルで公開
    public struct VegetationData
    {
        public float density; // 0..1
        public string type;   // 表示用
    }

    public struct ErosionData
    {
        public float waterErosion; // 0..1
        public float windErosion;  // 0..1
    }

    public class ClimateTerrainFeedbackSystem : MonoBehaviour
    {
        // フィードバック強度（簡易実装）
        private float vegetationIntensity = 1f;
        private float erosionIntensity = 1f;
        private float couplingIntensity = 1f;

        // 直近の状態（簡易キャッシュ）
        private VegetationData lastVegetation;
        private ErosionData lastErosion;

        public void Initialize()
        {
        }

        public void UpdateClimateEffects()
        {
        }

        public VegetationData GetVegetationAt(Vector3 worldPosition)
        {
            // 簡易：標高/ノイズから密度を算出
            float n = Mathf.PerlinNoise(worldPosition.x * 0.001f, worldPosition.z * 0.001f);
            float density = Mathf.Clamp01(n * vegetationIntensity);
            string type = density > 0.66f ? "Forest" : density > 0.33f ? "Grassland" : "Sparse";

            lastVegetation = new VegetationData { density = density, type = type };
            return lastVegetation;
        }

        public ErosionData GetErosionAt(Vector3 worldPosition)
        {
            // 簡易：位置ベースの疑似値
            float w = Mathf.Clamp01(Mathf.Abs(Mathf.Sin(worldPosition.x * 0.0005f)) * erosionIntensity);
            float d = Mathf.Clamp01(Mathf.Abs(Mathf.Cos(worldPosition.z * 0.0005f)) * erosionIntensity * 0.5f + 0.25f);
            lastErosion = new ErosionData { waterErosion = w, windErosion = d };
            return lastErosion;
        }

        public void SetFeedbackIntensity(float vegetation, float erosion, float coupling)
        {
            vegetationIntensity = Mathf.Max(0f, vegetation);
            erosionIntensity = Mathf.Max(0f, erosion);
            couplingIntensity = Mathf.Max(0f, coupling);
        }

        public void ResetFeedbackData()
        {
            vegetationIntensity = 1f;
            erosionIntensity = 1f;
            couplingIntensity = 1f;
            lastVegetation = new VegetationData { density = 0f, type = "None" };
            lastErosion = new ErosionData { waterErosion = 0f, windErosion = 0f };
        }
    }
}
