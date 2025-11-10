using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// プール管理可能なオブジェクトのインターフェース
    /// オブジェクトプールからの取得・返却時の動作を定義
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// プールからオブジェクトを取得した時の初期化処理
        /// </summary>
        void OnSpawnFromPool();

        /// <summary>
        /// プールにオブジェクトを返却する時のクリーンアップ処理
        /// </summary>
        void OnReturnToPool();

        /// <summary>
        /// オブジェクトが利用可能かどうかの確認
        /// </summary>
        bool IsAvailable { get; }
    }
}
