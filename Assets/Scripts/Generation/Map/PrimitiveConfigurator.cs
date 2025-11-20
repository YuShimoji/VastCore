using UnityEngine;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// プリミティブ設定処理クラス
    /// </summary>
    public static class PrimitiveConfigurator
    {
        /// <summary>
        /// マテリアルを設定
        /// </summary>
        public static void SetupMaterial(GameObject primitiveObject, PrimitiveTerrainGenerator.PrimitiveGenerationParams parameters)
        {
            var renderer = primitiveObject.GetComponent<MeshRenderer>();
            if (renderer != null && parameters.material != null)
            {
                renderer.material = parameters.material;

                // 色のバリエーションを適用
                if (parameters.randomizeMaterial)
                {
                    var materialInstance = new Material(parameters.material);
                    materialInstance.color = parameters.colorVariation;
                    renderer.material = materialInstance;
                }
            }
        }

        /// <summary>
        /// コライダーを生成
        /// </summary>
        public static void GenerateCollider(GameObject primitiveObject, PrimitiveTerrainGenerator.PrimitiveGenerationParams parameters)
        {
            // 既存のコライダーを削除
            var existingCollider = primitiveObject.GetComponent<Collider>();
            if (existingCollider != null)
            {
                Object.DestroyImmediate(existingCollider);
            }

            // メッシュコライダーを追加
            var meshCollider = primitiveObject.AddComponent<MeshCollider>();
            meshCollider.convex = false; // 大きなオブジェクトなので非凸型

            var meshFilter = primitiveObject.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                meshCollider.sharedMesh = meshFilter.sharedMesh;
            }
        }

        /// <summary>
        /// インタラクション用コンポーネントを設定
        /// </summary>
        public static void SetupInteractionComponents(GameObject primitiveObject, PrimitiveTerrainGenerator.PrimitiveGenerationParams parameters)
        {
            // プリミティブ地形オブジェクトコンポーネントを追加
            // var primitiveComponent = primitiveObject.AddComponent<PrimitiveTerrainObject>();
            // primitiveComponent.primitiveType = (GenerationPrimitiveType)(int)parameters.primitiveType;
            // primitiveComponent.isClimbable = parameters.isClimbable;
            // primitiveComponent.isGrindable = parameters.isGrindable;
            // primitiveComponent.hasCollision = parameters.generateCollider;

            // 適切なレイヤーを設定
            primitiveObject.layer = LayerMask.NameToLayer("Default"); // 必要に応じて専用レイヤーを作成
        }
    }
}
