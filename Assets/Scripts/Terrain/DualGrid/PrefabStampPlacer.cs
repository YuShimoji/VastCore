using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.Terrain.DualGrid
{
    /// <summary>
    /// スタンプのGameObjectインスタンス化を担当するクラス
    /// StampPlacement + Grid情報 → 実際のGameObject配置
    /// </summary>
    public class PrefabStampPlacer
    {
        #region Constants
        /// <summary>
        /// 1レイヤーあたりのワールド高さ
        /// GridDebugVisualizerのDrawVerticalStacksと整合（layer + 0.5f）
        /// </summary>
        private const float c_LayerHeight = 1.0f;
        #endregion

        #region Private Fields
        /// <summary>
        /// 配置ID → 生成されたGameObject のマップ
        /// </summary>
        private readonly Dictionary<int, GameObject> m_Instances;

        /// <summary>
        /// 生成されたインスタンスの親Transform
        /// </summary>
        private Transform m_Parent;

        /// <summary>
        /// 地形高さサンプラー（null の場合はレイヤー高さのみ使用）
        /// </summary>
        private IHeightSampler m_HeightSampler;
        #endregion

        #region Public Properties
        /// <summary>
        /// 生成済みインスタンス数
        /// </summary>
        public int InstanceCount => m_Instances.Count;
        #endregion

        #region Constructors
        /// <summary>
        /// PrefabStampPlacerを作成
        /// </summary>
        /// <param name="_parent">インスタンスの親Transform（nullの場合はルート直下）</param>
        public PrefabStampPlacer(Transform _parent = null)
        {
            m_Instances = new Dictionary<int, GameObject>();
            m_Parent = _parent;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 親Transformを設定
        /// </summary>
        /// <param name="_parent">親Transform</param>
        public void SetParent(Transform _parent)
        {
            m_Parent = _parent;
        }

        /// <summary>
        /// 地形高さサンプラーを設定。
        /// 設定すると、スタンプ配置時にレイヤー高さの代わりに地形表面高さを使用する。
        /// </summary>
        /// <param name="_sampler">高さサンプラー（null で無効化）</param>
        public void SetHeightSampler(IHeightSampler _sampler)
        {
            m_HeightSampler = _sampler;
        }

        /// <summary>
        /// StampPlacementに基づいてGameObjectを生成
        /// </summary>
        /// <param name="_placement">配置データ</param>
        /// <param name="_grid">グリッド（セル位置の解決に使用）</param>
        /// <returns>生成されたGameObject。失敗時はnull</returns>
        public GameObject Instantiate(StampPlacement _placement, IrregularGrid _grid)
        {
            if (_placement == null || _placement.Definition == null || !_placement.Definition.IsValid())
            {
                return null;
            }

            if (_grid == null)
            {
                return null;
            }

            // アンカーセルを検索
            Cell anchorCell = FindCell(_grid, _placement.AnchorCellId);
            if (anchorCell == null)
            {
                return null;
            }

            // 既に生成済みの場合は先に破棄
            if (m_Instances.ContainsKey(_placement.PlacementId))
            {
                Destroy(_placement.PlacementId);
            }

            // ワールド座標を計算
            Vector3 position = CalculateWorldPosition(anchorCell, _placement.Layer);
            Quaternion rotation = Quaternion.Euler(0f, _placement.Rotation, 0f);
            Vector3 scale = Vector3.one * _placement.Scale;

            // Prefabをインスタンス化
            GameObject instance = Object.Instantiate(
                _placement.Definition.Prefab, position, rotation, m_Parent);

            instance.transform.localScale = scale;
            instance.name = $"Stamp_{_placement.Definition.DisplayName}_{_placement.PlacementId}";

            m_Instances[_placement.PlacementId] = instance;
            return instance;
        }

        /// <summary>
        /// 全StampPlacementをまとめて生成
        /// </summary>
        /// <param name="_registry">スタンプレジストリ</param>
        /// <param name="_grid">グリッド</param>
        /// <returns>生成されたGameObjectのリスト</returns>
        public List<GameObject> InstantiateAll(StampRegistry _registry, IrregularGrid _grid)
        {
            List<GameObject> results = new List<GameObject>();

            if (_registry == null || _grid == null)
            {
                return results;
            }

            foreach (StampPlacement placement in _registry.Placements)
            {
                GameObject instance = Instantiate(placement, _grid);
                if (instance != null)
                {
                    results.Add(instance);
                }
            }

            return results;
        }

        /// <summary>
        /// 配置IDのGameObjectを破棄
        /// </summary>
        /// <param name="_placementId">配置ID</param>
        /// <returns>破棄に成功した場合true</returns>
        public bool Destroy(int _placementId)
        {
            if (!m_Instances.TryGetValue(_placementId, out GameObject instance))
            {
                return false;
            }

            if (instance != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(instance);
                else
                    Object.DestroyImmediate(instance);
            }

            m_Instances.Remove(_placementId);
            return true;
        }

        /// <summary>
        /// 全インスタンスを破棄
        /// </summary>
        public void DestroyAll()
        {
            foreach (var kvp in m_Instances)
            {
                if (kvp.Value != null)
                {
                    if (Application.isPlaying)
                        Object.Destroy(kvp.Value);
                    else
                        Object.DestroyImmediate(kvp.Value);
                }
            }

            m_Instances.Clear();
        }

        /// <summary>
        /// 配置IDに対応するGameObjectを取得
        /// </summary>
        /// <param name="_placementId">配置ID</param>
        /// <returns>GameObject。なければnull</returns>
        public GameObject GetInstance(int _placementId)
        {
            m_Instances.TryGetValue(_placementId, out GameObject instance);
            return instance;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// セルIDからCellを検索
        /// </summary>
        private Cell FindCell(IrregularGrid _grid, int _cellId)
        {
            foreach (Cell cell in _grid.Cells)
            {
                if (cell.Id == _cellId)
                {
                    return cell;
                }
            }

            return null;
        }

        /// <summary>
        /// セル中心 → ワールド座標。
        /// IHeightSampler が設定されている場合は地形表面高さを使用し、
        /// レイヤー高さをオフセットとして加算する。
        /// 未設定の場合は従来通りレイヤー高さのみ。
        /// </summary>
        private Vector3 CalculateWorldPosition(Cell _cell, int _layer)
        {
            Vector3 center = _cell.GetCenter();

            float y;
            if (m_HeightSampler != null)
            {
                float terrainHeight = m_HeightSampler.SampleHeight(center.x, center.z);
                y = terrainHeight + _layer * c_LayerHeight;
            }
            else
            {
                y = _layer * c_LayerHeight;
            }

            return new Vector3(center.x, y, center.z);
        }
        #endregion
    }
}
