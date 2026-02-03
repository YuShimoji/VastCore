using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.Terrain.DualGrid
{
    /// <summary>
    /// 垂直データ管理クラス
    /// 各セル（Cell）の高さ方向のデータを保持する
    /// </summary>
    public class ColumnStack
    {
        #region Private Fields
        /// <summary>
        /// セルID → 高さレイヤーのリスト（各レイヤーがSolidかEmptyかを保持）
        /// </summary>
        private Dictionary<int, List<bool>> m_StackData;
        
        /// <summary>
        /// 最大高さ（レイヤー数）
        /// </summary>
        private int m_MaxHeight;
        #endregion

        #region Public Properties
        /// <summary>
        /// 最大高さ（レイヤー数）
        /// </summary>
        public int MaxHeight => m_MaxHeight;
        #endregion

        #region Constructors
        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public ColumnStack()
        {
            m_StackData = new Dictionary<int, List<bool>>();
            m_MaxHeight = 0;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// セルの指定レイヤーがSolidかどうかを取得
        /// </summary>
        /// <param name="_cellId">セルID</param>
        /// <param name="_layer">レイヤーインデックス（0から開始）</param>
        /// <returns>Solidの場合はtrue、Emptyまたは範囲外の場合はfalse</returns>
        public bool IsSolid(int _cellId, int _layer)
        {
            if (!m_StackData.ContainsKey(_cellId))
            {
                return false;
            }
            
            List<bool> layers = m_StackData[_cellId];
            if (_layer < 0 || _layer >= layers.Count)
            {
                return false;
            }
            
            return layers[_layer];
        }
        
        /// <summary>
        /// セルの指定レイヤーを設定
        /// </summary>
        /// <param name="_cellId">セルID</param>
        /// <param name="_layer">レイヤーインデックス</param>
        /// <param name="_isSolid">Solidかどうか</param>
        public void SetLayer(int _cellId, int _layer, bool _isSolid)
        {
            if (!m_StackData.ContainsKey(_cellId))
            {
                m_StackData[_cellId] = new List<bool>();
            }
            
            List<bool> layers = m_StackData[_cellId];
            
            // レイヤーが足りない場合は拡張
            while (layers.Count <= _layer)
            {
                layers.Add(false);
            }
            
            layers[_layer] = _isSolid;
            
            // 最大高さを更新
            if (_layer + 1 > m_MaxHeight)
            {
                m_MaxHeight = _layer + 1;
            }
        }
        
        /// <summary>
        /// セルの高さ（最上位のSolidレイヤー）を取得
        /// </summary>
        /// <param name="_cellId">セルID</param>
        /// <returns>高さ（レイヤー数）。Solidレイヤーがない場合は0</returns>
        public int GetHeight(int _cellId)
        {
            if (!m_StackData.ContainsKey(_cellId))
            {
                return 0;
            }
            
            List<bool> layers = m_StackData[_cellId];
            
            // 上から下に向かって最初のSolidレイヤーを探す
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                if (layers[i])
                {
                    return i + 1; // 高さは1から開始（レイヤー0 = 高さ1）
                }
            }
            
            return 0;
        }
        
        /// <summary>
        /// セルの全レイヤーをクリア
        /// </summary>
        /// <param name="_cellId">セルID</param>
        public void ClearCell(int _cellId)
        {
            if (m_StackData.ContainsKey(_cellId))
            {
                m_StackData[_cellId].Clear();
            }
        }
        
        /// <summary>
        /// 全データをクリア
        /// </summary>
        public void Clear()
        {
            m_StackData.Clear();
            m_MaxHeight = 0;
        }
        
        /// <summary>
        /// セルのレイヤーデータを取得（デバッグ用）
        /// </summary>
        /// <param name="_cellId">セルID</param>
        /// <returns>レイヤーのリスト（コピー）</returns>
        public List<bool> GetLayers(int _cellId)
        {
            if (!m_StackData.ContainsKey(_cellId))
            {
                return new List<bool>();
            }
            
            return new List<bool>(m_StackData[_cellId]);
        }
        #endregion
    }
}
