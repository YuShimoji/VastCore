using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// 構成要素のカテゴリ
    /// </summary>
    public enum ComponentCategory
    {
        /// <summary>外殻 (壁/屋根/基礎/床)</summary>
        Shell,
        /// <summary>開口部 (窓/ドア/通気口)</summary>
        Aperture,
        /// <summary>装飾 (柱/アーチ/彫刻等)</summary>
        Ornament
    }

    /// <summary>
    /// 構成要素の種類
    /// </summary>
    public enum ComponentType
    {
        // Shell
        Wall,
        Roof,
        Foundation,
        Floor,

        // Aperture
        Window,
        Door,
        Vent,

        // Ornament
        Column,
        ArchOrnament,
        Carving,
        Battlement,
        Buttress,
        Pinnacle
    }

    /// <summary>
    /// 構成要素の1バリエーション。
    /// タグ親和度ベクトルを持ち、建物タグとのブレンドスコアで選択確率が決まる。
    /// </summary>
    [Serializable]
    public class ComponentVariant
    {
        /// <summary>バリエーション名 (例: "GothicWindow", "RoundWindow")</summary>
        public string variantName;

        /// <summary>所属するカテゴリ</summary>
        public ComponentCategory category;

        /// <summary>構成要素の種類</summary>
        public ComponentType componentType;

        /// <summary>このバリエーションが親和するタグの重みプロファイル</summary>
        public StructureTagProfile affinity;

        public ComponentVariant(string _name, ComponentCategory _category,
            ComponentType _type, StructureTagProfile _affinity)
        {
            variantName = _name;
            category = _category;
            componentType = _type;
            affinity = _affinity;
        }
    }

    /// <summary>
    /// 構成要素バリエーションの登録・選択を行うシステム。
    /// タグ親和度によるブレンドスコアで確率的に選択する。
    /// </summary>
    public class ComponentSelector
    {
        #region Fields
        private readonly List<ComponentVariant> m_Variants = new List<ComponentVariant>();
        #endregion

        #region Registration
        /// <summary>
        /// バリエーションを登録
        /// </summary>
        public void Register(ComponentVariant _variant)
        {
            if (_variant == null) return;
            m_Variants.Add(_variant);
        }

        /// <summary>
        /// 複数のバリエーションを一括登録
        /// </summary>
        public void RegisterRange(IEnumerable<ComponentVariant> _variants)
        {
            if (_variants == null) return;
            foreach (var v in _variants)
            {
                Register(v);
            }
        }

        /// <summary>
        /// 登録済みバリエーション数
        /// </summary>
        public int Count => m_Variants.Count;
        #endregion

        #region Selection
        /// <summary>
        /// 指定タイプのバリエーションを建物タグプロファイルに基づいて選択する。
        /// ブレンドスコアを確率重みとしてルーレット選択を行う。
        /// </summary>
        /// <param name="_buildingProfile">建物のタグプロファイル</param>
        /// <param name="_componentType">選択する構成要素の種類</param>
        /// <param name="_random">乱数生成器</param>
        /// <returns>選択されたバリエーション。候補がなければ null</returns>
        public ComponentVariant Select(StructureTagProfile _buildingProfile,
            ComponentType _componentType, System.Random _random)
        {
            if (_buildingProfile == null || _random == null)
            {
                return null;
            }

            // 該当タイプのバリエーションを収集+スコア算出
            var candidates = new List<(ComponentVariant variant, float score)>();
            float totalScore = 0f;

            for (int i = 0; i < m_Variants.Count; i++)
            {
                if (m_Variants[i].componentType != _componentType) continue;
                if (m_Variants[i].affinity == null) continue;

                float score = _buildingProfile.BlendScore(m_Variants[i].affinity);
                if (score > 0f)
                {
                    candidates.Add((m_Variants[i], score));
                    totalScore += score;
                }
            }

            if (candidates.Count == 0)
            {
                // スコア0の候補も含めてフォールバック
                for (int i = 0; i < m_Variants.Count; i++)
                {
                    if (m_Variants[i].componentType != _componentType) continue;
                    candidates.Add((m_Variants[i], 1f));
                    totalScore += 1f;
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            // ルーレット選択
            float roll = (float)_random.NextDouble() * totalScore;
            float cumulative = 0f;

            for (int i = 0; i < candidates.Count; i++)
            {
                cumulative += candidates[i].score;
                if (roll <= cumulative)
                {
                    return candidates[i].variant;
                }
            }

            return candidates[candidates.Count - 1].variant;
        }

        /// <summary>
        /// 指定カテゴリの全タイプについてバリエーションを選択する。
        /// </summary>
        /// <param name="_buildingProfile">建物のタグプロファイル</param>
        /// <param name="_category">選択するカテゴリ</param>
        /// <param name="_random">乱数生成器</param>
        /// <returns>タイプ→バリエーションのマッピング</returns>
        public Dictionary<ComponentType, ComponentVariant> SelectCategory(
            StructureTagProfile _buildingProfile, ComponentCategory _category,
            System.Random _random)
        {
            var result = new Dictionary<ComponentType, ComponentVariant>();
            var types = GetTypesForCategory(_category);

            foreach (var type in types)
            {
                var selected = Select(_buildingProfile, type, _random);
                if (selected != null)
                {
                    result[type] = selected;
                }
            }

            return result;
        }

        /// <summary>
        /// 全カテゴリの全タイプについてバリエーションを選択する。
        /// </summary>
        public Dictionary<ComponentType, ComponentVariant> SelectAll(
            StructureTagProfile _buildingProfile, System.Random _random)
        {
            var result = new Dictionary<ComponentType, ComponentVariant>();

            foreach (ComponentCategory cat in Enum.GetValues(typeof(ComponentCategory)))
            {
                var catResult = SelectCategory(_buildingProfile, cat, _random);
                foreach (var kvp in catResult)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }
        #endregion

        #region Helpers
        /// <summary>
        /// カテゴリに属するComponentTypeの一覧を取得
        /// </summary>
        public static ComponentType[] GetTypesForCategory(ComponentCategory _category)
        {
            switch (_category)
            {
                case ComponentCategory.Shell:
                    return new[] { ComponentType.Wall, ComponentType.Roof,
                        ComponentType.Foundation, ComponentType.Floor };
                case ComponentCategory.Aperture:
                    return new[] { ComponentType.Window, ComponentType.Door,
                        ComponentType.Vent };
                case ComponentCategory.Ornament:
                    return new[] { ComponentType.Column, ComponentType.ArchOrnament,
                        ComponentType.Carving, ComponentType.Battlement,
                        ComponentType.Buttress, ComponentType.Pinnacle };
                default:
                    return Array.Empty<ComponentType>();
            }
        }
        #endregion
    }
}
