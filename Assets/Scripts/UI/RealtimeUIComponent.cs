using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.UI
{
    /// <summary>
    /// リアルタイムUIコンポーネントのインターフェース
    /// </summary>
    public interface IRealtimeUIComponent
    {
        /// <summary>
        /// コンポーネントがアクティブかどうか
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// リアルタイム更新の優先度
        /// </summary>
        int UpdatePriority { get; }

        /// <summary>
        /// リアルタイム更新を実行
        /// </summary>
        void UpdateRealtime();

        /// <summary>
        /// 更新が必要かどうかチェック
        /// </summary>
        bool NeedsUpdate();
    }

    /// <summary>
    /// UIコンポーネントプール
    /// </summary>
    public class UIComponentPool
    {
        private readonly string componentType;
        private readonly int initialSize;
        private readonly Queue<GameObject> availableObjects = new Queue<GameObject>();
        private readonly List<GameObject> allObjects = new List<GameObject>();

        public UIComponentPool(string type, int size)
        {
            componentType = type;
            initialSize = size;
            InitializePool();
        }

        private void InitializePool()
        {
            for (int i = 0; i < initialSize; i++)
            {
                GameObject obj = CreateNewObject();
                obj.SetActive(false);
                availableObjects.Enqueue(obj);
                allObjects.Add(obj);
            }
        }

        private GameObject CreateNewObject()
        {
            GameObject obj = new GameObject($"{componentType}_{allObjects.Count}");
            // ここに実際のコンポーネント追加ロジックを実装
            return obj;
        }

        public GameObject GetPooledObject()
        {
            GameObject obj;
            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
            }
            else
            {
                obj = CreateNewObject();
                allObjects.Add(obj);
            }

            obj.SetActive(true);
            return obj;
        }

        public void ReturnToPool(GameObject obj)
        {
            if (obj != null && allObjects.Contains(obj))
            {
                obj.SetActive(false);
                availableObjects.Enqueue(obj);
            }
        }

        public void Reset()
        {
            foreach (GameObject obj in allObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                    availableObjects.Enqueue(obj);
                }
            }
        }

        public int GetActiveCount()
        {
            return allObjects.Count - availableObjects.Count;
        }

        public int GetTotalCount()
        {
            return allObjects.Count;
        }
    }

    /// <summary>
    /// リアルタイムUI更新の基本クラス
    /// </summary>
    public abstract class RealtimeUIBehaviour : MonoBehaviour, IRealtimeUIComponent
    {
        [SerializeField] protected int updatePriority = 0;
        [SerializeField] protected float updateInterval = 1f / 30f; // 30FPS

        protected float lastUpdateTime;
        protected bool isRegistered = false;

        public virtual bool IsActive => gameObject.activeInHierarchy;
        public virtual int UpdatePriority => updatePriority;

        protected virtual void OnEnable()
        {
            RegisterWithManager();
        }

        protected virtual void OnDisable()
        {
            UnregisterFromManager();
        }

        protected virtual void Start()
        {
            RegisterWithManager();
        }

        protected virtual void OnDestroy()
        {
            UnregisterFromManager();
        }

        private void RegisterWithManager()
        {
            if (!isRegistered && UIRealtimeManager.Instance != null)
            {
                UIRealtimeManager.Instance.RegisterComponent(this);
                isRegistered = true;
            }
        }

        private void UnregisterFromManager()
        {
            if (isRegistered && UIRealtimeManager.Instance != null)
            {
                UIRealtimeManager.Instance.UnregisterComponent(this);
                isRegistered = false;
            }
        }

        public virtual bool NeedsUpdate()
        {
            return Time.time - lastUpdateTime >= updateInterval;
        }

        public virtual void UpdateRealtime()
        {
            if (NeedsUpdate())
            {
                PerformRealtimeUpdate();
                lastUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// リアルタイム更新の実装
        /// </summary>
        protected abstract void PerformRealtimeUpdate();

        /// <summary>
        /// 更新間隔を設定
        /// </summary>
        public void SetUpdateInterval(float interval)
        {
            updateInterval = Mathf.Max(interval, 1f / 120f); // 最低120FPS
        }

        /// <summary>
        /// 更新優先度を設定
        /// </summary>
        public void SetUpdatePriority(int priority)
        {
            updatePriority = priority;
        }
    }
}
