using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.UI
{
    /// <summary>
    /// リアルタイムUIコンポーネントの管理シングルトン
    /// </summary>
    public class UIRealtimeManager : MonoBehaviour
    {
        public static UIRealtimeManager Instance { get; private set; }

        private readonly List<IRealtimeUIComponent> registeredComponents = new List<IRealtimeUIComponent>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            for (int i = registeredComponents.Count - 1; i >= 0; i--)
            {
                var component = registeredComponents[i];
                if (component == null || (component is MonoBehaviour mb && mb == null))
                {
                    registeredComponents.RemoveAt(i);
                    continue;
                }

                if (component.IsActive && component.NeedsUpdate())
                {
                    component.UpdateRealtime();
                }
            }
        }

        public void RegisterComponent(IRealtimeUIComponent component)
        {
            if (component != null && !registeredComponents.Contains(component))
            {
                registeredComponents.Add(component);
            }
        }

        public void UnregisterComponent(IRealtimeUIComponent component)
        {
            registeredComponents.Remove(component);
        }
    }
}
