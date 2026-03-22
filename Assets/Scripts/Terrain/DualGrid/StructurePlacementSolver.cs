using System;
using System.Collections.Generic;
using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Terrain.DualGrid
{
    /// <summary>
    /// 配置候補の評価結果
    /// </summary>
    public struct PlacementCandidate
    {
        /// <summary>配置対象セル</summary>
        public Cell cell;

        /// <summary>選択されたスタンプ定義</summary>
        public PrefabStampDefinition stampDefinition;

        /// <summary>ゾーンバイアスとのブレンドスコア</summary>
        public float biasScore;

        /// <summary>隣接建物との親和度スコア</summary>
        public float adjacencyScore;

        /// <summary>総合スコア (biasScore * adjacencyScore)</summary>
        public float TotalScore => biasScore * adjacencyScore;
    }

    /// <summary>
    /// タグベースの知的配置を行うソルバー。
    /// PlacementZone のバイアス + AdjacencyRuleSet の隣接親和度を組み合わせて
    /// 「景観に映える」配置を実現する。
    /// </summary>
    public class StructurePlacementSolver
    {
        #region Private Fields

        private readonly StampRegistry m_Registry;

        #endregion

        #region Constructors

        /// <summary>
        /// ソルバーを作成する
        /// </summary>
        /// <param name="_registry">スタンプレジストリ (占有管理)</param>
        public StructurePlacementSolver(StampRegistry _registry)
        {
            m_Registry = _registry ?? throw new ArgumentNullException(nameof(_registry));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// グリッド上の指定セル群に対してタグベースの配置を実行する。
        /// </summary>
        /// <param name="_cells">配置候補セルの一覧</param>
        /// <param name="_availableStamps">利用可能なスタンプ定義の一覧</param>
        /// <param name="_zone">配置ゾーン (密度・バイアス・間隔制約)</param>
        /// <param name="_adjacencyRules">隣接親和度ルール (null の場合はデフォルト親和度を使用)</param>
        /// <param name="_random">乱数生成器</param>
        /// <returns>配置されたセル数</returns>
        public int Solve(
            IReadOnlyList<Cell> _cells,
            IReadOnlyList<PrefabStampDefinition> _availableStamps,
            PlacementZone _zone,
            AdjacencyRuleSet _adjacencyRules,
            System.Random _random)
        {
            if (_cells == null || _cells.Count == 0)
            {
                return 0;
            }

            if (_availableStamps == null || _availableStamps.Count == 0)
            {
                return 0;
            }

            if (_zone == null || _random == null)
            {
                return 0;
            }

            int placedCount = 0;
            int maxCount = _zone.MaxCount > 0 ? _zone.MaxCount : int.MaxValue;

            // シャッフルされたセル順序を生成 (配置順序のバイアスを防ぐ)
            List<int> shuffledIndices = CreateShuffledIndices(_cells.Count, _random);

            foreach (int idx in shuffledIndices)
            {
                if (placedCount >= maxCount)
                {
                    break;
                }

                Cell cell = _cells[idx];

                // 占有チェック
                if (m_Registry.IsOccupied(cell.Id))
                {
                    continue;
                }

                // 密度チェック: 確率的にスキップ
                if ((float)_random.NextDouble() > _zone.Density)
                {
                    continue;
                }

                // MinSpacing チェック: 近隣セルに既存配置がないか
                if (_zone.MinSpacing > 0 && HasNearbyPlacement(cell, _cells, _zone.MinSpacing))
                {
                    continue;
                }

                // スタンプ選択: ゾーンバイアスとのブレンドスコアで確率的選択
                PrefabStampDefinition selectedStamp = SelectStamp(
                    _availableStamps, _zone, _adjacencyRules, cell, _cells, _random);

                if (selectedStamp == null)
                {
                    continue;
                }

                // 配置
                float rotation = selectedStamp.GetRandomRotation(_random);
                float scale = selectedStamp.GetRandomScale(_random);

                StampPlacement placement = m_Registry.Place(
                    selectedStamp, cell, null, rotation, scale);

                if (placement != null)
                {
                    placedCount++;
                }
            }

            return placedCount;
        }

        /// <summary>
        /// スタンプを単体で評価する (テスト用)。
        /// ゾーンバイアスとのブレンドスコアと隣接親和度を返す。
        /// </summary>
        public PlacementCandidate Evaluate(
            Cell _cell,
            PrefabStampDefinition _stamp,
            PlacementZone _zone,
            AdjacencyRuleSet _adjacencyRules,
            IReadOnlyList<Cell> _allCells)
        {
            float biasScore = 0f;
            if (_zone != null && _stamp != null && _stamp.TagProfile != null)
            {
                biasScore = _zone.ZoneBias.BlendScore(_stamp.TagProfile);
            }

            float adjScore = 1f;
            if (_adjacencyRules != null && _cell != null && _stamp != null
                && _stamp.TagProfile != null && _allCells != null)
            {
                adjScore = EvaluateAdjacency(
                    _stamp.TagProfile, _adjacencyRules, _cell, _allCells);
            }

            return new PlacementCandidate
            {
                cell = _cell,
                stampDefinition = _stamp,
                biasScore = biasScore,
                adjacencyScore = adjScore
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// ゾーンバイアス + 隣接親和度を考慮してスタンプを確率的に選択する
        /// </summary>
        private PrefabStampDefinition SelectStamp(
            IReadOnlyList<PrefabStampDefinition> _stamps,
            PlacementZone _zone,
            AdjacencyRuleSet _adjacencyRules,
            Cell _cell,
            IReadOnlyList<Cell> _allCells,
            System.Random _random)
        {
            // 各スタンプのスコアを算出
            float totalScore = 0f;
            float[] scores = new float[_stamps.Count];

            for (int i = 0; i < _stamps.Count; i++)
            {
                PrefabStampDefinition stamp = _stamps[i];
                if (stamp == null || !stamp.IsValid())
                {
                    scores[i] = 0f;
                    continue;
                }

                // ゾーンバイアスとのブレンドスコア
                float biasScore = 0f;
                if (stamp.TagProfile != null && !_zone.ZoneBias.IsEmpty)
                {
                    biasScore = _zone.ZoneBias.BlendScore(stamp.TagProfile);
                }
                else
                {
                    // タグなしスタンプにもベースライン確率を与える
                    biasScore = 0.1f;
                }

                // 隣接親和度
                float adjScore = 1f;
                if (_adjacencyRules != null && stamp.TagProfile != null)
                {
                    adjScore = EvaluateAdjacency(
                        stamp.TagProfile, _adjacencyRules, _cell, _allCells);
                }

                float score = biasScore * adjScore;
                scores[i] = score;
                totalScore += score;
            }

            if (totalScore <= 0f)
            {
                // フォールバック: 均一ランダム
                int fallbackIdx = _random.Next(_stamps.Count);
                return _stamps[fallbackIdx];
            }

            // ルーレット選択
            float roll = (float)_random.NextDouble() * totalScore;
            float cumulative = 0f;

            for (int i = 0; i < scores.Length; i++)
            {
                cumulative += scores[i];
                if (roll <= cumulative)
                {
                    return _stamps[i];
                }
            }

            return _stamps[_stamps.Count - 1];
        }

        /// <summary>
        /// 対象セルの周辺に既配置建物との隣接親和度を評価する。
        /// 近隣に配置がない場合は 1.0 を返す。
        /// </summary>
        private float EvaluateAdjacency(
            StructureTagProfile _candidateProfile,
            AdjacencyRuleSet _rules,
            Cell _cell,
            IReadOnlyList<Cell> _allCells)
        {
            float totalAffinity = 0f;
            int neighborCount = 0;

            // セルのエッジから隣接セルを辿る
            IReadOnlyList<Edge> edges = _cell.Edges;
            for (int e = 0; e < edges.Count; e++)
            {
                Edge edge = edges[e];
                // エッジに隣接するセルを探す
                for (int c = 0; c < _allCells.Count; c++)
                {
                    Cell neighbor = _allCells[c];
                    if (neighbor.Id == _cell.Id) continue;

                    // 隣接セルに配置があるか
                    StampPlacement existing = m_Registry.GetPlacementAt(neighbor.Id);
                    if (existing == null) continue;
                    if (existing.Definition == null || existing.Definition.TagProfile == null)
                        continue;

                    float affinity = _rules.EvaluateAdjacency(
                        _candidateProfile, existing.Definition.TagProfile);
                    totalAffinity += affinity;
                    neighborCount++;
                }
            }

            if (neighborCount == 0)
            {
                return 1f; // 近隣に配置なし → 制約なし
            }

            return Mathf.Clamp01(totalAffinity / neighborCount);
        }

        /// <summary>
        /// 近隣にMinSpacing以内の既存配置があるかチェックする。
        /// 簡易版: セルの中心座標間距離で判定 (ヘックストポロジーではなくユークリッド距離)。
        /// </summary>
        private bool HasNearbyPlacement(Cell _cell, IReadOnlyList<Cell> _allCells, int _minSpacing)
        {
            // セル間距離の概算: DualGridのセル間距離は不均一だが、
            // minSpacing はセル数の概算として使用。
            // 正確なヘックス距離はグリッドトポロジーが必要だが、
            // ここではOccupancyMap上の配置との距離を簡易的にチェック。
            UnityEngine.Vector3 center = _cell.GetCenter();

            foreach (StampPlacement placement in m_Registry.Placements)
            {
                // アンカーセルの位置を allCells から探す
                for (int i = 0; i < _allCells.Count; i++)
                {
                    if (_allCells[i].Id == placement.AnchorCellId)
                    {
                        UnityEngine.Vector3 otherCenter = _allCells[i].GetCenter();
                        float dist = UnityEngine.Vector3.Distance(center, otherCenter);
                        // セル間距離の概算: 1セル ≈ 1.0 ワールド単位
                        // (実際のDualGridではリラクゼーション後のセルサイズに依存)
                        if (dist < _minSpacing)
                        {
                            return true;
                        }
                        break;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Fisher-Yates シャッフルでインデックスリストを生成
        /// </summary>
        private List<int> CreateShuffledIndices(int _count, System.Random _random)
        {
            List<int> indices = new List<int>(_count);
            for (int i = 0; i < _count; i++)
            {
                indices.Add(i);
            }

            for (int i = _count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                int temp = indices[i];
                indices[i] = indices[j];
                indices[j] = temp;
            }

            return indices;
        }

        #endregion
    }
}
