using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Generation
{
    /// <summary>
    /// 最小実装のプリミティブ地形マネージャー
    /// テスト用にランダムなプリミティブ生成とカウント/検索/クリーンアップAPIを提供
    /// </summary>
    public class PrimitiveTerrainManager : MonoBehaviour
    {
        [Header("Runtime Generation Settings")]
        [SerializeField] private bool dynamicGenerationEnabled = false;
        [SerializeField] private float spawnInterval = 0.5f;
        [SerializeField] private int primitivesPerSpawn = 3;
        [SerializeField] private float spawnRadiusMin = 800f;
        [SerializeField] private float spawnRadiusMax = 1600f;
        [SerializeField] private int maxActivePrimitives = 200;

        private readonly List<GameObject> activePrimitives = new List<GameObject>();
        private Coroutine spawnRoutine;
        private Transform player;

        private void Awake()
        {
            FindPlayer();
        }

        private void FindPlayer()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else if (Camera.main != null)
            {
                player = Camera.main.transform;
            }
        }

        public void SetDynamicGenerationEnabled(bool enabled)
        {
            dynamicGenerationEnabled = enabled;
            if (enabled)
            {
                if (spawnRoutine == null)
                    spawnRoutine = StartCoroutine(SpawnLoop());
            }
            else
            {
                if (spawnRoutine != null)
                {
                    StopCoroutine(spawnRoutine);
                    spawnRoutine = null;
                }
            }
        }

        public int GetActivePrimitiveCount()
        {
            CleanupNulls();
            return activePrimitives.Count;
        }

        public List<GameObject> GetPrimitivesInRadius(Vector3 center, float radius)
        {
            CleanupNulls();
            var list = new List<GameObject>();
            float r2 = radius * radius;
            foreach (var go in activePrimitives)
            {
                if (go == null) continue;
                if ((go.transform.position - center).sqrMagnitude <= r2)
                    list.Add(go);
            }
            return list;
        }

        public void ForceCleanup()
        {
            foreach (var go in activePrimitives)
            {
                if (go != null)
                {
                    if (Application.isPlaying)
                        Destroy(go);
                    else
                        DestroyImmediate(go);
                }
            }
            activePrimitives.Clear();
            System.GC.Collect();
        }

        private IEnumerator SpawnLoop()
        {
            var wait = new WaitForSeconds(spawnInterval);
            while (dynamicGenerationEnabled)
            {
                if (player == null) FindPlayer();

                if (activePrimitives.Count < maxActivePrimitives)
                {
                    int spawnCount = Mathf.Min(primitivesPerSpawn, maxActivePrimitives - activePrimitives.Count);
                    for (int i = 0; i < spawnCount; i++)
                    {
                        var type = (PrimitiveTerrainGenerator.PrimitiveType)Random.Range(0, (int)PrimitiveTerrainGenerator.PrimitiveType.Formation + 1);
                        var p = PrimitiveTerrainGenerator.PrimitiveGenerationParams.Default(type);

                        Vector3 origin = player != null ? player.position : Vector3.zero;
                        float dist = Random.Range(spawnRadiusMin, spawnRadiusMax);
                        float angle = Random.Range(0f, Mathf.PI * 2f);
                        p.position = origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dist;
                        p.scale = PrimitiveTerrainGenerator.GetDefaultScale(type);

                        GameObject obj = null;
                        try
                        {
                            obj = PrimitiveTerrainGenerator.GeneratePrimitiveTerrain(p);
                        }
                        catch
                        {
                            // 失敗時はスキップ
                        }

                        if (obj != null)
                        {
                            activePrimitives.Add(obj);
                        }
                    }
                }

                yield return wait;
            }
        }

        private void CleanupNulls()
        {
            activePrimitives.RemoveAll(go => go == null);
        }
    }
}
