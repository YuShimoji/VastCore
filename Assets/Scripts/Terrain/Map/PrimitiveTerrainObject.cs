using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Terrain.Map
{
    /// <summary>
    /// プリミティブ地形オブジェクトのデータコンテナ
    /// </summary>
    public class PrimitiveTerrainObject : MonoBehaviour
    {
        public GenerationPrimitiveType primitiveType;
        public bool isClimbable;
        public bool isGrindable;
        public bool hasCollision = true;

        public void InitializeFromPool(GenerationPrimitiveType type, Vector3 position, float scale)
        {
            primitiveType = type;
            transform.position = position;
            transform.localScale = new Vector3(scale, scale, scale);
            gameObject.SetActive(true);

            UpdateInteractionSettings();
        }

        public void PrepareForPool()
        {
            gameObject.SetActive(false);
        }

        private void UpdateInteractionSettings()
        {
            switch (primitiveType)
            {
                case GenerationPrimitiveType.Sphere:
                case GenerationPrimitiveType.Boulder:
                    isClimbable = true;
                    isGrindable = false;
                    break;
                case GenerationPrimitiveType.Ring:
                case GenerationPrimitiveType.Torus:
                    isClimbable = false;
                    isGrindable = true;
                    break;
                default:
                    isClimbable = true;
                    isGrindable = false;
                    break;
            }
        }
    }
}
