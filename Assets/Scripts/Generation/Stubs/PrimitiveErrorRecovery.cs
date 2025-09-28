using System;
using System.Collections;
using UnityEngine;

namespace Vastcore.Generation
{
    // Minimal stub to unblock editor tests. Provides a simple spawn + fallback.
    public class PrimitiveErrorRecovery : MonoBehaviour
    {
        private static PrimitiveErrorRecovery _instance;
        public static PrimitiveErrorRecovery Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = GameObject.Find("__PrimitiveErrorRecovery__");
                    if (go == null)
                    {
                        go = new GameObject("__PrimitiveErrorRecovery__");
                        go.hideFlags = HideFlags.HideAndDontSave;
                    }
                    _instance = go.GetComponent<PrimitiveErrorRecovery>();
                    if (_instance == null) _instance = go.AddComponent<PrimitiveErrorRecovery>();
                }
                return _instance;
            }
        }

        public IEnumerator RecoverPrimitiveSpawn(
            Vector3 position,
            PrimitiveType primitiveType,
            float scale,
            Action<GameObject> onSuccess,
            Action onFailure)
        {
            GameObject spawned = null;
            try
            {
                spawned = GameObject.CreatePrimitive(primitiveType);
            }
            catch
            {
                // fallback
                spawned = GameObject.CreatePrimitive(PrimitiveType.Cube);
            }

            if (spawned != null)
            {
                spawned.transform.position = position + Vector3.up * 0.5f; // lift slightly to avoid ground clipping
                spawned.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);
                onSuccess?.Invoke(spawned);
            }
            else
            {
                onFailure?.Invoke();
            }
            yield break;
        }
    }
}
