using System.Collections;
using UnityEngine;

namespace Vastcore.Core
{
    /// <summary>
    /// マネージャークラスの共通インターフェース
    /// 全てのマネージャーが実装すべき基本機能を定義
    /// </summary>
    public interface IManager
    {
        /// <summary>
        /// マネージャーの初期化
        /// </summary>
        void Initialize();

        /// <summary>
        /// マネージャーのシャットダウン
        /// </summary>
        void Shutdown();

        /// <summary>
        /// 初期化状態の確認
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// マネージャーの状態を取得
        /// </summary>
        ManagerStatus GetStatus();
    }

    /// <summary>
    /// マネージャーの状態
    /// </summary>
    public enum ManagerStatus
    {
        NotInitialized,
        Initializing,
        Running,
        Error,
        Shutdown
    }
}
