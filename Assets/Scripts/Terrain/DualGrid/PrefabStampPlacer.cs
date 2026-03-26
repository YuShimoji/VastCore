using System.Collections.Generic;
using UnityEngine;
using Vastcore.Generation;

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

        /// <summary>
        /// 変異生成のベースシード（0の場合は配置IDのみでシード生成）
        /// </summary>
        private int m_VariationSeed;

        /// <summary>
        /// StructureMaterialPalette 候補配列（パレットベースマテリアル選択用）
        /// </summary>
        private StructureMaterialPalette[] m_MaterialPalettes;

        /// <summary>
        /// タグ親和度ベースのマテリアル選択器
        /// </summary>
        private StructureMaterialSelector m_MaterialSelector;
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
        /// 変異生成のベースシードを設定。
        /// 配置IDと組み合わせて各スタンプ固有の乱数列を生成する。
        /// </summary>
        /// <param name="_seed">ベースシード</param>
        public void SetVariationSeed(int _seed)
        {
            m_VariationSeed = _seed;
        }

        /// <summary>
        /// マテリアルパレット候補を設定。
        /// パレットが設定されている場合、スタンプの TagProfile とのブレンドスコアで
        /// マテリアルを自動選択する (MaterialVariants より優先)。
        /// </summary>
        /// <param name="_palettes">パレット候補配列</param>
        public void SetMaterialPalettes(StructureMaterialPalette[] _palettes)
        {
            m_MaterialPalettes = _palettes;
            m_MaterialSelector = (_palettes != null && _palettes.Length > 0)
                ? new StructureMaterialSelector()
                : null;
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

            // 変異用の決定論的乱数を生成
            System.Random variationRng = new System.Random(
                m_VariationSeed ^ _placement.PlacementId);

            // ワールド座標を計算 + PositionJitter
            Vector3 position = CalculateWorldPosition(anchorCell, _placement.Layer);
            position += _placement.Definition.GetRandomPositionOffset(variationRng);

            Quaternion rotation = Quaternion.Euler(0f, _placement.Rotation, 0f);
            Vector3 scale = Vector3.one * _placement.Scale;

            // Prefabをインスタンス化
            GameObject instance = Object.Instantiate(
                _placement.Definition.Prefab, position, rotation, m_Parent);

            instance.transform.localScale = scale;
            instance.name = $"Stamp_{_placement.Definition.DisplayName}_{_placement.PlacementId}";

            // MaterialVariants 適用
            ApplyMaterialVariation(instance, _placement.Definition, variationRng);

            // ChildToggleGroups 適用
            ApplyChildToggle(instance, _placement.Definition, variationRng);

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
        /// マテリアルバリエーションを適用。
        /// パレットが設定されている場合はタグ親和度ベースで選択 (MaterialVariants よりも優先)。
        /// </summary>
        private void ApplyMaterialVariation(GameObject _instance,
            PrefabStampDefinition _definition, System.Random _random)
        {
            Material mat = null;

            // パレットベース選択 (TagProfile があり、パレットが設定されている場合)
            if (m_MaterialSelector != null && m_MaterialPalettes != null
                && _definition.TagProfile != null && !_definition.TagProfile.IsEmpty)
            {
                StructureMaterialPalette palette = m_MaterialSelector.Select(
                    _definition.TagProfile, m_MaterialPalettes, _random);
                if (palette != null)
                {
                    mat = palette.WallMaterial;
                }
            }

            // フォールバック: 従来の MaterialVariants からランダム選択
            if (mat == null)
            {
                mat = _definition.GetRandomMaterial(_random);
            }

            if (mat == null) return;

            MeshRenderer renderer = _instance.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = mat;
            }
        }

        /// <summary>
        /// 子オブジェクト表示切替を適用
        /// </summary>
        private void ApplyChildToggle(GameObject _instance,
            PrefabStampDefinition _definition, System.Random _random)
        {
            string[] groups = _definition.ChildToggleGroups;
            if (groups == null || groups.Length == 0) return;

            int selectedIndex = _definition.GetRandomChildToggleIndex(_random);

            bool anyFound = false;
            for (int i = 0; i < groups.Length; i++)
            {
                Transform child = _instance.transform.Find(groups[i]);
                if (child != null)
                {
                    child.gameObject.SetActive(i == selectedIndex);
                    anyFound = true;
                }
            }

            // 名前が1つも見つからなかった場合は警告
            if (!anyFound)
            {
                Vastcore.Utilities.VastcoreLogger.Instance.LogWarning("StampPlacer",
                    $"ChildToggleGroups: '{_definition.DisplayName}' に該当する子オブジェクトが見つかりません");
            }
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
