using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    public static class TreeGenerator
    {
        public static void ConfigureTrees(TerrainGenerator generator, TerrainData terrainData)
        {
            if (generator.TreePrototypes == null || generator.TreePrototypes.Length == 0)
            {
                return;
            }

            terrainData.treePrototypes = generator.TreePrototypes;
            terrainData.RefreshPrototypes();

            // 既存のツリーをクリア
            terrainData.treeInstances = new TreeInstance[0];

            var instances = new List<TreeInstance>();

            // 配置ルール:
            // - 標高: 0.15..0.65 (極端な低地/高地を回避)
            // - 傾斜: 30度未満（急斜面を回避）
            // - グリッドサンプリング + ジッターで分布の均一性確保
            int maxInstances = 2000;
            int placed = 0;
            float step = 0.02f; // UV 空間でのステップ
            for (float vz = 0f; vz < 1f && placed < maxInstances; vz += step)
            {
                for (float ux = 0f; ux < 1f && placed < maxInstances; ux += step)
                {
                    float u = Mathf.Clamp01(ux + Random.Range(-step * 0.3f, step * 0.3f));
                    float v = Mathf.Clamp01(vz + Random.Range(-step * 0.3f, step * 0.3f));

                    // 高さ・傾斜を取得
                    float worldHeight = terrainData.GetInterpolatedHeight(u, v);
                    float normHeight = Mathf.Clamp01(worldHeight / terrainData.size.y);
                    float slope = terrainData.GetSteepness(u, v);

                    if (normHeight < 0.15f || normHeight > 0.65f) continue;
                    if (slope >= 30f) continue;

                    // 局所的なばらつき: 平坦+適高度ほど確率を上げる
                    float heightWeight = 1f - Mathf.Abs(normHeight - 0.4f) / 0.25f; // 0.4 付近が最大
                    heightWeight = Mathf.Clamp01(heightWeight);
                    float slopeWeight = 1f - Mathf.Clamp01(slope / 30f);
                    float prob = Mathf.Clamp01(0.5f * heightWeight * (0.5f + 0.5f * slopeWeight));

                    if (Random.value > prob) continue;

                    var ti = new TreeInstance
                    {
                        position = new Vector3(u, normHeight, v),
                        prototypeIndex = Random.Range(0, terrainData.treePrototypes.Length),
                        widthScale = Random.Range(0.9f, 1.2f),
                        heightScale = Random.Range(0.9f, 1.3f),
                        color = Color.white,
                        lightmapColor = Color.white
                    };
                    instances.Add(ti);
                    placed++;
                }
            }

            if (instances.Count > 0)
            {
                terrainData.SetTreeInstances(instances.ToArray(), true);
            }
        }
    }
}
